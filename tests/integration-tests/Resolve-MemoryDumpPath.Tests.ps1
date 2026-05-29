BeforeAll {
    $ModulePath = Join-Path $PSScriptRoot "..\..\PowerShell.MemoryAnalysis\publish\MemoryAnalysis.psd1"

    if (Get-Module MemoryAnalysis) {
        Remove-Module MemoryAnalysis -Force
    }

    if (-not (Test-Path $ModulePath)) {
        throw "Published module not found at $ModulePath. Run: dotnet publish PowerShell.MemoryAnalysis/PowerShell.MemoryAnalysis.csproj -c Release -o PowerShell.MemoryAnalysis/publish"
    }

    Import-Module $ModulePath -ErrorAction Stop

    function script:New-MemoryDumpTestRoot {
        $root = Join-Path ([System.IO.Path]::GetTempPath()) ("mad-pester-" + [guid]::NewGuid().ToString())
        New-Item -Path $root -ItemType Directory -Force | Out-Null
        return $root
    }

    function script:New-TestDumpFile {
        param(
            [Parameter(Mandatory)]
            [string]$Directory,
            [Parameter(Mandatory)]
            [string]$Name
        )
        $path = Join-Path $Directory $Name
        [System.IO.File]::WriteAllText($path, 'pester-test-dump-placeholder')
        return (Resolve-Path $path).Path
    }
}

AfterAll {
    if (Get-Module MemoryAnalysis) {
        Remove-Module MemoryAnalysis -Force
    }
}

Describe "Resolve-MemoryDumpPath discovery" -Tag 'Discovery', 'NoAcquire' {

    AfterEach {
        if ($script:TestRoot -and (Test-Path $script:TestRoot)) {
            Remove-Item $script:TestRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It "finds a single .raw file at search depth 0" {
        $script:TestRoot = New-MemoryDumpTestRoot
        $dumpPath = New-TestDumpFile -Directory $script:TestRoot -Name 'host.raw'

        $result = Resolve-MemoryDumpPath -SearchPath $script:TestRoot -MaxSearchDepth 0 -NoAcquire -Force

        $result.Path | Should -Be $dumpPath
    }

    It "finds a dump one subdirectory deep with default depth 1" {
        $script:TestRoot = New-MemoryDumpTestRoot
        $sub = Join-Path $script:TestRoot 'case01'
        New-Item -Path $sub -ItemType Directory -Force | Out-Null
        $dumpPath = New-TestDumpFile -Directory $sub -Name 'memory.raw'

        $result = Resolve-MemoryDumpPath -SearchPath $script:TestRoot -NoAcquire -Force

        $result.Path | Should -Be $dumpPath
    }

    It "throws when no dump exists and -NoAcquire is set" {
        $script:TestRoot = New-MemoryDumpTestRoot

        { Resolve-MemoryDumpPath -SearchPath $script:TestRoot -MaxSearchDepth 1 -NoAcquire -ErrorAction Stop } |
            Should -Throw
    }

    It "does not offer acquisition in CI when no dump exists" {
        $script:TestRoot = New-MemoryDumpTestRoot
        $previous = $env:GITHUB_ACTIONS
        $env:GITHUB_ACTIONS = 'true'
        try {
            { Resolve-MemoryDumpPath -SearchPath $script:TestRoot -MaxSearchDepth 1 -ErrorAction Stop } |
                Should -Throw
        }
        finally {
            if ($null -eq $previous) {
                Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue
            }
            else {
                $env:GITHUB_ACTIONS = $previous
            }
        }
    }

    It "excludes small .dmp minidumps from discovery" {
        $script:TestRoot = New-MemoryDumpTestRoot
        $mini = Join-Path $script:TestRoot 'wer.dmp'
        [System.IO.File]::WriteAllBytes($mini, [byte[]]::new(1024))

        { Resolve-MemoryDumpPath -SearchPath $script:TestRoot -MaxSearchDepth 0 -NoAcquire -ErrorAction Stop } |
            Should -Throw
    }

    It "accepts an explicit -Path" {
        $script:TestRoot = New-MemoryDumpTestRoot
        $dumpPath = New-TestDumpFile -Directory $script:TestRoot -Name 'explicit.vmem'

        $result = Resolve-MemoryDumpPath -Path $dumpPath

        $result.Path | Should -Be $dumpPath
    }
}

Describe "Invoke-MemoryDumpAcquisition platform guard" -Tag 'NoAcquire' {

    It "fails on non-Windows platforms" -Skip:$IsWindows {
        { Invoke-MemoryDumpAcquisition -ErrorAction Stop } |
            Should -Throw
    }
}
