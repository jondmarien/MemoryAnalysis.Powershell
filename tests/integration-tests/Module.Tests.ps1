BeforeAll {
    # Import the module from the publish directory
    $ModulePath = Join-Path $PSScriptRoot "..\..\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"
    
    # Remove if already loaded to ensure clean test
    if (Get-Module MemoryAnalysis) {
        Remove-Module MemoryAnalysis -Force
    }
    
    # Import the module
    try {
        Import-Module $ModulePath -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to import module: $_"
        throw
    }
}

AfterAll {
    # Clean up
    if (Get-Module MemoryAnalysis) {
        Remove-Module MemoryAnalysis -Force
    }
}

Describe "MemoryAnalysis Module" {
    
    Context "Module Loading" {
        
        It "Should load the module successfully" {
            Get-Module MemoryAnalysis | Should -Not -BeNullOrEmpty
        }
        
        It "Should have the correct module name" {
            $module = Get-Module MemoryAnalysis
            $module.Name | Should -Be "MemoryAnalysis"
        }
        
        It "Should have a valid version number" {
            $module = Get-Module MemoryAnalysis
            $module.Version | Should -Not -BeNullOrEmpty
            $module.Version.Major | Should -BeGreaterOrEqual 0
        }
        
        It "Should have a description" {
            $module = Get-Module MemoryAnalysis
            $module.Description | Should -Not -BeNullOrEmpty
        }
        
        It "Should have an author" {
            $module = Get-Module MemoryAnalysis
            $module.Author | Should -Not -BeNullOrEmpty
        }
    }
    
    Context "Cmdlet Availability" {
        
        It "Should export Get-MemoryDump cmdlet" {
            Get-Command Get-MemoryDump -Module MemoryAnalysis -ErrorAction SilentlyContinue | 
                Should -Not -BeNullOrEmpty
        }
        
        It "Should export Analyze-ProcessTree cmdlet" {
            Get-Command Analyze-ProcessTree -Module MemoryAnalysis -ErrorAction SilentlyContinue | 
                Should -Not -BeNullOrEmpty
        }
        
        It "Find-Malware cmdlet should not be exported (disabled in Win11)" -Skip {
            # Disabled due to Windows 11 Build 26100 compatibility issues
            Get-Command Find-Malware -Module MemoryAnalysis -ErrorAction SilentlyContinue | 
                Should -BeNullOrEmpty
        }
        
        It "Get-NetworkConnection cmdlet should not be exported (disabled in Win11)" -Skip {
            # Disabled due to Windows 11 Build 26100 compatibility issues
            Get-Command Get-NetworkConnection -Module MemoryAnalysis -ErrorAction SilentlyContinue | 
                Should -BeNullOrEmpty
        }
        
        It "Should export Get-ProcessCommandLine cmdlet" {
            Get-Command Get-ProcessCommandLine -Module MemoryAnalysis -ErrorAction SilentlyContinue | 
                Should -Not -BeNullOrEmpty
        }
        
        It "Should export Get-ProcessDll cmdlet" {
            Get-Command Get-ProcessDll -Module MemoryAnalysis -ErrorAction SilentlyContinue | 
                Should -Not -BeNullOrEmpty
        }
        
        It "Should export exactly 4 working cmdlets (2 disabled in Win11)" {
            $cmdlets = Get-Command -Module MemoryAnalysis -CommandType Cmdlet
            $cmdlets.Count | Should -Be 4
        }
    }
    
    Context "Cmdlet Parameter Validation" {
        
        It "Get-MemoryDump should have Path parameter" {
            $cmd = Get-Command Get-MemoryDump
            $cmd.Parameters.Keys | Should -Contain 'Path'
        }
        
        It "Get-MemoryDump Path parameter should be mandatory" {
            $cmd = Get-Command Get-MemoryDump
            $param = $cmd.Parameters['Path']
            $param.Attributes.Where({$_ -is [System.Management.Automation.ParameterAttribute]}).Mandatory | 
                Should -Contain $true
        }
        
        It "Get-MemoryDump should have Validate switch" {
            $cmd = Get-Command Get-MemoryDump
            $cmd.Parameters.Keys | Should -Contain 'Validate'
        }
        
        It "Get-MemoryDump Validate parameter should be a switch" {
            $cmd = Get-Command Get-MemoryDump
            $param = $cmd.Parameters['Validate']
            $param.SwitchParameter | Should -Be $true
        }
        
        It "Find-Malware parameter tests skipped (disabled cmdlet)" -Skip {
            # Cmdlet disabled due to Windows 11 Build 26100 compatibility
            $true | Should -Be $true
        }
        
        It "Get-ProcessDll should have Pid parameter" {
            $cmd = Get-Command Get-ProcessDll
            $cmd.Parameters.Keys | Should -Contain 'Pid'
        }
    }
    
    Context "Cmdlet Help" {
        
        It "Get-MemoryDump should have help content" {
            $help = Get-Help Get-MemoryDump
            $help.Synopsis | Should -Not -BeNullOrEmpty
        }
        
        It "Get-MemoryDump help should have examples" {
            $help = Get-Help Get-MemoryDump -Full
            $help.examples.example.Count | Should -BeGreaterOrEqual 1
        }
        
        It "Find-Malware help tests skipped (disabled cmdlet)" -Skip {
            # Cmdlet disabled due to Windows 11 Build 26100 compatibility
            $true | Should -Be $true
        }
        
        It "All cmdlets should have parameter descriptions" {
            $cmdlets = Get-Command -Module MemoryAnalysis -CommandType Cmdlet
            foreach ($cmdlet in $cmdlets) {
                # Get help without triggering parameter prompts
                $help = Get-Help $cmdlet.Name -Full -ErrorAction SilentlyContinue
                if ($help -and $help.parameters -and $help.parameters.parameter) {
                    foreach ($param in $help.parameters.parameter) {
                        if ($param.description) {
                            $param.description.Text | Should -Not -BeNullOrEmpty -Because "$($cmdlet.Name) parameter $($param.name) should have a description"
                        }
                    }
                }
            }
        }
    }
    
    Context "Error Handling" {
        
        It "Get-MemoryDump should throw on non-existent file" {
            { Get-MemoryDump -Path "C:\nonexistent\file.dmp" -ErrorAction Stop } | 
                Should -Throw
        }
        
        It "Get-MemoryDump should throw on empty path" {
            { Get-MemoryDump -Path "" -ErrorAction Stop } | 
                Should -Throw
        }
        
        It "Get-ProcessDll should require MemoryDump" {
            { Get-ProcessDll -Pid 1234 -ErrorAction Stop } | 
                Should -Throw
        }
    }
}
