# Export Quest.bmd (legacy 200 quests) after BuxConvert XOR {FC,CF,AB}
param(
    [string]$QuestPath = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Local\Quest.bmd",
    [string]$OutCsv = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\quest_bmd_export.csv",
    [string]$OutTxt = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\quest_bmd_preview.txt"
)

function BuxConvert([byte[]]$buf) {
    $key = @(0xFC, 0xCF, 0xAB)
    $out = New-Object byte[] $buf.Length
    for ($i = 0; $i -lt $buf.Length; $i++) { $out[$i] = $buf[$i] -bxor $key[$i % 3] }
    return $out
}

function Read-U16([byte[]]$b, [int]$o) { [BitConverter]::ToUInt16($b, $o) }
function Read-I16([byte[]]$b, [int]$o) { [BitConverter]::ToInt16($b, $o) }
function Read-U32([byte[]]$b, [int]$o) { [BitConverter]::ToUInt32($b, $o) }

# QUEST_ATTRIBUTE layout from 6.Main_UP52_Full/source/_struct.h (MSVC x86, #pragma pack default)
# QUEST_CLASS_ACT = 28 bytes (padded), header = 40 bytes, record = 744 bytes
$ACT_SIZE = 28
$REQ_SIZE = 16
$RECORD_SIZE = 744
$MAX_ACT = 16
$MAX_REQ = 16
$HEADER_SIZE = 40

$enc = [IO.File]::ReadAllBytes($QuestPath)
$dec = BuxConvert $enc
$count = [int]($dec.Length / $RECORD_SIZE)

Write-Host "File: $QuestPath"
Write-Host "Encrypted size: $($enc.Length) | Records: $count x $RECORD_SIZE"

$rows = New-Object System.Collections.Generic.List[object]
$preview = New-Object System.Collections.Generic.List[string]
$preview.Add("=== Quest.bmd descriptografado (BuxConvert FC-CF-AB) ===")
$preview.Add("Tipo: quests LEGACY MU (200), NAO sao as 202 Main Quests Mudream")
$preview.Add("")

for ($q = 0; $q -lt $count; $q++) {
    $base = $q * $RECORD_SIZE
    $condNum = Read-I16 $dec ($base + 0)
    $reqNum = Read-I16 $dec ($base + 2)
    $npcType = Read-U16 $dec ($base + 4)
    $name = [Text.Encoding]::ASCII.GetString($dec, $base + 6, 32).TrimEnd([char]0)

    $actBase = $base + $HEADER_SIZE
    $acts = @()
    for ($a = 0; $a -lt $MAX_ACT; $a++) {
        $ab = $actBase + ($a * $ACT_SIZE)
        if ($dec[$ab] -eq 0) { continue }
        $acts += "type=$($dec[$ab+1]) item=$([BitConverter]::ToUInt16($dec,$ab+2)):$($dec[$ab+4]) lv$($dec[$ab+5]) x$($dec[$ab+6])"
    }

    $reqBase = $actBase + ($MAX_ACT * $ACT_SIZE)
    $reqs = @()
    $zenList = @()
    for ($r = 0; $r -lt $MAX_REQ; $r++) {
        $rb = $reqBase + ($r * $REQ_SIZE)
        if ($dec[$rb] -eq 0) { continue }
        $zen = Read-U32 $dec ($rb + 10)
        if ($zen -gt 0) { $zenList += $zen }
        $reqs += "type=$($dec[$rb+1]) lv$([BitConverter]::ToUInt16($dec,$rb+6))-$([BitConverter]::ToUInt16($dec,$rb+8)) zen=$zen"
    }

    $rows.Add([PSCustomObject]@{
        Id = $q + 1
        Name = $name
        NpcType = $npcType
        Conditions = $condNum
        Requests = $reqNum
        ZenValues = ($zenList -join ';')
        Actions = ($acts -join ' | ')
        Requirements = ($reqs -join ' | ')
    })

    if ($q -lt 40) {
        $line = ('{0,3}. {1}  (NPC={2}, cond={3}, req={4}, zen={5})' -f ($q+1), $name, $npcType, $condNum, $reqNum, ($zenList -join ';'))
        $preview.Add($line)
    }
}

$rows | Export-Csv -NoTypeInformation -Encoding UTF8 $OutCsv
[IO.File]::WriteAllLines($OutTxt, $preview)

Write-Host "CSV: $OutCsv"
Write-Host "Preview: $OutTxt"
Write-Host ""
Get-Content $OutTxt
