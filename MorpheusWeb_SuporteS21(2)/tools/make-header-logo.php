<?php
$srcPath = __DIR__ . '/../templates/youplay_v3/assets/images/logo.png';
$outPath = __DIR__ . '/../templates/unique/assets/images/logo-header.png';
$maxW = 380;

$data = file_get_contents($srcPath);
$src = imagecreatefromstring($data);
if (!$src) {
    fwrite(STDERR, "Failed to load source logo\n");
    exit(1);
}

$w = imagesx($src);
$h = imagesy($src);
$threshold = 28;

$tmp = imagecreatetruecolor($w, $h);
imagealphablending($tmp, false);
imagesavealpha($tmp, true);
$clear = imagecolorallocatealpha($tmp, 0, 0, 0, 127);
imagefilledrectangle($tmp, 0, 0, $w, $h, $clear);

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
            $col = imagecolorallocatealpha($tmp, $r, $g, $b, $alpha);
        } else {
            $col = imagecolorallocatealpha($tmp, $r, $g, $b, 0);
        }
        imagesetpixel($tmp, $x, $y, $col);
    }
}

imagedestroy($src);

$newW = min($maxW, $w);
$newH = (int) round($h * ($newW / $w));
$dst = imagecreatetruecolor($newW, $newH);
imagealphablending($dst, false);
imagesavealpha($dst, true);
imagefilledrectangle($dst, 0, 0, $newW, $newH, $clear);
imagecopyresampled($dst, $tmp, 0, 0, 0, 0, $newW, $newH, $w, $h);
imagedestroy($tmp);

imagepng($dst, $outPath, 6);
imagedestroy($dst);

echo "Wrote {$outPath} ({$newW}x{$newH})\n";
