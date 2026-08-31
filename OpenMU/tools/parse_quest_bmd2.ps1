$QuestPath = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Local\Quest.bmd"
$OutCsv = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\quest_bmd_parsed.csv"

function BuxConvert([byte[]]$buf) {
    $key = @(0xFC, 0xCF, 0xAB)
    $out = New-Object byte[] $buf.Length
    for ($i = 0; $i -lt $buf.Length; $i++) { $out[$i] = $buf[$i] -bxor $key[$i % 3] }
    return $out
}

$dec = BuxConvert ([IO.File]::ReadAllBytes($QuestPath))
$recordSize = 744
$count = [int]($dec.Length / $recordSize)
Write-Host "Records: $count x $recordSize bytes"

$rows = @()
for ($q = 0; $q -lt $count; $q++) {
    $base = $q * $recordSize
    $name = [Text.Encoding]::UTF8.GetString($dec, $base + 6, 32).TrimEnd([char]0)
    
    # scan record for likely EXP (DWORD > 10000) and ZEN values
    $dwords = @()
    for ($o = 0; $o -lt $recordSize - 4; $o += 4) {
        $v = [BitConverter]::ToUInt32($dec, $base + $o)
        if ($v -ge 10000 -and $v -le 500000000) { $dwords += $v }
    }
    $expGuess = ($dwords | Sort-Object -Unique)[0]
    $zenGuess = ($dwords | Sort-Object -Unique)[1]
    
    $rows += [PSCustomObject]@{
        Id = $q + 1
        Name = $name
        Dwords = ($dwords | Sort-Object -Unique -Descending | Select-Object -First 6) -join ';'
    }
}

$rows | Export-Csv -NoTypeInformation -Encoding UTF8 $OutCsv
$rows | Select-Object -First 25 | Format-Table -Wrap
Write-Host "Saved: $OutCsv"
