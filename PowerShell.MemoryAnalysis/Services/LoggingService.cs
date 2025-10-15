using Microsoft.Extensions.Logging;
using System;

namespace PowerShell.MemoryAnalysis.Services;

/// <summary>
/// Centralized logging service for the Memory Analysis module.
/// Provides consistent logging across all cmdlets and services.
/// </summary>
public static class LoggingService
{
    private static ILoggerFactory? _loggerFactory;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Initialize the logging service with optional configuration.
    /// </summary>
    /// <param name="configureLogging">Optional logging configuration action</param>
    public static void Initialize(Action<ILoggingBuilder>? configureLogging = null)
    {
        lock (_lock)
        {
            if (_loggerFactory != null)
            {
                return; // Already initialized
            }

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole()
                    .AddDebug();

                configureLogging?.Invoke(builder);
            });
        }
    }

    /// <summary>
    /// Get a logger for the specified type.
    /// </summary>
    /// <typeparam name="T">The type requesting the logger</typeparam>
    /// <returns>An ILogger instance for the specified type</returns>
    public static ILogger<T> GetLogger<T>()
    {
        if (_loggerFactory == null)
        {
            Initialize();
        }

        return _loggerFactory!.CreateLogger<T>();
    }

    /// <summary>
    /// Get a logger with the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger</param>
    /// <returns>An ILogger instance</returns>
    public static ILogger GetLogger(string categoryName)
    {
        if (_loggerFactory == null)
        {
            Initialize();
        }

        return _loggerFactory!.CreateLogger(categoryName);
    }

    /// <summary>
    /// Dispose of the logger factory and release resources.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            _loggerFactory?.Dispose();
            _loggerFactory = null;
        }
    }
}
