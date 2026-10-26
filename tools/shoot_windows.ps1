<#
.SYNOPSIS
Takes the README screenshots: the capture box with a typed line, the help panel, the list window, the alarm card.

.DESCRIPTION
Starts the built app (Debug by default, -Exe for another), opens each window, and saves a PNG of just that
window into docs\img. Keys are sent only after the foreground window's title is checked, so nothing is
typed into another program; the script aborts otherwise. Seed a data folder with the CLI (--data) and pass it
as -Data: the app then runs beside the installed one. Stop that instance afterwards (its title is Tasketier).
#>
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\src\Tasketier.App\bin\Debug\net10.0-windows\TasketierApp.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '../docs/img'),
    [string]$Data = '',
    [string]$Cli = (Join-Path $PSScriptRoot '../src/Tasketier.Cli/bin/Debug/net10.0/tasketier.exe')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices; using System.Text;
public static class Win {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  public struct RECT { public int L, T, R, B; }
  public static string Title() { var sb = new StringBuilder(256); GetWindowText(GetForegroundWindow(), sb, 256); return sb.ToString(); }
}
"@
if (-not $Data -and (Get-Process TasketierApp -ErrorAction SilentlyContinue)) { throw 'TasketierApp is running; stop it first, or pass -Data with a seeded folder.' }
$dataArgs = if ($Data) { @('--data', $Data) } else { @() }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Expect-Foreground([string]$Title) {
    $actual = [Win]::Title()
    if ($actual -ne $Title) { throw "ABORT: the foreground window is '$actual', expected '$Title'" }
}

function Save-Window([string]$Name) {
    $r = New-Object Win+RECT
    [Win]::GetWindowRect([Win]::GetForegroundWindow(), [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $bmp.Save((Join-Path $OutDir "$Name.png"))
    $g.Dispose(); $bmp.Dispose()
    Write-Host "saved $Name.png ($w x $h)"
}

Start-Process -FilePath $Exe -ArgumentList (@('--capture') + $dataArgs)
Start-Sleep -Milliseconds 1500
Expect-Foreground 'Add a task'
[System.Windows.Forms.SendKeys]::SendWait('Call the dentist @fri @15:30 ! #health')
Start-Sleep -Milliseconds 600
Expect-Foreground 'Add a task'
Save-Window 'capture'
[System.Windows.Forms.SendKeys]::SendWait('{F1}')
Start-Sleep -Milliseconds 600
Expect-Foreground 'Add a task'
Save-Window 'capture-help'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 600

# A task whose alarm is already due: the running instance picks it up at its next half-minute poll.
if ($Data) {
    $due = (Get-Date).AddMinutes(1).ToString('HH:mm')
    & $Cli add "Team standup @today @$due @alarm5" --data $Data | Out-Null
}

Start-Process -FilePath $Exe -ArgumentList (@('--list') + $dataArgs)
Start-Sleep -Milliseconds 1500
Expect-Foreground 'Tasketier'
Save-Window 'list'

if ($Data) {
    $seen = $false
    for ($i = 0; $i -lt 16; $i++) {
        Start-Sleep -Seconds 3
        if ([Win]::Title() -eq 'Tasketier alarm') { $seen = $true; break }
    }
    if (-not $seen) { throw 'ABORT: no alarm card came to the front within 48 s' }
    Start-Sleep -Milliseconds 800
    Save-Window 'alarm'
    $instance = Get-CimInstance Win32_Process -Filter "Name = 'TasketierApp.exe'" | Where-Object { $_.CommandLine -like "*$Data*" }
    foreach ($proc in $instance) { Stop-Process -Id $proc.ProcessId -Force }
    Write-Host 'the test instance is stopped'
}
