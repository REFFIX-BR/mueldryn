param(
    [string]$QuestPath = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Local\Quest.bmd",
    [string]$OutPath = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\quest_decrypted.bin"
)

function BuxConvert([byte[]]$buf) {
    $key = @(0xFC, 0xCF, 0xAB)
    $out = New-Object byte[] $buf.Length
    for ($i = 0; $i -lt $buf.Length; $i++) {
        $out[$i] = $buf[$i] -bxor $key[$i % 3]
    }
    return $out
}

function MapFileDecrypt([byte[]]$src) {
    $key = @(0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27, 0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2)
    $out = New-Object byte[] $src.Length
    $wMapKey = 0x5E
    for ($i = 0; $i -lt $src.Length; $i++) {
        $out[$i] = ($src[$i] -bxor $key[$i % 16]) - $wMapKey
        $wMapKey = ($src[$i] + 0x3D) -band 0xFF
    }
    return $out
}

function Show-Head([string]$label, [byte[]]$data, [int]$len = 64) {
    Write-Host "`n=== $label ==="
    Write-Host ("HEX: " + [BitConverter]::ToString($data[0..([Math]::Min($len - 1, $data.Length - 1))]))
    $ascii = -join ($data[0..([Math]::Min(200, $data.Length - 1))] | ForEach-Object {
            if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { '.' }
        })
    Write-Host "ASCII: $ascii"
}

$enc = [IO.File]::ReadAllBytes($QuestPath)
Write-Host "Quest.bmd size: $($enc.Length)"

Show-Head "Encrypted" $enc

$bux = BuxConvert $enc
Show-Head "BuxConvert" $bux

$map = MapFileDecrypt $enc
Show-Head "MapFileDecrypt" $map

# Try single-byte XOR keys that produce common BMD magic
foreach ($k in @(0xFC, 0xAB, 0x5E, 0xD1, 0x00)) {
    $trial = $enc | ForEach-Object { $_ -bxor $k }
    $head = $trial[0..15]
    if ($head[0] -eq 0x00 -or $head[0] -eq 0x01 -or $head[0] -eq 0x02) {
        Write-Host "Interesting XOR key 0x$('{0:X2}' -f $k): $([BitConverter]::ToString($head))"
    }
}

# Search decrypted bux for quest count patterns: 202 = 0xCA
for ($recordSize = 400..900) {
    if ($enc.Length % $recordSize -eq 0) {
        $count = $enc.Length / $recordSize
        if ($count -ge 200 -and $count -le 210) {
            Write-Host "Possible record size $recordSize x $count quests"
        }
    }
}

# Save BuxConvert output for manual inspection
[IO.File]::WriteAllBytes($OutPath, $bux)
Write-Host "`nWrote $OutPath"
