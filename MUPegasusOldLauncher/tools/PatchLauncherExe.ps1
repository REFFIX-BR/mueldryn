# Patch strings UTF-16LE in Launcher.exe (sem recompilar)
param(
    [string]$LauncherExe = ""
)

$ErrorActionPreference = "Stop"
if (-not $LauncherExe) {
    $LauncherExe = Join-Path (Split-Path $PSScriptRoot -Parent) "Launcher\bin\Release\Launcher.exe"
}
if (-not (Test-Path $LauncherExe)) { throw "Launcher.exe not found: $LauncherExe" }

function Get-UnicodeBytes([string]$s) { [Text.Encoding]::Unicode.GetBytes($s) }

$replacements = @{
    "http://update.titanswrathmu.pro/" = "http://200.11.121.89/update/    "
    "https://titanswrathmu.pro/"       = "http://127.0.0.1:8090/    "
}

$bytes = [IO.File]::ReadAllBytes($LauncherExe)
$patched = 0

foreach ($kv in $replacements.GetEnumerator()) {
    $from = Get-UnicodeBytes $kv.Key
    $to = Get-UnicodeBytes $kv.Value
    if ($from.Length -ne $to.Length) {
        Write-Warning "Skip length mismatch ($($kv.Key.Length) vs $($kv.Value.Length) chars): $($kv.Key)"
        continue
    }
    for ($i = 0; $i -le $bytes.Length - $from.Length; $i++) {
        $match = $true
        for ($j = 0; $j -lt $from.Length; $j++) {
            if ($bytes[$i + $j] -ne $from[$j]) { $match = $false; break }
        }
        if ($match) {
            for ($j = 0; $j -lt $to.Length; $j++) { $bytes[$i + $j] = $to[$j] }
            $patched++
            Write-Host "Patched: $($kv.Key) -> $($kv.Value.Trim())"
        }
    }
}

[IO.File]::WriteAllBytes($LauncherExe, $bytes)
Write-Host "Done. Patches applied: $patched"
