using System;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets
{
    /// <summary>
    /// Watch a memory dump file for changes and automatically invalidate cache
    /// </summary>
    [Cmdlet(VerbsCommon.Watch, "MemoryDumpFile")]
    [OutputType(typeof(void))]
    public class WatchCacheFileCommand : PSCmdlet
    {
        /// <summary>
        /// Path to the memory dump file to watch
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string? Path { get; set; }

        /// <summary>
        /// Caching service instance (static for session persistence)
        /// </summary>
        private static CachingService? _cachingService;

        /// <summary>
        /// Cache invalidation service instance (static for session persistence)
        /// </summary>
        private static CacheInvalidationService? _invalidationService;

        /// <summary>
        /// Logger instance (unused - kept for future logging)
        /// </summary>
        #pragma warning disable CS0169
        private readonly ILogger<WatchCacheFileCommand>? _logger;
        #pragma warning restore CS0169

        /// <summary>
        /// Process record - start watching the file
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrEmpty(Path))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentNullException(nameof(Path)),
                        "PathNull",
                        ErrorCategory.InvalidArgument,
                        null
                    ));
                    return;
                }

                // Initialize services if needed
                _cachingService ??= new CachingService();

                _invalidationService ??= _cachingService.GetInvalidationService(
                        LoggingService.GetLogger<CacheInvalidationService>());

                // Verify file exists
                if (!System.IO.File.Exists(Path))
                {
                    WriteError(new ErrorRecord(
                        new System.IO.FileNotFoundException($"File not found: {Path}"),
                        "FileNotFound",
                        ErrorCategory.ObjectNotFound,
                        Path
                    ));
                    return;
                }

                // Start watching
                _invalidationService?.WatchFile(Path);

                WriteObject($"Now watching file: {Path}");
                WriteObject("Cache will be automatically invalidated when file changes.");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "WatchFileError",
                    ErrorCategory.OperationStopped,
                    Path
                ));
            }
        }
    }

    /// <summary>
    /// Stop watching a memory dump file
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "WatchingMemoryDumpFile")]
    [OutputType(typeof(void))]
    public class StopWatchingCacheFileCommand : PSCmdlet
    {
        /// <summary>
        /// Path to the memory dump file to stop watching
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string? Path { get; set; }

        /// <summary>
        /// Caching service instance
        /// </summary>
        private static CachingService? _cachingService;

        /// <summary>
        /// Cache invalidation service instance
        /// </summary>
        private static CacheInvalidationService? _invalidationService;

        /// <summary>
        /// Process record - stop watching the file
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                _cachingService ??= new CachingService();

                _invalidationService ??= _cachingService.GetInvalidationService(null);

                if (_invalidationService == null)
                {
                    WriteWarning("No files are currently being watched");
                    return;
                }

                if (!string.IsNullOrEmpty(Path))
                {
                    _invalidationService.UnwatchFile(Path);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ArgumentNullException(nameof(Path)),
                        "PathNull",
                        ErrorCategory.InvalidArgument,
                        null
                    ));
                }

                WriteObject($"Stopped watching file: {Path}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "UnwatchFileError",
                    ErrorCategory.OperationStopped,
                    Path
                ));
            }
        }
    }

    /// <summary>
    /// List all watched memory dump files
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WatchedMemoryDumpFiles")]
    [OutputType(typeof(string))]
    public class GetWatchedFilesCommand : PSCmdlet
    {
        /// <summary>
        /// Caching service instance
        /// </summary>
        private static CachingService? _cachingService;

        /// <summary>
        /// Cache invalidation service instance
        /// </summary>
        private static CacheInvalidationService? _invalidationService;

        /// <summary>
        /// Logger instance (unused - kept for future logging)
        /// </summary>
        #pragma warning disable CS0169
        private static readonly ILogger<GetWatchedFilesCommand>? _logger;
        #pragma warning restore CS0169

        /// <summary>
        /// Process record - list watched files
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                _cachingService ??= new CachingService();

                _invalidationService ??= _cachingService.GetInvalidationService(null);

                if (_invalidationService == null)
                {
                    WriteObject("No files are currently being watched");
                    return;
                }

                var watchedFiles = _invalidationService.GetWatchedFiles();

                if (watchedFiles.Count == 0)
                {
                    WriteObject("No files are currently being watched");
                    return;
                }

                WriteObject($"Currently watching {watchedFiles.Count} file(s):");
                foreach (var file in watchedFiles)
                {
                    WriteObject($"  - {file}");
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "ListWatchedFilesError",
                    ErrorCategory.OperationStopped,
                    null
                ));
            }
        }
    }

    /// <summary>
    /// Validate all watched files and trigger cache invalidation if changes detected
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "CacheValidity")]
    [OutputType(typeof(PSObject))]
    public class TestCacheValidityCommand : PSCmdlet
    {
        /// <summary>
        /// Caching service instance
        /// </summary>
        private static CachingService? _cachingService;

        /// <summary>
        /// Cache invalidation service instance
        /// </summary>
        private static CacheInvalidationService? _invalidationService;

        /// <summary>
        /// Process record - validate all watched files
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                _cachingService ??= new CachingService();

                _invalidationService ??= _cachingService.GetInvalidationService(null);

                if (_invalidationService == null)
                {
                    WriteObject("No files are currently being watched");
                    return;
                }

                bool allValid = _invalidationService.ValidateAllCacheEntries();

                var result = new PSObject();
                result.Properties.Add(new PSNoteProperty("AllValid", allValid));
                result.Properties.Add(new PSNoteProperty("Timestamp", DateTime.UtcNow));

                if (allValid)
                {
                    WriteObject("All cache entries are valid", false);
                }
                else
                {
                    WriteWarning("Cache has been invalidated due to file changes");
                }

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "ValidateCacheError",
                    ErrorCategory.OperationStopped,
                    null
                ));
            }
        }
    }
}
