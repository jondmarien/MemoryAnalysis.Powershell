// Quick test to see what's failing with Volatility
use std::path::Path;

fn main() {
    // Simulating what our code does
    let dump_path = r"J:\projects\personal-projects\MemoryAnalysis\samples\dumps\preview.DMP";
    
    if !Path::new(dump_path).exists() {
        eprintln!("Dump file doesn't exist!");
        return;
    }
    
    println!("Testing Volatility with: {}", dump_path);
    
    // Initialize Python (simulating our code)
    pyo3::prepare_freethreaded_python();
    
    pyo3::Python::attach(|py| {
        println!("Python initialized");
        
        // Try importing Volatility
        match py.import("volatility3.framework.contexts") {
            Ok(_) => println!("✓ Imported volatility3.framework.contexts"),
            Err(e) => {
                eprintln!("✗ Failed to import contexts: {}", e);
                return;
            }
        }
        
        match py.import("volatility3.plugins.windows.pslist") {
            Ok(_) => println!("✓ Imported volatility3.plugins.windows.pslist"),
            Err(e) => {
                eprintln!("✗ Failed to import pslist: {}", e);
                return;
            }
        }
        
        println!("All imports successful!");
    })
}