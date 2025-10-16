using System;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using PowerShell.MemoryAnalysis.Services;

namespace PowerShell.MemoryAnalysis.Cmdlets
{
    /// <summary>
    /// Clear-Cache cmdlet - Clear memory analysis cache
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "Cache", SupportsShouldProcess = true)]
    public class ClearCacheCommand : PSCmdlet
    {
        private ILogger<ClearCacheCommand>? _logger;
        private static CachingService? _cachingService;

        /// <summary>
        /// Whether to confirm cache clearing
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Initialize the cmdlet
        /// </summary>
        protected override void BeginProcessing()
        {
            _logger = LoggingService.GetLogger<ClearCacheCommand>();
            _logger?.LogInformation("Clear-Cache cmdlet started");

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
                // Check for WhatIf or Confirm
                if (!ShouldProcess("all memory analysis caches", "clear"))
                {
                    WriteVerbose("Cache clearing cancelled by user");
                    return;
                }

                WriteVerbose("Clearing all caches");
                _cachingService?.ClearAllCaches();

                WriteObject("Cache cleared successfully");
                _logger?.LogInformation("Cache cleared successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing cache");

                var error = new ErrorRecord(
                    ex,
                    "CacheClearError",
                    ErrorCategory.InvalidOperation,
                    null
                )
                {
                    ErrorDetails = new ErrorDetails(
                        $"Failed to clear cache: {ex.Message}"
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
            _logger?.LogInformation("Clear-Cache cmdlet completed");
        }
    }
}
