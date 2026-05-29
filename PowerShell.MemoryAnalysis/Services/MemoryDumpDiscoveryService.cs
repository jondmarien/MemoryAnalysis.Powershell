using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PowerShell.MemoryAnalysis.Models;

namespace PowerShell.MemoryAnalysis.Services;

/// <summary>
/// Discovers memory dump files under a directory tree with a configurable depth.
/// </summary>
public sealed class MemoryDumpDiscoveryService
{
    /// <summary>
    /// Default search depth: current directory plus one level of subdirectories.
    /// </summary>
    public const int DefaultMaxSearchDepth = 1;

    /// <summary>
    /// Files smaller than this are treated as Windows minidumps, not full memory images.
    /// </summary>
    public const long DefaultMinDumpSizeBytes = 100L * 1024 * 1024;

    private static readonly HashSet<string> DumpExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".raw", ".vmem", ".mem", ".img", ".aff", ".bin", ".lime", ".dmp"
    };

    private readonly long _minDumpSizeBytes;

    public MemoryDumpDiscoveryService(long minDumpSizeBytes = DefaultMinDumpSizeBytes)
    {
        _minDumpSizeBytes = minDumpSizeBytes;
    }

    public IReadOnlyList<DiscoveredMemoryDump> Discover(string searchRoot, int maxSearchDepth)
    {
        if (string.IsNullOrWhiteSpace(searchRoot))
        {
            throw new ArgumentException("Search root cannot be null or empty.", nameof(searchRoot));
        }

        if (maxSearchDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSearchDepth), "Max search depth cannot be negative.");
        }

        var fullRoot = Path.GetFullPath(searchRoot);
        if (!Directory.Exists(fullRoot))
        {
            throw new DirectoryNotFoundException($"Search directory not found: {fullRoot}");
        }

        var results = new List<DiscoveredMemoryDump>();

        foreach (var file in EnumerateFilesWithDepth(fullRoot, maxSearchDepth))
        {
            if (!IsCandidateDumpFile(file))
            {
                continue;
            }

            var info = new FileInfo(file);
            results.Add(new DiscoveredMemoryDump
            {
                Path = info.FullName,
                SizeBytes = info.Length,
                LastWriteTimeUtc = info.LastWriteTimeUtc
            });
        }

        return results
            .OrderByDescending(d => d.SizeBytes)
            .ThenByDescending(d => d.LastWriteTimeUtc)
            .ToList();
    }

    /// <summary>
    /// Merges discovery under <paramref name="searchRoot"/> with scans of Collect-MemoryDump output folders.
    /// </summary>
    public IReadOnlyList<DiscoveredMemoryDump> DiscoverWithCollectMemoryDumpOutput(
        string searchRoot,
        int maxSearchDepth,
        string? collectMemoryDumpScriptPath)
    {
        var merged = new Dictionary<string, DiscoveredMemoryDump>(StringComparer.OrdinalIgnoreCase);

        foreach (var dump in Discover(searchRoot, maxSearchDepth))
        {
            merged[dump.Path] = dump;
        }

        if (!string.IsNullOrWhiteSpace(collectMemoryDumpScriptPath)
            && File.Exists(collectMemoryDumpScriptPath))
        {
            var outputRoot = CollectMemoryDumpLocator.GetCollectMemoryDumpOutputRoot(collectMemoryDumpScriptPath);
            var extraDepth = Math.Max(maxSearchDepth, 3);
            foreach (var dump in Discover(outputRoot, extraDepth))
            {
                merged[dump.Path] = dump;
            }
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// Re-scans search and script output directories after live acquisition (depth ≥ 3).
    /// </summary>
    public IReadOnlyList<DiscoveredMemoryDump> DiscoverAfterAcquisition(string searchRoot, string collectMemoryDumpScriptPath)
    {
        var depth = Math.Max(DefaultMaxSearchDepth, 3);
        var merged = new Dictionary<string, DiscoveredMemoryDump>(StringComparer.OrdinalIgnoreCase);

        foreach (var dump in Discover(searchRoot, depth))
        {
            merged[dump.Path] = dump;
        }

        var outputRoot = CollectMemoryDumpLocator.GetCollectMemoryDumpOutputRoot(collectMemoryDumpScriptPath);
        foreach (var dump in Discover(outputRoot, depth))
        {
            merged[dump.Path] = dump;
        }

        return merged.Values
            .OrderByDescending(d => d.SizeBytes)
            .ThenByDescending(d => d.LastWriteTimeUtc)
            .ToList();
    }

    public bool IsCandidateDumpFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (!DumpExtensions.Contains(extension))
        {
            return false;
        }

        if (!File.Exists(filePath))
        {
            return false;
        }

        var size = new FileInfo(filePath).Length;
        if (extension.Equals(".dmp", StringComparison.OrdinalIgnoreCase) && size < _minDumpSizeBytes)
        {
            return false;
        }

        return size > 0;
    }

    private static IEnumerable<string> EnumerateFilesWithDepth(string root, int maxDepth)
    {
        var queue = new Queue<(string Directory, int Depth)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (directory, depth) = queue.Dequeue();

            string[] files;
            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            string[] subdirs;
            try
            {
                subdirs = Directory.GetDirectories(directory);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var subdir in subdirs)
            {
                queue.Enqueue((subdir, depth + 1));
            }
        }
    }
}
