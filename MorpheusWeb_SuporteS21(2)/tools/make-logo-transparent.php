<?php
$in = $argv[1] ?? null;
$out = $argv[2] ?? null;
if (!$in || !$out) {
    fwrite(STDERR, "Usage: php make-logo-transparent.php <in> <out.png>\n");
    exit(1);
}

$data = file_get_contents($in);
if ($data === false) {
    fwrite(STDERR, "Failed to read: $in\n");
    exit(1);
}

$src = @imagecreatefromstring($data);
if (!$src) {
    fwrite(STDERR, "Failed to decode image: $in\n");
    exit(1);
}

$w = imagesx($src);
$h = imagesy($src);
$dst = imagecreatetruecolor($w, $h);
imagealphablending($dst, false);
imagesavealpha($dst, true);
$clear = imagecolorallocatealpha($dst, 0, 0, 0, 127);
imagefilledrectangle($dst, 0, 0, $w, $h, $clear);

// Near-black (and near-neutral dark gray) becomes transparent.
$threshold = 28;

for ($y = 0; $y < $h; $y++) {
    for ($x = 0; $x < $w; $x++) {
        $rgba = imagecolorat($src, $x, $y);
        $r = ($rgba >> 16) & 0xFF;
        $g = ($rgba >> 8) & 0xFF;
        $b = $rgba & 0xFF;

        $maxc = max($r, $g, $b);
        $minc = min($r, $g, $b);
        $neutral = ($maxc - $minc) <= 14;

        if ($neutral && $maxc <= $threshold) {
            $alpha = 127;
            if ($maxc > 5) {
                $alpha = (int) round(127 * (1 - ($maxc / $threshold)));
                $alpha = max(0, min(127, $alpha));
            }
            $col = imagecolorallocatealpha($dst, $r, $g, $b, $alpha);
        } else {
            $col = imagecolorallocatealpha($dst, $r, $g, $b, 0);
        }
        imagesetpixel($dst, $x, $y, $col);
    }
}

imagepng($dst, $out, 6);
imagedestroy($src);
imagedestroy($dst);
echo "Wrote $out ($w x $h)\n";
