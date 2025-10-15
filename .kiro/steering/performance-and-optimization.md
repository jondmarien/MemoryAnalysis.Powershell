---
inclusion: always
---

# Performance and Optimization Standards

## Performance Targets

### Response Time Targets

- **Rust-Python FFI overhead**: < 100ms per call
- **Memory dump load (4GB)**: < 30 seconds
- **Process tree analysis**: < 5 seconds
- **Malware scan (quick)**: < 30 seconds
- **Malware scan (full)**: < 60 seconds
- **Cached operations**: < 2 seconds

### Memory Usage Targets

- **Base module overhead**: < 50MB
- **Per loaded dump overhead**: < 1GB
- **Maximum concurrent dumps**: 10
- **Memory leak tolerance**: 0 (zero leaks acceptable)

### Throughput Targets

- **Parallel dump processing**: 4+ concurrent dumps
- **Process analysis throughput**: 1000+ processes/second
- **Plugin execution**: 10+ plugins in parallel

## Rust Performance Optimization

### Avoid Unnecessary Allocations

```rust
// Good: Use string slices
pub fn get_process_name(info: &ProcessInfo) -> &str {
    &info.name
}

// Bad: Unnecessary allocation
pub fn get_process_name_bad(info: &ProcessInfo) -> String {
    info.name.clone()
}

// Good: Reuse buffers
pub fn process_multiple_dumps(paths: &[&str]) -> Result<Vec<Analysis>> {
    let mut results = Vec::with_capacity(paths.len());
    for path in paths {
        results.push(analyze_dump(path)?);
    }
    Ok(results)
}
```

### Use Efficient Data Structures

```rust
use std::collections::HashMap;

// Pre-allocate when size is known
let mut process_map = HashMap::with_capacity(expected_count);

// Use appropriate collection types
use std::collections::BTreeMap;  // For sorted keys
use std::collections::HashSet;   // For unique values
use smallvec::SmallVec;          // For small vectors on stack
```

### Minimize Python GIL Contention

```rust
use pyo3::prelude::*;

pub fn analyze_multiple_dumps(paths: &[String]) -> Result<Vec<Analysis>> {
    // Prepare data outside GIL
    let prepared_paths: Vec<_> = paths
        .iter()
        .map(|p| prepare_path(p))
        .collect();
    
    // Acquire GIL once for all operations
    Python::with_gil(|py| {
        let results: Result<Vec<_>> = prepared_paths
            .iter()
            .map(|path| analyze_with_python(py, path))
            .collect();
        results
    })
}
```

### Use Lazy Initialization

```rust
use once_cell::sync::Lazy;

static PYTHON_INITIALIZED: Lazy<()> = Lazy::new(|| {
    pyo3::prepare_freethreaded_python();
});

pub fn ensure_python() {
    Lazy::force(&PYTHON_INITIALIZED);
}
```

### Optimize Serialization

```rust
use serde::Serialize;

// Use borrowed data when possible
#[derive(Serialize)]
pub struct ProcessInfoRef<'a> {
    pub pid: u32,
    pub name: &'a str,
    pub command_line: &'a str,
}

// Serialize directly to writer
pub fn write_processes_json<W: Write>(
    writer: W,
    processes: &[ProcessInfo]
) -> Result<()> {
    serde_json::to_writer(writer, processes)?;
    Ok(())
}
```

## C# Performance Optimization

### Minimize Allocations

```csharp
// Good: Use Span<T> for buffers
public void ProcessBuffer(ReadOnlySpan<byte> buffer)
{
    // Process without allocation
}

// Good: Use StringBuilder for string concatenation
var sb = new StringBuilder(capacity: 1000);
foreach (var process in processes)
{
    sb.AppendLine($"{process.Pid}: {process.Name}");
}
return sb.ToString();

// Bad: String concatenation in loop
string result = "";
foreach (var process in processes)
{
    result += $"{process.Pid}: {process.Name}\n";  // Allocates each iteration
}
```

### Use Object Pooling

```csharp
using Microsoft.Extensions.ObjectPool;

public class ProcessAnalyzer
{
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;
    
    public ProcessAnalyzer()
    {
        var provider = new DefaultObjectPoolProvider();
        _stringBuilderPool = provider.CreateStringBuilderPool();
    }
    
    public string FormatProcesses(IEnumerable<ProcessInfo> processes)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            foreach (var process in processes)
            {
                sb.AppendLine($"{process.Pid}: {process.Name}");
            }
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }
}
```

### Optimize P/Invoke Calls

```csharp
// Cache delegate to avoid repeated lookups
private static class NativeMethods
{
    private static readonly Lazy<AnalyzeProcessesDelegate> _analyzeProcesses =
        new Lazy<AnalyzeProcessesDelegate>(() =>
            Marshal.GetDelegateForFunctionPointer<AnalyzeProcessesDelegate>(
                GetProcAddress("analyze_processes")
            )
        );
    
    public static AnalyzeProcessesDelegate AnalyzeProcesses =>
        _analyzeProcesses.Value;
}

// Reuse marshaled strings
private readonly Dictionary<string, IntPtr> _stringCache = new();

private IntPtr GetCachedString(string str)
{
    if (!_stringCache.TryGetValue(str, out var ptr))
    {
        ptr = Marshal.StringToHGlobalAnsi(str);
        _stringCache[str] = ptr;
    }
    return ptr;
}
```

### Use ValueTask for Async Operations

```csharp
// Good: Use ValueTask for frequently synchronous operations
public ValueTask<ProcessInfo[]> GetProcessesAsync(string dumpPath)
{
    if (_cache.TryGetValue(dumpPath, out var cached))
    {
        return new ValueTask<ProcessInfo[]>(cached);
    }
    
    return new ValueTask<ProcessInfo[]>(LoadProcessesAsync(dumpPath));
}

// Bad: Always allocate Task
public Task<ProcessInfo[]> GetProcessesAsync(string dumpPath)
{
    if (_cache.TryGetValue(dumpPath, out var cached))
    {
        return Task.FromResult(cached);  // Allocates
    }
    
    return LoadProcessesAsync(dumpPath);
}
```

## Caching Strategies

### Memory Dump Metadata Cache

```csharp
public class MemoryDumpCache
{
    private readonly LruCache<string, MemoryDumpMetadata> _cache;
    
    public MemoryDumpCache(int maxSize = 100)
    {
        _cache = new LruCache<string, MemoryDumpMetadata>(maxSize);
    }
    
    public MemoryDumpMetadata GetOrLoad(string path)
    {
        if (_cache.TryGetValue(path, out var metadata))
        {
            return metadata;
        }
        
        metadata = LoadMetadata(path);
        _cache.Add(path, metadata);
        return metadata;
    }
}
```

### Process Analysis Cache

```rust
use lru::LruCache;
use std::sync::Mutex;

pub struct ProcessCache {
    cache: Mutex<LruCache<String, Vec<ProcessInfo>>>,
}

impl ProcessCache {
    pub fn new(capacity: usize) -> Self {
        Self {
            cache: Mutex::new(LruCache::new(capacity)),
        }
    }
    
    pub fn get_or_analyze(
        &self,
        dump_path: &str
    ) -> Result<Vec<ProcessInfo>> {
        let mut cache = self.cache.lock().unwrap();
        
        if let Some(processes) = cache.get(dump_path) {
            return Ok(processes.clone());
        }
        
        drop(cache);  // Release lock before expensive operation
        
        let processes = analyze_processes_impl(dump_path)?;
        
        let mut cache = self.cache.lock().unwrap();
        cache.put(dump_path.to_string(), processes.clone());
        
        Ok(processes)
    }
}
```

### Cache Invalidation

```csharp
public class SmartCache
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    
    private class CacheEntry
    {
        public object Value { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CachedAt { get; set; }
    }
    
    public T GetOrLoad<T>(string path, Func<string, T> loader)
    {
        if (_cache.TryGetValue(path, out var entry))
        {
            var fileInfo = new FileInfo(path);
            
            // Invalidate if file changed
            if (fileInfo.LastWriteTime <= entry.LastModified)
            {
                return (T)entry.Value;
            }
        }
        
        var value = loader(path);
        _cache[path] = new CacheEntry
        {
            Value = value,
            LastModified = new FileInfo(path).LastWriteTime,
            CachedAt = DateTime.UtcNow
        };
        
        return value;
    }
}
```

## Parallel Processing

### Rust Parallel Processing

```rust
use rayon::prelude::*;

pub fn analyze_multiple_dumps_parallel(
    paths: &[String]
) -> Vec<Result<Analysis>> {
    paths
        .par_iter()
        .map(|path| analyze_dump(path))
        .collect()
}

// With progress reporting
pub fn analyze_with_progress(
    paths: &[String],
    progress_callback: impl Fn(usize, usize) + Sync
) -> Vec<Result<Analysis>> {
    use std::sync::atomic::{AtomicUsize, Ordering};
    
    let completed = AtomicUsize::new(0);
    let total = paths.len();
    
    paths
        .par_iter()
        .map(|path| {
            let result = analyze_dump(path);
            let count = completed.fetch_add(1, Ordering::Relaxed) + 1;
            progress_callback(count, total);
            result
        })
        .collect()
}
```

### PowerShell Parallel Processing

```csharp
protected override void ProcessRecord()
{
    if (Parallel.IsPresent)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = ThrottleLimit,
            CancellationToken = CancellationToken
        };
        
        Parallel.ForEach(DumpPaths, options, path =>
        {
            try
            {
                var result = _rustInterop.AnalyzeProcesses(path);
                
                // Thread-safe output
                lock (_outputLock)
                {
                    WriteObject(result);
                }
            }
            catch (Exception ex)
            {
                lock (_errorLock)
                {
                    WriteError(CreateErrorRecord(ex, path));
                }
            }
        });
    }
    else
    {
        // Sequential processing
        foreach (var path in DumpPaths)
        {
            var result = _rustInterop.AnalyzeProcesses(path);
            WriteObject(result);
        }
    }
}
```

## Memory Management

### Rust Memory Management

```rust
// Use RAII for automatic cleanup
pub struct MemoryDump {
    data: Vec<u8>,
    handle: Option<FileHandle>,
}

impl Drop for MemoryDump {
    fn drop(&mut self) {
        if let Some(handle) = self.handle.take() {
            // Cleanup resources
            handle.close();
        }
    }
}

// Use Rc/Arc only when necessary
use std::rc::Rc;
use std::sync::Arc;

// Single-threaded: use Rc
let shared = Rc::new(data);

// Multi-threaded: use Arc
let shared = Arc::new(data);
```

### C# Memory Management

```csharp
public class MemoryDump : IDisposable
{
    private IntPtr _nativeHandle;
    private bool _disposed;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            // Dispose managed resources
        }
        
        // Free unmanaged resources
        if (_nativeHandle != IntPtr.Zero)
        {
            NativeMethods.FreeMemoryDump(_nativeHandle);
            _nativeHandle = IntPtr.Zero;
        }
        
        _disposed = true;
    }
    
    ~MemoryDump()
    {
        Dispose(false);
    }
}
```

### Memory Pressure Hints

```csharp
public class LargeMemoryDump : IDisposable
{
    private byte[] _data;
    
    public LargeMemoryDump(long size)
    {
        _data = new byte[size];
        
        // Inform GC about memory pressure
        GC.AddMemoryPressure(size);
    }
    
    public void Dispose()
    {
        if (_data != null)
        {
            GC.RemoveMemoryPressure(_data.Length);
            _data = null;
        }
    }
}
```

## Profiling and Benchmarking

### Rust Profiling

```rust
// Use criterion for benchmarks
#[cfg(test)]
mod benches {
    use criterion::{black_box, Criterion};
    
    pub fn benchmark_process_analysis(c: &mut Criterion) {
        c.bench_function("analyze_processes", |b| {
            b.iter(|| {
                analyze_processes(black_box("test.vmem"))
            });
        });
    }
}

// Use flamegraph for profiling
// cargo install flamegraph
// cargo flamegraph --bin your_binary
```

### C# Profiling

```csharp
using System.Diagnostics;

public class PerformanceMonitor
{
    public static void MeasureOperation(
        string name,
        Action operation)
    {
        var sw = Stopwatch.StartNew();
        var memBefore = GC.GetTotalMemory(false);
        
        operation();
        
        sw.Stop();
        var memAfter = GC.GetTotalMemory(false);
        
        Console.WriteLine($"{name}:");
        Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Memory: {(memAfter - memBefore) / 1024}KB");
    }
}
```

## Performance Testing

### Load Testing

```powershell
# Test with multiple concurrent dumps
$dumps = Get-ChildItem *.vmem
$results = $dumps | ForEach-Object -Parallel {
    Measure-Command {
        Get-MemoryDump -Path $_.FullName | Test-ProcessTree
    }
} -ThrottleLimit 4

$results | Measure-Object -Property TotalMilliseconds -Average -Maximum
```

### Memory Leak Detection

```rust
#[cfg(test)]
mod tests {
    #[test]
    fn test_no_memory_leak() {
        let initial = get_memory_usage();
        
        for _ in 0..1000 {
            let _ = analyze_processes("test.vmem");
        }
        
        let final_mem = get_memory_usage();
        let leak = final_mem - initial;
        
        assert!(leak < 1_000_000, "Memory leak detected: {} bytes", leak);
    }
}
```

## Optimization Checklist

### Before Optimizing

- [ ] Profile to identify bottlenecks
- [ ] Measure current performance
- [ ] Set specific performance targets
- [ ] Identify critical paths

### Rust Optimizations

- [ ] Minimize allocations
- [ ] Use appropriate data structures
- [ ] Reduce GIL contention
- [ ] Enable LTO in release builds
- [ ] Use `#[inline]` for hot functions
- [ ] Consider unsafe code for critical sections (with safety proof)

### C# Optimizations

- [ ] Use Span<T> and Memory<T>
- [ ] Minimize P/Invoke overhead
- [ ] Cache frequently used data
- [ ] Use ValueTask for async
- [ ] Optimize LINQ queries
- [ ] Consider stackalloc for small buffers

### After Optimizing

- [ ] Verify performance improvement
- [ ] Ensure correctness maintained
- [ ] Update benchmarks
- [ ] Document optimization rationale
