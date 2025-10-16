using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PowerShell.MemoryAnalysis.Services
{
    /// <summary>
    /// File change monitoring and cache invalidation service
    /// </summary>
    public class CacheInvalidationService : IDisposable
    {
        private readonly ILogger<CacheInvalidationService>? _logger;
        private readonly CachingService _cachingService;
        private readonly Dictionary<string, FileSystemWatcher> _watchers;
        private readonly Dictionary<string, long> _fileHashes;
        private readonly Lock _lock = new();
        private bool _disposed;

        /// <summary>
        /// Event fired when cache needs invalidation
        /// </summary>
        public event EventHandler<CacheInvalidationEventArgs>? CacheInvalidationTriggered;

        /// <summary>
        /// Create a new cache invalidation service
        /// </summary>
        public CacheInvalidationService(
            CachingService cachingService,
            ILogger<CacheInvalidationService>? logger = null)
        {
            _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
            _logger = logger ?? LoggingService.GetLogger<CacheInvalidationService>();
            _watchers = [];
            _fileHashes = [];

            _logger?.LogInformation("Cache invalidation service initialized");
        }

        /// <summary>
        /// Start monitoring a file for changes
        /// </summary>
        /// <param name="filePath">Path to the file to monitor</param>
        public void WatchFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger?.LogWarning("Cannot watch file that does not exist: {FilePath}", filePath);
                return;
            }

            lock (_lock)
            {
                if (_watchers.ContainsKey(filePath))
                {
                    _logger?.LogDebug("File already being watched: {FilePath}", filePath);
                    return;
                }

                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    var fileName = Path.GetFileName(filePath);

                    if (string.IsNullOrEmpty(directory))
                    {
                        _logger?.LogWarning("Invalid directory path for file: {FilePath}", filePath);
                        return;
                    }

                    var watcher = new FileSystemWatcher(directory)
                    {
                        Filter = fileName,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
                    };

                    watcher.Changed += (s, e) => OnFileChanged(e.FullPath);
                    watcher.Renamed += (s, e) => OnFileChanged(e.FullPath);
                    watcher.Deleted += (s, e) => OnFileChanged(e.FullPath);

                    // Store initial hash
                    _fileHashes[filePath] = CalculateFileHash(filePath);

                    watcher.EnableRaisingEvents = true;
                    _watchers[filePath] = watcher;

                    _logger?.LogInformation("Started watching file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error starting file watch for {FilePath}", filePath);
                }
            }
        }

        /// <summary>
        /// Stop monitoring a file for changes
        /// </summary>
        public void UnwatchFile(string filePath)
        {
            lock (_lock)
            {
                if (_watchers.TryGetValue(filePath, out var watcher))
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    _watchers.Remove(filePath);
                    _fileHashes.Remove(filePath);

                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("Stopped watching file: {FilePath}", filePath);
                    }
                }
            }
        }

        /// <summary>
        /// Stop monitoring all files
        /// </summary>
        public void StopWatchingAll()
        {
            lock (_lock)
            {
                foreach (var watcher in _watchers.Values)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }

                _watchers.Clear();
                _fileHashes.Clear();

                _logger?.LogInformation("Stopped watching all files");
            }
        }

        /// <summary>
        /// Get list of currently watched files
        /// </summary>
        public IReadOnlyList<string> GetWatchedFiles()
        {
            lock (_lock)
            {
                return _watchers.Keys.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Invalidate cache for a specific file
        /// </summary>
        public void InvalidateForFile(string filePath)
        {
            _cachingService.ClearAllCaches();
            _logger?.LogInformation("Cache invalidated for file: {FilePath}", filePath);

            CacheInvalidationTriggered?.Invoke(
                this,
                new CacheInvalidationEventArgs
                {
                    FilePath = filePath,
                    Reason = "File changed",
                    Timestamp = DateTime.UtcNow
                }
            );
        }

        /// <summary>
        /// Validate cache entry based on file hash
        /// </summary>
        public bool ValidateCacheEntry(string filePath)
        {
            if (!_fileHashes.TryGetValue(filePath, out var storedHash))
            {
                return true; // Not being watched, assume valid
            }

            var currentHash = CalculateFileHash(filePath);

            if (storedHash != currentHash)
            {
                if (_logger?.IsEnabled(LogLevel.Warning) == true)
                {
                    _logger.LogWarning(
                        "Cache validation failed for {FilePath}: hash changed from {OldHash} to {NewHash}",
                        filePath,
                        storedHash,
                        currentHash
                    );
                }

                // Update hash for next check
                _fileHashes[filePath] = currentHash;
                InvalidateForFile(filePath);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle file change events
        /// </summary>
        private void OnFileChanged(string filePath)
        {
            _logger?.LogDebug("File change detected: {FilePath}", filePath);

            // Debounce - wait a bit for multiple events
            System.Threading.Thread.Sleep(100);

            lock (_lock)
            {
                if (_fileHashes.ContainsKey(filePath))
                {
                    InvalidateForFile(filePath);
                }
            }
        }

        /// <summary>
        /// Calculate file hash for change detection
        /// </summary>
        private static long CalculateFileHash(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return 0;

                // Use size + modification time as a simple hash
                return (fileInfo.Length * 397) ^ fileInfo.LastWriteTimeUtc.ToBinary();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            StopWatchingAll();
            _disposed = true;
            GC.SuppressFinalize(this);

            _logger?.LogInformation("Cache invalidation service disposed");
        }
    }

    /// <summary>
    /// Event arguments for cache invalidation events
    /// </summary>
    public class CacheInvalidationEventArgs : EventArgs
    {
        /// <summary>
        /// File that triggered invalidation
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Reason for invalidation
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// When invalidation occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Extension methods for cache invalidation
    /// </summary>
    public static class CacheInvalidationExtensions
    {
        private static readonly Dictionary<string, CacheInvalidationService> _services = [];
        private static readonly Lock _servicesLock = new();

        /// <summary>
        /// Get or create cache invalidation service for a caching service
        /// </summary>
        public static CacheInvalidationService GetInvalidationService(
            this CachingService cachingService,
            ILogger<CacheInvalidationService>? logger = null)
        {
            var key = cachingService.GetHashCode().ToString();

            lock (_servicesLock)
            {
                if (!_services.TryGetValue(key, out var service))
                {
                    service = new CacheInvalidationService(cachingService, logger);
                    _services[key] = service;
                }

                return service;
            }
        }

        /// <summary>
        /// Validate all cache entries based on watched files
        /// </summary>
        public static bool ValidateAllCacheEntries(this CacheInvalidationService service)
        {
            var watchedFiles = service.GetWatchedFiles();
            var allValid = true;

            foreach (var file in watchedFiles)
            {
                if (!service.ValidateCacheEntry(file))
                {
                    allValid = false;
                }
            }

            return allValid;
        }
    }
}
