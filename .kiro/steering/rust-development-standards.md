---
inclusion: fileMatch
fileMatchPattern: "rust-bridge/**/*.rs"
---

# Rust Development Standards

## Code Organization

### Module Structure

All Rust code lives in `rust-bridge/src/`:

```tree
rust-bridge/src/
├── lib.rs                 # FFI exports, main entry point
├── python_manager.rs      # Python interpreter lifecycle
├── volatility.rs          # Volatility 3 wrapper
├── process_analysis.rs    # Process tree analysis
├── malware_detection.rs   # Malware scanning
├── memory_dump.rs         # Memory dump operations
├── types.rs               # Shared data structures
├── serialization.rs       # Data marshaling
└── error.rs               # Error handling
```

## PyO3 Integration Patterns

### Python Interpreter Management

**ALWAYS use singleton pattern with lazy initialization:**

```rust
use pyo3::prelude::*;
use std::sync::Once;

static INIT: Once = Once::new();

pub fn ensure_python_initialized() {
    INIT.call_once(|| {
        pyo3::prepare_freethreaded_python();
    });
}
```

### GIL Management

**ALWAYS acquire GIL before Python operations:**

```rust
Python::with_gil(|py| {
    // Python operations here
    let result = py.import("volatility3.framework")?;
    // ...
});
```

### Error Handling

**Use `anyhow::Result` for all fallible operations:**

```rust
use anyhow::{Result, Context};

pub fn load_memory_dump(path: &str) -> Result<MemoryDump> {
    Python::with_gil(|py| {
        let vol = py.import("volatility3.framework")
            .context("Failed to import Volatility framework")?;
        // ...
    })
}
```

## FFI Export Standards

### C-Compatible Functions

**All FFI exports MUST:**
- Use `#[no_mangle]`
- Use `extern "C"` calling convention
- Return C-compatible types or pointers
- Handle panics with `catch_unwind`

```rust
#[no_mangle]
pub extern "C" fn analyze_processes(
    dump_path: *const c_char,
    out_json: *mut *mut c_char,
    out_len: *mut usize
) -> i32 {
    std::panic::catch_unwind(|| {
        // Implementation
    }).unwrap_or(-1)
}
```

### Memory Safety

**ALWAYS:**
- Validate pointer arguments are non-null
- Use `CString` for string conversions
- Free allocated memory with matching deallocation function
- Document ownership transfer in comments

```rust
/// Caller must free returned pointer with `free_string()`
#[no_mangle]
pub extern "C" fn get_process_list(/* ... */) -> *mut c_char {
    // ...
}

#[no_mangle]
pub extern "C" fn free_string(ptr: *mut c_char) {
    if !ptr.is_null() {
        unsafe { CString::from_raw(ptr) };
    }
}
```

## Data Serialization

### Serde Integration

**Use Serde for all data structures:**

```rust
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct ProcessInfo {
    pub pid: u32,
    pub ppid: u32,
    pub name: String,
    pub command_line: String,
    pub create_time: String,
    pub threads: u32,
    pub handles: u32,
}
```

### JSON Conversion

**Provide JSON serialization for C# interop:**

```rust
pub fn processes_to_json(processes: &[ProcessInfo]) -> Result<String> {
    serde_json::to_string(processes)
        .context("Failed to serialize processes to JSON")
}
```

## Error Handling Patterns

### Custom Error Types

```rust
use thiserror::Error;

#[derive(Error, Debug)]
pub enum VolatilityError {
    #[error("Python error: {0}")]
    PythonError(String),
    
    #[error("Memory dump not found: {0}")]
    DumpNotFound(String),
    
    #[error("Invalid dump format: {0}")]
    InvalidFormat(String),
}
```

### Error Propagation

**Use `?` operator and context:**

```rust
pub fn analyze_dump(path: &str) -> Result<Analysis> {
    let dump = load_dump(path)
        .context("Failed to load memory dump")?;
    
    let processes = extract_processes(&dump)
        .context("Failed to extract process list")?;
    
    Ok(Analysis { processes })
}
```

## Testing Standards

### Unit Tests

**ALWAYS include unit tests in the same file:**

```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_process_info_serialization() {
        let process = ProcessInfo {
            pid: 1234,
            ppid: 100,
            name: "test.exe".to_string(),
            // ...
        };
        
        let json = serde_json::to_string(&process).unwrap();
        assert!(json.contains("1234"));
    }
}
```

### Integration Tests

**Place in `rust-bridge/tests/`:**

```rust
// tests/volatility_integration.rs
use rust_bridge::*;

#[test]
fn test_full_analysis_workflow() {
    let result = analyze_processes("test_data/mini_dump.raw");
    assert!(result.is_ok());
}
```

## Performance Considerations

### Avoid Unnecessary Allocations

```rust
// Good: Use string slices
pub fn process_name(info: &ProcessInfo) -> &str {
    &info.name
}

// Avoid: Unnecessary cloning
pub fn process_name_bad(info: &ProcessInfo) -> String {
    info.name.clone()  // Unnecessary allocation
}
```

### Use Efficient Data Structures

```rust
use std::collections::HashMap;

// Pre-allocate with capacity when size is known
let mut cache = HashMap::with_capacity(expected_size);
```

## Documentation Standards

### Public API Documentation

**ALWAYS document public functions:**

```rust
/// Analyzes process tree from a memory dump.
///
/// # Arguments
///
/// * `dump_path` - Path to the memory dump file
/// * `filter` - Optional process name filter
///
/// # Returns
///
/// Returns a vector of `ProcessInfo` structures on success.
///
/// # Errors
///
/// Returns error if:
/// - Dump file cannot be read
/// - Volatility framework fails to initialize
/// - Process extraction fails
///
/// # Example
///
/// ```
/// let processes = analyze_processes("/path/to/dump.vmem", None)?;
/// ```
pub fn analyze_processes(
    dump_path: &str,
    filter: Option<&str>
) -> Result<Vec<ProcessInfo>> {
    // ...
}
```

## Dependencies Management

### Cargo.toml Standards

```toml
[package]
name = "rust-bridge"
version = "0.1.0"
edition = "2021"

[lib]
crate-type = ["cdylib", "rlib"]

[dependencies]
pyo3 = { version = "0.20", features = ["auto-initialize"] }
serde = { version = "1.0", features = ["derive"] }
serde_json = "1.0"
anyhow = "1.0"
thiserror = "1.0"
tokio = { version = "1.0", features = ["full"] }

[dev-dependencies]
tempfile = "3.0"
```

## Build Configuration

### build.rs Standards

```rust
// build.rs
fn main() {
    // Link Python libraries
    println!("cargo:rustc-link-lib=python3.11");
    
    // Platform-specific configuration
    #[cfg(target_os = "windows")]
    println!("cargo:rustc-link-search=C:\\Python311\\libs");
}
```

## Code Style

- Use `rustfmt` for formatting (run `cargo fmt`)
- Use `clippy` for linting (run `cargo clippy`)
- Follow Rust naming conventions:
  - `snake_case` for functions and variables
  - `PascalCase` for types and traits
  - `SCREAMING_SNAKE_CASE` for constants
