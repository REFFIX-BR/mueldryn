# PowerShell string extractor for Mudream binaries (no Python required).
# Developmental RE aid for MuMain/OpenMU porting — not for cracking/DRM bypass.

param(
    [Parameter(Mandatory = $true)][string[]]$Binaries,
    [Parameter(Mandatory = $true)][string]$OutDir,
    [int]$MinLen = 5
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$keywords = @(
    "EventList", "EventSchedule", "EventTimer", "Events Timer", "ActiveInvasion", "Invasion",
    "SoulSystem", "Soul", "QuestInfo", "QuestPanel", "QuestSystem", "Quest",
    "LegendUI", "LegendHUD", "MainMenu", "DeathJournal", "DamageStatistic",
    "GuildManager", "GuildOracle", "PartySearch", "MacroUI", "Collections",
    "CharacterOverview", "HarmonyMix", "InGameShop", "JewelBank", "VipShop",
    "Dungeon", "BossLife", "BossHealth", "NaviMap", "Notification", "NpcIcons",
    "GuestAccess", "GameMaster", "SkillStack", "PartCharge", "bottom_panel",
    "new_main_frame", "Scaleform", "Mudream", "MuDream", "Reborn",
    "Interface\\", "Data\\Interface", "0xFA", "0xFB", "0xFC", "0xFE",
    "opcode", "packet", ".json", ".lua", "Atlas\\", "LostedWords", "ItemSkinning",
    "PreviewWindow", "BanSystem"
)

function Get-AsciiStrings([byte[]]$bytes, [int]$min) {
    $sb = New-Object System.Text.StringBuilder
    $list = New-Object System.Collections.Generic.List[string]
    foreach ($b in $bytes) {
        if ($b -ge 0x20 -and $b -le 0x7E) {
            [void]$sb.Append([char]$b)
        }
        else {
            if ($sb.Length -ge $min) { $list.Add($sb.ToString()) }
            [void]$sb.Clear()
        }
    }
    if ($sb.Length -ge $min) { $list.Add($sb.ToString()) }
    return $list
}

function Get-Utf16LeAsciiStrings([byte[]]$bytes, [int]$min) {
    $list = New-Object System.Collections.Generic.List[string]
    $sb = New-Object System.Text.StringBuilder
    $i = 0
    while ($i + 1 -lt $bytes.Length) {
        $lo = $bytes[$i]
        $hi = $bytes[$i + 1]
        if ($hi -eq 0 -and $lo -ge 0x20 -and $lo -le 0x7E) {
            [void]$sb.Append([char]$lo)
            $i += 2
        }
        else {
            if ($sb.Length -ge $min) { $list.Add($sb.ToString()) }
            [void]$sb.Clear()
            $i += 2
        }
    }
    if ($sb.Length -ge $min) { $list.Add($sb.ToString()) }
    return $list
}

$summary = @()

foreach ($bin in $Binaries) {
    if (-not (Test-Path $bin)) {
        Write-Warning "Missing: $bin"
        continue
    }
    $fi = Get-Item $bin
    Write-Host "Scanning $($fi.FullName) ($($fi.Length) bytes)..."
    $bytes = [System.IO.File]::ReadAllBytes($fi.FullName)
    $ascii = Get-AsciiStrings $bytes $MinLen
    $utf16 = Get-Utf16LeAsciiStrings $bytes $MinLen

    $hits = New-Object System.Collections.Generic.List[object]
    $seen = @{}
    foreach ($pair in @(@("ascii", $ascii), @("utf16le", $utf16))) {
        $enc = $pair[0]
        foreach ($s in $pair[1]) {
            $matched = $false
            foreach ($k in $keywords) {
                if ($s.IndexOf($k, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    $matched = $true
                    break
                }
            }
            if (-not $matched) { continue }
            $key = "$enc|$s"
            if ($seen.ContainsKey($key)) { continue }
            $seen[$key] = $true
            $hits.Add([pscustomobject]@{ encoding = $enc; string = $s })
        }
    }

    $safeName = ($fi.Name -replace '[^\w\.-]', '_') + "_" + $fi.Directory.Name
    $txtPath = Join-Path $OutDir ("strings_" + $safeName + ".txt")
    $lines = @("FILE: $($fi.FullName)", "SIZE: $($fi.Length)", "ASCII_TOTAL: $($ascii.Count)", "UTF16_TOTAL: $($utf16.Count)", "HITS: $($hits.Count)", "")
    $lines += ($hits | Sort-Object string | ForEach-Object { "[{0}] {1}" -f $_.encoding, $_.string })
    $lines | Set-Content -Path $txtPath -Encoding UTF8

    $summary += [pscustomobject]@{
        path         = $fi.FullName
        size         = $fi.Length
        ascii_total  = $ascii.Count
        utf16_total  = $utf16.Count
        keyword_hits = $hits.Count
        out_txt      = $txtPath
    }
    Write-Host "  -> $($hits.Count) keyword hits -> $txtPath"
}

$summaryPath = Join-Path $OutDir "strings_summary.json"
$summary | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host "Summary: $summaryPath"
