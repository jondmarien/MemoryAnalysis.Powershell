@{
    # Script module or binary module file associated with this manifest
    RootModule = 'PowerShell.MemoryAnalysis.dll'
    
    # Version number of this module
    ModuleVersion = '0.1.0'
    
    # Supported PSEditions
    CompatiblePSEditions = @('Core')
    
    # ID used to uniquely identify this module
    GUID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    
    # Author of this module
    Author = 'Memory Analysis Team'
    
    # Company or vendor of this module
    CompanyName = 'Unknown'
    
    # Copyright statement for this module
    Copyright = '(c) 2025. All rights reserved.'
    
    # Description of the functionality provided by this module
    Description = 'PowerShell module for memory dump analysis using Volatility 3 framework with high-performance Rust/Python bridge. Note: Network scanning and malware detection are not supported on Windows 11 Build 26100 due to Volatility 3 compatibility issues.'
    
    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '7.6.0'
    
    # Minimum version of the .NET Framework required by this module
    DotNetFrameworkVersion = '10.0'
    
    # Functions to export from this module
    FunctionsToExport = @()
    
    # Cmdlets to export from this module
    CmdletsToExport = @(
        'Get-MemoryDump'
        'Test-ProcessTree'
        'Get-ProcessCommandLine'
        'Get-ProcessDll'
        'Get-CacheInfo'
        'Clear-Cache'
        'Watch-MemoryDumpFile'
        'Stop-WatchingMemoryDumpFile'
        'Get-WatchedMemoryDumpFiles'
        'Test-CacheValidity'
        # 'Get-NetworkConnection'  # Disabled: Not compatible with Windows 11 Build 26100
        # 'Find-Malware'           # Disabled: Not compatible with Windows 11 Build 26100
    )
    
    # Variables to export from this module
    VariablesToExport = @()
    
    # Aliases to export from this module
    AliasesToExport = @(
        'Analyze-ProcessTree'
    )
    
    # List of all files packaged with this module
    FileList = @(
        'PowerShell.MemoryAnalysis.dll'
        'rust_bridge.dll'
        'MemoryAnalysis.psd1'
        'MemoryAnalysis.Format.ps1xml'
    )
    
    # Private data to pass to the module specified in RootModule
    PrivateData = @{
        PSData = @{
            # Tags applied to this module
            Tags = @(
                'Memory'
                'Forensics'
                'Volatility'
                'Security'
                'Malware'
                'Incident-Response'
                'DFIR'
            )
            
            # A URL to the license for this module
            LicenseUri = ''
            
            # A URL to the main website for this project
            ProjectUri = 'https://github.com/jondmarien/MemoryAnalysis.Powershell'
            
            # A URL to an icon representing this module
            IconUri = ''
            
            # ReleaseNotes of this module
            ReleaseNotes = @'
Version 0.1.0
- Initial release
- Get-MemoryDump cmdlet for loading memory dumps
- Test-ProcessTree cmdlet for process hierarchy analysis
- Get-ProcessCommandLine cmdlet for command line extraction
- Get-ProcessDll cmdlet for DLL enumeration
- Integration with Volatility 3 framework
- High-performance Rust/Python bridge
- Note: Network and malware scanning disabled for Windows 11 Build 26100 compatibility
'@
            
            # Prerelease string of this module
            Prerelease = 'preview'
            
            # Flag to indicate whether the module requires explicit user acceptance
            RequireLicenseAcceptance = $false
            
            # External dependent modules of this module
            ExternalModuleDependencies = @()
        }
    }
    
    # HelpInfo URI of this module
    HelpInfoURI = ''
    
    # Default prefix for commands exported from this module
    DefaultCommandPrefix = ''
}
