$QuestPath = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Local\Quest.bmd"
$OutCsv = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\quest_bmd_legacy.csv"

function BuxConvert([byte[]]$buf) {
    $key = @(0xFC, 0xCF, 0xAB)
    $out = New-Object byte[] $buf.Length
    for ($i = 0; $i -lt $buf.Length; $i++) {
        $out[$i] = $buf[$i] -bxor $key[$i % 3]
    }
    return $out
}

function Read-U16([byte[]]$b, [int]$o) { return [BitConverter]::ToUInt16($b, $o) }
function Read-U32([byte[]]$b, [int]$o) { return [BitConverter]::ToUInt32($b, $o) }

$enc = [IO.File]::ReadAllBytes($QuestPath)
$dec = BuxConvert $enc
Write-Host "Decrypted size: $($dec.Length)"

# QuestAttributeFile layout (MuMain CSQuest.cpp):
# short shQuestConditionNum (2)
# short shQuestRequestNum (2)
# WORD wNpcType (2)
# char strQuestName[32]
# QUEST_CLASS_ACT QuestAct[16] -> 16 * 20 = 320 bytes (MAX_CLASS=7)
# QUEST_CLASS_REQUEST QuestRequest[16] -> 16 * 16 = 256
# Total = 2+2+2+32+320+256 = 614 bytes per record

$recordSize = 614
$count = [int]($dec.Length / $recordSize)
$remainder = $dec.Length % $recordSize
Write-Host "Record size $recordSize -> $count records, remainder $remainder"

$rows = New-Object System.Collections.Generic.List[object]
for ($q = 0; $q -lt $count; $q++) {
    $base = $q * $recordSize
    $cond = Read-U16 $dec ($base + 0)
    $req = Read-U16 $dec ($base + 2)
    $npc = Read-U16 $dec ($base + 4)
    $name = [Text.Encoding]::UTF8.GetString($dec, $base + 6, 32).TrimEnd([char]0)

    # First QuestAct at offset 38
    $actOff = $base + 38
    $actType = $dec[$actOff + 1]
    $itemType = Read-U16 $dec ($actOff + 2)
    $itemSub = $dec[$actOff + 4]
    $itemLvl = $dec[$actOff + 5]
    $itemNum = $dec[$actOff + 6]

    # First QuestRequest zen at actOff + 320 + 12 = base + 38 + 320 + 12 = base + 370? 
    # QuestRequest offset = base + 38 + 320 = base + 358
    $reqOff = $base + 38 + 320
    $reqZen = Read-U32 $dec ($reqOff + 10)

    $rows.Add([PSCustomObject]@{
        Index = $q
        Name = $name
        Conditions = $cond
        Requests = $req
        NpcType = $npc
        ActType = $actType
        ItemType = $itemType
        ItemSub = $itemSub
        ItemLevel = $itemLvl
        ItemNum = $itemNum
        RequestZen = $reqZen
    })
}

$rows | Export-Csv -NoTypeInformation -Encoding UTF8 $OutCsv
Write-Host "Exported $($rows.Count) rows to $OutCsv"
$rows | Select-Object -First 20 | Format-Table -AutoSize
