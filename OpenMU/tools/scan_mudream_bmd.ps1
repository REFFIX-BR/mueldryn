$root = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data"
$needle = "Wrath of the Demon Bull"
$key = @(0xFC, 0xCF, 0xAB)

function BuxConvert([byte[]]$buf) {
    $out = New-Object byte[] $buf.Length
    for ($i = 0; $i -lt $buf.Length; $i++) { $out[$i] = $buf[$i] -bxor $key[$i % 3] }
    return $out
}

Get-ChildItem $root -Recurse -Filter *.bmd | ForEach-Object {
    try {
        $raw = [IO.File]::ReadAllBytes($_.FullName)
        $dec = BuxConvert $raw
        $text = [Text.Encoding]::UTF8.GetString($dec)
        if ($text.Contains($needle)) {
            Write-Host "FOUND in $($_.FullName)"
        }
        if ($text.Contains("899300") -or $text.Contains("750000")) {
            Write-Host "NUMBERS in $($_.FullName)"
        }
    } catch {}
}

# Export first 30 legacy quest names from Quest.bmd
$dec = BuxConvert ([IO.File]::ReadAllBytes("$root\Local\Quest.bmd"))
$rs = 744
$out = "c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\quest_bmd_names.txt"
$lines = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt 30; $i++) {
    $name = [Text.Encoding]::UTF8.GetString($dec, $i * $rs + 6, 32).TrimEnd([char]0)
    $lines.Add("{0,3}. {1}" -f ($i+1), $name)
}
[IO.File]::WriteAllLines($out, $lines)
Write-Host "Wrote names preview to $out"
