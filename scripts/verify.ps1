<#
.SYNOPSIS
Runs every quality gate of Tasketier and prints one line per gate.

.DESCRIPTION
Gates: Build (analyzers, code style and nullable run inside it, warnings fail it), Unit tests with line
and branch coverage, Formatting, Cyclomatic complexity. The script continues past a failed gate and
exits non-zero when any gate is FAIL or NOT VERIFIED. A tool that is missing gives NOT VERIFIED, never a
guessed result.

Needs the .NET SDK (10.0.400 or any SDK that opens .slnx) and lizard (pip install lizard==1.17.31).
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Tasketier.slnx'
$testResultsFolders = @((Join-Path $root 'tests\Tasketier.Core.Tests\TestResults'), (Join-Path $root 'tests\Tasketier.Cli.Tests\TestResults'))
$results = [ordered]@{}

function Invoke-Gate([string]$Name, [string]$Exe, [scriptblock]$Command) {
    if (-not $Exe) {
        $results[$Name] = 'NOT VERIFIED (tool not found)'
        return
    }
    Write-Host "== $Name"
    & $Command $Exe
    $results[$Name] = if ($LASTEXITCODE -eq 0) { 'PASS' } else { 'FAIL' }
}

function Find-Tool([string]$Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$dotnet = Find-Tool 'dotnet'
$lizard = Find-Tool 'lizard'

Push-Location $root
try {
    foreach ($folder in $testResultsFolders) { if (Test-Path $folder) { Remove-Item -Recurse -Force $folder } }  # no stale coverage

    Invoke-Gate 'Build' $dotnet { param($t) & $t build $solution -warnaserror --nologo -v q }
    Invoke-Gate 'Unit tests' $dotnet {
        param($t) & $t test $solution --no-build --nologo -v q --collect 'XPlat Code Coverage'
    }
    $results['Integration tests'] = 'N/A (the MCP round trip runs the real exe inside Unit tests; the windows are checked by hand, see README)'
    $results['Lint'] = 'N/A (analyzers and code style run in the build; reported on Build)'
    Invoke-Gate 'Formatting' $dotnet { param($t) & $t format $solution --verify-no-changes --no-restore }
    $results['Static type check'] = 'N/A (compiler nullable analysis; reported on Build)'
    $results['Static analysis'] = 'N/A (Roslyn analyzers at AnalysisLevel latest-recommended; reported on Build)'
    # Generated XAML code (obj\*.g.cs) is not ours; only the sources count.
    Invoke-Gate 'Cyclomatic complexity' $lizard { param($t) & $t -l csharp -C 9 -w -x '*/obj/*' -x '*/bin/*' (Join-Path $root 'src') }

    # One cobertura file per test project, reported per project: Core and Cli hold the logic and are covered
    # by their tests; App holds the WPF windows and Win32 adapters, which the manual checks cover.
    $reports = @($testResultsFolders | Where-Object { Test-Path $_ } | ForEach-Object { Get-ChildItem -Path $_ -Filter 'coverage.cobertura.xml' -Recurse })
    if ($reports.Count -gt 0) {
        $lineParts = @(); $branchParts = @()
        foreach ($report in $reports) {
            $xml = [xml](Get-Content $report.FullName)
            $name = if ($report.FullName -match 'Tasketier[.](\w+)[.]Tests') { $Matches[1] } else { $report.Directory.Name }
            $lineParts += ('{0} {1} % ({2} of {3} lines)' -f $name, [math]::Round(100 * [double]$xml.coverage.'line-rate', 1), $xml.coverage.'lines-covered', $xml.coverage.'lines-valid')
            $branchParts += ('{0} {1} % ({2} of {3} branches)' -f $name, [math]::Round(100 * [double]$xml.coverage.'branch-rate', 1), $xml.coverage.'branches-covered', $xml.coverage.'branches-valid')
        }
        $results['Line coverage'] = $lineParts -join '; '
        $results['Branch coverage'] = $branchParts -join '; '
    } else {
        $results['Line coverage'] = 'NOT VERIFIED (no coverage.cobertura.xml)'
        $results['Branch coverage'] = 'NOT VERIFIED (no coverage.cobertura.xml)'
    }
}
finally {
    Pop-Location
}

Write-Host ''
$failed = $false
foreach ($entry in $results.GetEnumerator()) {
    Write-Host ('{0,-23}{1}' -f ($entry.Key + ':'), $entry.Value)
    if ($entry.Value -like 'FAIL*' -or $entry.Value -like 'NOT VERIFIED*') { $failed = $true }
}
if ($failed) { exit 1 }
exit 0
