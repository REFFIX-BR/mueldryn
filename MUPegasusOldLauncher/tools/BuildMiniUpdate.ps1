# MuEldryn Launcher — Build MiniUpdate pack
# Uso:
#   .\BuildMiniUpdate.ps1 -ClientRoot "C:\path\to\client" -Files @("Data\Local\Item.bmd","Data\Player\player.bmd")
#   .\BuildMiniUpdate.ps1 -ClientRoot "C:\path\to\client" -IncludeAllData
#
# Gera UpdateServer\MiniUpdate\ com arquivos + update.info
# Publique a pasta MiniUpdate em http://SEU_IP/update/MiniUpdate/

param(
    [Parameter(Mandatory = $true)]
    [string]$ClientRoot,

    [string[]]$Files = @(),

    [switch]$IncludeAllData,

    [string]$OutRoot = ""
)

$ErrorActionPreference = "Stop"
$LauncherRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutRoot) {
    $OutRoot = Join-Path $LauncherRoot "UpdateServer"
}

$Mini = Join-Path $OutRoot "MiniUpdate"
New-Item -ItemType Directory -Force -Path $Mini | Out-Null

$ClientRoot = (Resolve-Path $ClientRoot).Path

function Get-Crc32Hex([byte[]]$bytes) {
    # Same polynomial as Launcher Crc32.cs (standard ZIP CRC)
    $crcTable = New-Object uint32[] 256
    for ($i = 0; $i -lt 256; $i++) {
        [uint32]$c = $i
        for ($k = 0; $k -lt 8; $k++) {
            if ($c -band 1) { $c = (0xEDB88320 -bxor ($c -shr 1)) } else { $c = ($c -shr 1) }
        }
        $crcTable[$i] = $c
    }
    [uint32]$crc = 0xFFFFFFFF
    foreach ($b in $bytes) {
        $crc = $crcTable[($crc -bxor $b) -band 0xFF] -bxor ($crc -shr 8)
    }
    $crc = $crc -bxor 0xFFFFFFFF
    return ("{0:X8}" -f $crc)
}

function XorEnc([string]$s) {
    $chars = $s.ToCharArray()
    for ($i = 0; $i -lt $chars.Length; $i++) {
        $chars[$i] = [char]([int]$chars[$i] -bxor 99)
    }
    return -join $chars
}

$list = New-Object System.Collections.Generic.List[object]

if ($IncludeAllData) {
    Get-ChildItem -Path (Join-Path $ClientRoot "Data") -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($ClientRoot.Length).TrimStart('\', '/')
        $Files += $rel
    }
}

if ($Files.Count -eq 0) {
    Write-Host "Nenhum arquivo. Passe -Files ou -IncludeAllData"
    exit 1
}

foreach ($rel in ($Files | Select-Object -Unique)) {
    $rel = $rel -replace '/', '\'
    $src = Join-Path $ClientRoot $rel
    if (-not (Test-Path $src)) {
        Write-Warning "Skip missing: $rel"
        continue
    }
    $bytes = [IO.File]::ReadAllBytes($src)
    $crc = Get-Crc32Hex $bytes
    $dest = Join-Path $Mini $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item -Force $src $dest
    $list.Add([pscustomobject]@{ Path = $rel; Crc = $crc; Size = $bytes.LongLength })
    Write-Host ("+ {0}  CRC={1}  {2} bytes" -f $rel, $crc, $bytes.LongLength)
}

# BinaryWriter strings = length-prefixed UTF8 (same as .NET BinaryWriter)
$infoPath = Join-Path $Mini "update.info"
$fs = [IO.File]::Create($infoPath)
$bw = New-Object IO.BinaryWriter $fs
try {
    $bw.Write((XorEnc ([string]$list.Count)))
    $bw.Write((XorEnc "1"))  # version marker
    foreach ($f in $list) {
        $bw.Write((XorEnc $f.Path))
        $bw.Write((XorEnc $f.Crc))
        $bw.Write((XorEnc ([string]$f.Size)))
    }
}
finally {
    $bw.Close()
}

Write-Host ""
Write-Host "OK: $($list.Count) arquivos em $Mini"
Write-Host "Publique em: http://SEU_HOST/update/MiniUpdate/"
Write-Host "URL no Launcher.bmd deve terminar com /update/"
