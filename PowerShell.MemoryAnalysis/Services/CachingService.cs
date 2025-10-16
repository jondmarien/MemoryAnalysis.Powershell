using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;

namespace PowerShell.MemoryAnalysis.Services
{
    /// <summary>
    /// Cache configuration settings
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// Maximum number of entries in cache
        /// </summary>
        public int MaxEntries { get; set; } = 20;

        /// <summary>
        /// Time-to-live in seconds (0 = no expiration)
        /// </summary>
        public long TtlSeconds { get; set; } = 7200; // 2 hours

        /// <summary>
        /// Enable persistent storage to disk
        /// </summary>
        public bool PersistToDisk { get; set; } = false;

        /// <summary>
        /// Cache directory for persistent storage
        /// </summary>
        public string CacheDir { get; set; } = ".cache";
    }

    /// <summary>
    /// Cache entry with metadata
    /// </summary>
    /// <typeparam name="T">Type of cached data</typeparam>
    public class CacheEntry<T>
    {
        /// <summary>
        /// The cached data
        /// </summary>
        public required T Data { get; set; }

        /// <summary>
        /// Unix timestamp when entry was created
        /// </summary>
        public long CreatedAt { get; set; }

        /// <summary>
        /// Unix timestamp when entry was last accessed
        /// </summary>
        public long AccessedAt { get; set; }

        /// <summary>
        /// File hash for change detection
        /// </summary>
        public required string FileHash { get; set; }

        /// <summary>
        /// Number of times this entry has been accessed
        /// </summary>
        public long AccessCount { get; set; }

        /// <summary>
        /// Check if entry is stale based on TTL
        /// </summary>
        public bool IsStale(long ttlSeconds)
        {
            if (ttlSeconds == 0)
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (now - CreatedAt) > ttlSeconds;
        }

        /// <summary>
        /// Update access time and increment counter
        /// </summary>
        public void Touch()
        {
            AccessedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AccessCount++;
        }
    }

    /// <summary>
    /// Cache statistics snapshot
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Current number of entries
        /// </summary>
        public int EntriesCount { get; set; }

        /// <summary>
        /// Maximum allowed entries
        /// </summary>
        public int MaxEntries { get; set; }

        /// <summary>
        /// Total accesses across all entries
        /// </summary>
        public long TotalAccesses { get; set; }

        /// <summary>
        /// Number of cache hits
        /// </summary>
        public long CacheHits { get; set; }

        /// <summary>
        /// Number of cache misses
        /// </summary>
        public long CacheMisses { get; set; }

        /// <summary>
        /// Cache hit rate (0-1)
        /// </summary>
        [JsonIgnore]
        public double HitRate
        {
            get
            {
                var total = CacheHits + CacheMisses;
                return total == 0 ? 0 : (double)CacheHits / total;
            }
        }
    }

    /// <summary>
    /// LRU cache for memory analysis results
    /// </summary>
    /// <typeparam name="T">Type of data to cache</typeparam>
    public class LruCache<T> where T : class
    {
        private readonly Dictionary<string, CacheEntry<T>> _entries;
        private readonly CacheConfiguration _config;
        private readonly ILogger<LruCache<T>>? _logger;
        private long _hits;
        private long _misses;
        private readonly Lock _lock = new();

        /// <summary>
        /// Create a new LRU cache
        /// </summary>
        public LruCache(CacheConfiguration config, ILogger<LruCache<T>>? logger = null)
        {
            _config = config ?? new CacheConfiguration();
            _logger = logger;
            _entries = new Dictionary<string, CacheEntry<T>>(_config.MaxEntries);
            _hits = 0;
            _misses = 0;

            // Create cache directory if persistence enabled
            if (_config.PersistToDisk)
            {
                Directory.CreateDirectory(_config.CacheDir);
            }
        }

        /// <summary>
        /// Get a value from cache
        /// </summary>
        public T? Get(string key, string expectedHash)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(key, out var entry))
                {
                    // Validate file hash
                    if (entry.FileHash != expectedHash)
                    {
                        _entries.Remove(key);
                        _misses++;
                        return null;
                    }

                    // Check TTL
                    if (_config.TtlSeconds > 0 && entry.IsStale(_config.TtlSeconds))
                    {
                        _entries.Remove(key);
                        _misses++;
                        return null;
                    }

                    // Update access stats
                    entry.Touch();
                    _hits++;
                    return entry.Data;
                }

                _misses++;
                return null;
            }
        }

        /// <summary>
        /// Put a value in cache
        /// </summary>
        public void Put(string key, T value, string fileHash)
        {
            lock (_lock)
            {
                // Evict LRU entry if at capacity
                if (_entries.Count >= _config.MaxEntries && !_entries.ContainsKey(key))
                {
                    var lruKey = _entries
                        .OrderBy(kvp => kvp.Value.AccessCount)
                        .ThenBy(kvp => kvp.Value.AccessedAt)
                        .First()
                        .Key;

                    _entries.Remove(lruKey);
                    _logger?.LogDebug($"Evicted cache entry: {lruKey}");
                }

                _entries[key] = new CacheEntry<T>
                {
                    Data = value,
                    FileHash = fileHash,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    AccessedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    AccessCount = 0
                };
            }
        }

        /// <summary>
        /// Clear all cache entries
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
                _logger?.LogInformation("Cache cleared");
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new CacheStatistics
                {
                    EntriesCount = _entries.Count,
                    MaxEntries = _config.MaxEntries,
                    TotalAccesses = _hits + _misses,
                    CacheHits = _hits,
                    CacheMisses = _misses
                };
            }
        }

        /// <summary>
        /// Check if key exists
        /// </summary>
        public bool Contains(string key)
        {
            lock (_lock)
            {
                return _entries.ContainsKey(key);
            }
        }
    }

    /// <summary>
    /// Caching service for memory analysis operations
    /// </summary>
    public class CachingService
    {
        private readonly LruCache<ProcessInfo[]> _processCache;
        private readonly LruCache<CommandLineInfo[]> _commandLineCache;
        private readonly LruCache<DllInfo[]> _dllCache;
        private readonly LruCache<NetworkConnectionInfo[]> _networkCache;
        private readonly LruCache<MalwareDetection[]> _malwareCache;
        private readonly CacheConfiguration _config;
        private readonly ILogger<CachingService>? _logger;

        /// <summary>
        /// Create a new caching service
        /// </summary>
        public CachingService(
            CacheConfiguration? config = null,
            ILogger<CachingService>? logger = null)
        {
            _config = config ?? new CacheConfiguration();
            _logger = logger;

            _processCache = new LruCache<ProcessInfo[]>(_config, LoggingService.GetLogger<LruCache<ProcessInfo[]>>());
            _commandLineCache = new LruCache<CommandLineInfo[]>(_config, LoggingService.GetLogger<LruCache<CommandLineInfo[]>>());
            _dllCache = new LruCache<DllInfo[]>(_config, LoggingService.GetLogger<LruCache<DllInfo[]>>());
            _networkCache = new LruCache<NetworkConnectionInfo[]>(_config, LoggingService.GetLogger<LruCache<NetworkConnectionInfo[]>>());
            _malwareCache = new LruCache<MalwareDetection[]>(_config, LoggingService.GetLogger<LruCache<MalwareDetection[]>>());

            _logger?.LogInformation("Caching service initialized with {MaxEntries} max entries", _config.MaxEntries);
        }

        /// <summary>
        /// Get or cache process analysis results
        /// </summary>
        public ProcessInfo[] GetOrCacheProcesses(
            string dumpPath,
            Func<ProcessInfo[]> loader)
        {
            var hash = CalculateFileHash(dumpPath);
            var cacheKey = $"processes_{dumpPath}";

            var cached = _processCache.Get(cacheKey, hash);
            if (cached != null)
            {
                _logger?.LogDebug("Process cache hit for {DumpPath}", dumpPath);
                return cached;
            }

            _logger?.LogDebug("Process cache miss for {DumpPath}", dumpPath);
            var data = loader();
            _processCache.Put(cacheKey, data, hash);

            return data;
        }

        /// <summary>
        /// Get or cache command line analysis results
        /// </summary>
        public CommandLineInfo[] GetOrCacheCommandLines(
            string dumpPath,
            Func<CommandLineInfo[]> loader)
        {
            var hash = CalculateFileHash(dumpPath);
            var cacheKey = $"cmdlines_{dumpPath}";

            var cached = _commandLineCache.Get(cacheKey, hash);
            if (cached != null)
            {
                _logger?.LogDebug("Command line cache hit for {DumpPath}", dumpPath);
                return cached;
            }

            _logger?.LogDebug("Command line cache miss for {DumpPath}", dumpPath);
            var data = loader();
            _commandLineCache.Put(cacheKey, data, hash);

            return data;
        }

        /// <summary>
        /// Get or cache DLL analysis results
        /// </summary>
        public DllInfo[] GetOrCacheDlls(
            string dumpPath,
            uint? pidFilter,
            Func<DllInfo[]> loader)
        {
            var hash = CalculateFileHash(dumpPath);
            var pidStr = pidFilter.HasValue ? $"_{pidFilter}" : string.Empty;
            var cacheKey = $"dlls_{dumpPath}{pidStr}";

            var cached = _dllCache.Get(cacheKey, hash);
            if (cached != null)
            {
                _logger?.LogDebug("DLL cache hit for {DumpPath}", dumpPath);
                return cached;
            }

            _logger?.LogDebug("DLL cache miss for {DumpPath}", dumpPath);
            var data = loader();
            _dllCache.Put(cacheKey, data, hash);

            return data;
        }

        /// <summary>
        /// Get or cache network analysis results
        /// </summary>
        public NetworkConnectionInfo[] GetOrCacheNetworkConnections(
            string dumpPath,
            Func<NetworkConnectionInfo[]> loader)
        {
            var hash = CalculateFileHash(dumpPath);
            var cacheKey = $"networks_{dumpPath}";

            var cached = _networkCache.Get(cacheKey, hash);
            if (cached != null)
            {
                _logger?.LogDebug("Network cache hit for {DumpPath}", dumpPath);
                return cached;
            }

            _logger?.LogDebug("Network cache miss for {DumpPath}", dumpPath);
            var data = loader();
            _networkCache.Put(cacheKey, data, hash);

            return data;
        }

        /// <summary>
        /// Get or cache malware analysis results
        /// </summary>
        public MalwareDetection[] GetOrCacheMalware(
            string dumpPath,
            Func<MalwareDetection[]> loader)
        {
            var hash = CalculateFileHash(dumpPath);
            var cacheKey = $"malware_{dumpPath}";

            var cached = _malwareCache.Get(cacheKey, hash);
            if (cached != null)
            {
                _logger?.LogDebug("Malware cache hit for {DumpPath}", dumpPath);
                return cached;
            }

            _logger?.LogDebug("Malware cache miss for {DumpPath}", dumpPath);
            var data = loader();
            _malwareCache.Put(cacheKey, data, hash);

            return data;
        }

        /// <summary>
        /// Clear all caches
        /// </summary>
        public void ClearAllCaches()
        {
            _processCache.Clear();
            _commandLineCache.Clear();
            _dllCache.Clear();
            _networkCache.Clear();
            _malwareCache.Clear();

            _logger?.LogInformation("All caches cleared");
        }

        /// <summary>
        /// Get statistics for all caches
        /// </summary>
        public Dictionary<string, CacheStatistics> GetAllCacheStatistics()
        {
            return new Dictionary<string, CacheStatistics>
            {
                { "Processes", _processCache.GetStatistics() },
                { "CommandLines", _commandLineCache.GetStatistics() },
                { "DLLs", _dllCache.GetStatistics() },
                { "Networks", _networkCache.GetStatistics() },
                { "Malware", _malwareCache.GetStatistics() }
            };
        }

        /// <summary>
        /// Calculate file hash for change detection
        /// </summary>
        private static string CalculateFileHash(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return "FILE_NOT_FOUND";

                var size = fileInfo.Length;
                var mtime = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();

                return $"{size}_{mtime}";
            }
            catch (Exception ex)
            {
                return $"ERROR_{ex.GetHashCode()}";
            }
        }
    }
}
