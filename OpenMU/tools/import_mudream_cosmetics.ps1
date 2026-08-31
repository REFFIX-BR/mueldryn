# Imports Mudream visual cosmetic client assets into MuMain.
# Run from repo root after updating Mudream.online client files.
param(
    [switch]$SkipSkinCopy,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$mudream = Join-Path $root 'Mudream.online'
$muData = Join-Path $root 'MuMain\src\bin\Data'

if (-not (Test-Path $mudream)) {
    Write-Error "Mudream client folder not found: $mudream"
}

function Copy-Tree($src, $dst, $label) {
    if (-not (Test-Path $src)) {
        Write-Warning "Skip $label - missing $src"
        return
    }
    Write-Host "Copy $label ..."
    if ($WhatIf) {
        Write-Host "  $src -> $dst"
        return
    }
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    robocopy $src $dst /E /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $label (exit $LASTEXITCODE)" }
}

# 1) 3D models + textures (~540 MB)
if (-not $SkipSkinCopy) {
    Copy-Tree (Join-Path $mudream 'Data\Item\CustomItem\Skin') (Join-Path $muData 'Item\CustomItem\Skin') 'CustomItem/Skin'
    Copy-Tree (Join-Path $mudream 'Data\Item\CustomItem\CustomItem') (Join-Path $muData 'Item\CustomItem\CustomItem') 'CustomItem/CustomItem'
}

Copy-Tree (Join-Path $mudream 'Data\Item\Transmogrify') (Join-Path $muData 'Item\Transmogrify') 'Transmogrify'

# 2) Item.bmd - DO NOT replace MuMain Item.bmd with Mudream's.
# Mudream Item.bmd is a custom 320-byte format (model paths in Name).
# MuMain uses 84-byte legacy Item_*.bmd; cosmetics are patched at runtime
# by MudreamCosmeticLoader + MudreamCosmeticCatalog.Generated.cpp.
# Keep Mudream copy as reference only:
$itemBmdSrc = Join-Path $mudream 'Data\Local\Item.bmd'
$itemBmdRef = Join-Path $muData 'Local\Item.bmd.mudream-ref'
if (Test-Path $itemBmdSrc) {
    Write-Host 'Copy Mudream Item.bmd as reference (not used by MuMain runtime)...'
    if (-not $WhatIf) {
        New-Item -ItemType Directory -Force -Path (Split-Path $itemBmdRef) | Out-Null
        Copy-Item $itemBmdSrc $itemBmdRef -Force
        # Ensure MuMain Item.bmd is restored if a previous import overwrote it
        $bak = Join-Path $muData 'Local\Item.bmd.bak-mumain'
        $dst = Join-Path $muData 'Local\Item.bmd'
        if ((Test-Path $bak) -and (Test-Path $dst) -and ((Get-Item $dst).Length -ne (Get-Item $bak).Length)) {
            Write-Warning "Restoring MuMain Item.bmd from backup (was Mudream format)."
            Copy-Item $bak $dst -Force
        }
    }
}

# 3) Supporting XML/txt for cloaks, glow, positions
$supportFiles = @(
    @{ Src = 'Data\Local\Renewal\ItemCloth.xml'; Dst = 'Local\Renewal\ItemCloth.xml' },
    @{ Src = 'Data\Local\txt\CRemoveGlow.txt'; Dst = 'Local\txt\CRemoveGlow.txt' },
    @{ Src = 'Data\Local\xml\ItemPosition.xml'; Dst = 'Local\xml\ItemPosition.xml' },
    @{ Src = 'Data\Local\ItemTooltip\Tooltip.xml'; Dst = 'Local\ItemTooltip\MudreamTooltip.xml' },
    @{ Src = 'Data\Local\ItemTooltip\Tooltip_text.xml'; Dst = 'Local\ItemTooltip\MudreamTooltip_text.xml' }
)
foreach ($f in $supportFiles) {
    $src = Join-Path $mudream $f.Src
    $dst = Join-Path $muData $f.Dst
    if (-not (Test-Path $src)) { continue }
    Write-Host "Copy $($f.Dst)..."
    if (-not $WhatIf) {
        New-Item -ItemType Directory -Force -Path (Split-Path $dst) | Out-Null
        Copy-Item $src $dst -Force
    }
}

# 4) Regenerate OpenMU server catalog + C# embed
Write-Host 'Regenerating OpenMU cosmetic catalog...'
& (Join-Path $PSScriptRoot 'import_mudream_cosmetics_catalog.ps1')
Write-Host 'Regenerating MuMain cosmetic client catalog...'
& (Join-Path $PSScriptRoot 'gen_mudream_cosmetic_client.ps1')

Write-Host 'Done. Rebuild Main.exe (MudreamCosmeticLoader). Do NOT use Mudream Item.bmd as MuMain Item.bmd.'
