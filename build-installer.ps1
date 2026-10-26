<#
.SYNOPSIS
Publishes the Tasketier app and CLI for win-x64 without the runtime and compiles the Inno Setup installer.

.DESCRIPTION
Reads the version from Directory.Build.props unless -Version is given; -NumVersion sets the four-part
file version (default <version>.0). Writes out\publish and out\installer\Tasketier-Setup-<version>.exe,
then prints the installer path and its SHA-256.
Needs the .NET SDK and Inno Setup 6.3 or later. ISCC.exe must be on PATH or in its default install folder.
#>
param(
    [string]$Version,
    [string]$NumVersion
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not $Version) {
    $props = [xml](Get-Content (Join-Path $root 'Directory.Build.props'))
    $Version = ($props.Project.PropertyGroup | Where-Object { $_.Version }).Version
}
# major.minor.patch, or major.minor.patch-ci.N for a CI build that is not a release
if ($Version -notmatch '^(\d+\.\d+\.\d+)(-ci\.\d+)?$') { throw "Version must be major.minor.patch[-ci.N]; got '$Version'" }
if (-not $NumVersion) { $NumVersion = "$($Matches[1]).0" }
$numVersion = $NumVersion

$iscc = (Get-Command iscc -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    foreach ($candidate in @("$env:ProgramFiles\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $candidate) { $iscc = $candidate; break }
    }
}
if (-not $iscc) { throw 'ISCC.exe not found: install Inno Setup 6.3 or later' }

$publish = Join-Path $root 'out\publish'
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }

foreach ($project in @('src\Tasketier.App\Tasketier.App.csproj', 'src\Tasketier.Cli\Tasketier.Cli.csproj')) {
    Write-Host "Publishing $project $Version (framework-dependent, win-x64)..."
    & dotnet publish (Join-Path $root $project) -c Release -r win-x64 --self-contained false `
        -p:Version=$Version -p:PublishSingleFile=false -o $publish
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $project with exit code $LASTEXITCODE" }
}
foreach ($required in @('TasketierApp.exe', 'tasketier.exe')) {
    if (-not (Test-Path (Join-Path $publish $required))) { throw "Publish output lacks $required" }
}

Write-Host "Compiling the installer with $iscc..."
& $iscc /Qp "/DAppVersion=$Version" "/DNumVersion=$numVersion" (Join-Path $root 'installer\Tasketier.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE" }

$setup = Join-Path $root "out\installer\Tasketier-Setup-$Version.exe"
if (-not (Test-Path $setup)) { throw "Installer not found at $setup" }
$hash = (Get-FileHash -Algorithm SHA256 $setup).Hash.ToLowerInvariant()
$size = [math]::Round((Get-Item $setup).Length / 1MB, 2)
Write-Host "Installer: $setup ($size MB)"
Write-Host "SHA-256:   $hash"
