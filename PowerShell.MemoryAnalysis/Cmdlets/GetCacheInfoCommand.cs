using System;
using System.Management.Automation;
using System.Text;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Models;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets
{
    /// <summary>
    /// Get-CacheInfo cmdlet - Display cache statistics
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CacheInfo")]
    [OutputType(typeof(CacheInfoOutput))]
    public class GetCacheInfoCommand : PSCmdlet
    {
        private ILogger<GetCacheInfoCommand>? _logger;
        private static CachingService? _cachingService;

        /// <summary>
        /// Initialize the cmdlet
        /// </summary>
        protected override void BeginProcessing()
        {
            _logger = LoggingService.GetLogger<GetCacheInfoCommand>();
            _logger?.LogInformation("Get-CacheInfo cmdlet started");

            // Initialize global caching service if needed
            _cachingService ??= new CachingService();
        }

        /// <summary>
        /// Process the cmdlet
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                WriteVerbose("Retrieving cache statistics");

                var stats = _cachingService?.GetAllCacheStatistics();
                if (stats == null)
                {
                    WriteWarning("Unable to retrieve cache statistics");
                    return;
                }

                foreach (var cacheType in stats)
                {
                    var output = new CacheInfoOutput
                    {
                        CacheType = cacheType.Key,
                        EntriesCount = cacheType.Value.EntriesCount,
                        MaxEntries = cacheType.Value.MaxEntries,
                        TotalAccesses = cacheType.Value.TotalAccesses,
                        CacheHits = cacheType.Value.CacheHits,
                        CacheMisses = cacheType.Value.CacheMisses,
                        HitRate = cacheType.Value.HitRate
                    };

                    WriteObject(output);

                    if (_logger?.IsEnabled(LogLevel.Debug) == true)
                    {
                        _logger.LogDebug(
                            "Cache {CacheType}: {Hits} hits, {Misses} misses, {HitRate:P} hit rate",
                            cacheType.Key,
                            cacheType.Value.CacheHits,
                            cacheType.Value.CacheMisses,
                            cacheType.Value.HitRate
                        );
                    }
                }

                WriteVerbose($"Retrieved statistics for {stats.Count} cache types");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving cache info");

                var error = new ErrorRecord(
                    ex,
                    "CacheInfoError",
                    ErrorCategory.InvalidOperation,
                    null
                )
                {
                    ErrorDetails = new ErrorDetails(
                        $"Failed to retrieve cache information: {ex.Message}"
                    )
                };
                WriteError(error);
            }
        }

        /// <summary>
        /// End processing
        /// </summary>
        protected override void EndProcessing()
        {
            _logger?.LogInformation("Get-CacheInfo cmdlet completed");
        }
    }

    /// <summary>
    /// Output object for cache information
    /// </summary>
    public class CacheInfoOutput
    {
        /// <summary>
        /// Type of cache (Processes, CommandLines, DLLs, etc.)
        /// </summary>
        public required string CacheType { get; set; }

        /// <summary>
        /// Number of entries currently in cache
        /// </summary>
        public int EntriesCount { get; set; }

        /// <summary>
        /// Maximum number of entries allowed
        /// </summary>
        public int MaxEntries { get; set; }

        /// <summary>
        /// Total number of cache accesses
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
        public double HitRate { get; set; }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"{CacheType}: {EntriesCount}/{MaxEntries} entries, {HitRate:P} hit rate ({CacheHits} hits, {CacheMisses} misses)";
        }
    }
}
