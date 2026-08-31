# Re-inventory Mudream.online client binaries and Interface extensions.
param(
    [string]$ClientRoot = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) "Mudream.online"),
    [string]$OutFile = (Join-Path $PSScriptRoot "inventory.json")
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $ClientRoot)) { throw "Client root not found: $ClientRoot" }

$exes = Get-ChildItem $ClientRoot -Recurse -Include *.exe -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch '^unins' } |
    Select-Object FullName, Length, LastWriteTime

$dlls = Get-ChildItem $ClientRoot -Recurse -Include *.dll -File -ErrorAction SilentlyContinue |
    Select-Object FullName, Length, LastWriteTime

$iface = Join-Path $ClientRoot "Data\Interface"
$extCounts = @{}
if (Test-Path $iface) {
    Get-ChildItem $iface -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $e = if ($_.Extension) { $_.Extension.ToLowerInvariant() } else { "(none)" }
        if (-not $extCounts.ContainsKey($e)) { $extCounts[$e] = 0 }
        $extCounts[$e]++
    }
}

$ifaceDirs = @()
if (Test-Path $iface) {
    $ifaceDirs = Get-ChildItem $iface -Directory | ForEach-Object {
        $pdream = (Get-ChildItem $_.FullName -Recurse -Filter *.pdream -EA SilentlyContinue | Measure-Object).Count
        $tdream = (Get-ChildItem $_.FullName -Recurse -Filter *.tdream -EA SilentlyContinue | Measure-Object).Count
        $files = (Get-ChildItem $_.FullName -Recurse -File -EA SilentlyContinue | Measure-Object).Count
        [pscustomobject]@{ name = $_.Name; files = $files; pdream = $pdream; tdream = $tdream }
    }
}

$result = [pscustomobject]@{
    client_root     = $ClientRoot
    generated_utc   = [DateTime]::UtcNow.ToString("o")
    executables     = $exes
    dlls            = $dlls
    interface_exts  = $extCounts
    interface_dirs  = $ifaceDirs
}

$result | ConvertTo-Json -Depth 6 | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "Wrote $OutFile"
Write-Host ("EXEs: {0}; DLLs: {1}; Interface folders: {2}" -f $exes.Count, $dlls.Count, $ifaceDirs.Count)
