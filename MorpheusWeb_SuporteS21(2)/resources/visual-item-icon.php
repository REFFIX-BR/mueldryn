<?php
/**
 * Ícone PNG para itens visuais/cosméticos sem .gif no pack web.
 * Nome preferencial: ?name= do OpenMU (config.ItemDefinition).
 */
define('PATH', dirname(__DIR__));
define('DS', DIRECTORY_SEPARATOR);

require PATH . DS . 'src' . DS . 'Item' . DS . 'VisualCatalog.php';

$section = isset($_GET['section']) ? (int) $_GET['section'] : 0;
$index = isset($_GET['index']) ? (int) $_GET['index'] : 0;
$size = isset($_GET['size']) ? max(32, min(128, (int) $_GET['size'])) : 64;
$nameParam = isset($_GET['name']) ? trim((string) $_GET['name']) : '';
$key = $section . '-' . $index;

$cacheDir = PATH . DS . 'tmp' . DS . 'cache' . DS . 'visual-icons';
$nameHash = $nameParam !== '' ? substr(md5($nameParam), 0, 8) : 'auto';
$cacheFile = $cacheDir . DS . $key . '-' . $size . '-' . $nameHash . '.png';

if (is_file($cacheFile) && (time() - filemtime($cacheFile)) < 86400 * 7) {
    header('Content-Type: image/png');
    header('Cache-Control: public, max-age=604800');
    readfile($cacheFile);
    exit;
}

$row = Morpheus\Item\VisualCatalog::lookup($section, $index);
$name = $nameParam !== '' ? $nameParam : (($row && !empty($row['name'])) ? $row['name'] : null);
$skin = ($row && !empty($row['skin'])) ? $row['skin'] : 'Visual';

if ($name === null) {
    $namesFile = PATH . DS . 'tmp' . DS . 'cache' . DS . 'openmu-item-names.php';
    if (is_file($namesFile)) {
        $names = include $namesFile;
        if (is_array($names) && isset($names[$key])) {
            $name = $names[$key];
        }
    }
}
if ($name === null) {
    $name = 'Item ' . $key;
}

if ($skin === 'Visual' && $name !== '') {
    if (preg_match('/^(Evil Rabbit|Santagreen|Santablue|Bloodbound|Hellshade|Icefrost|Nightfire|Darkbunny|Fallen Angel|Devil Majestyc|Devil_Majestyc)/i', $name, $m)) {
        $skin = str_replace(' ', '_', $m[1]);
    } elseif (preg_match('/^([A-Za-z]+)/', $name, $m)) {
        $skin = $m[1];
    }
}

$slotLabels = array(
    0 => 'ARMA', 2 => 'ARMA', 4 => 'ARMA', 5 => 'CAJ', 6 => 'ESC',
    7 => 'ELMO', 8 => 'PEIT', 9 => 'CAL', 10 => 'LUV', 11 => 'BOT',
    12 => 'ASA', 13 => 'ACC', 14 => 'CONS',
);
$slot = isset($slotLabels[$section]) ? $slotLabels[$section] : 'VIS';

$palette = array(
    'Fallen_Angel' => array(120, 180, 255),
    'Fallen' => array(120, 180, 255),
    'Devil_Majestyc' => array(220, 80, 80),
    'Devil' => array(220, 80, 80),
    'Arcanor' => array(160, 100, 255),
    'Aurion' => array(255, 200, 80),
    'Druid' => array(80, 200, 120),
    'Hellshade' => array(180, 60, 120),
    'Icefrost' => array(100, 200, 255),
    'Magma' => array(255, 120, 40),
    'Nightfire' => array(255, 80, 120),
    'Hallow' => array(255, 140, 40),
    'Wraith' => array(140, 100, 200),
    'Abbadon' => array(200, 40, 40),
    'Death' => array(80, 80, 90),
    'Blackray' => array(60, 60, 80),
    'Bloodbound' => array(180, 30, 50),
    'Darkness' => array(50, 50, 70),
    'Elegance' => array(200, 160, 220),
    'Santablue' => array(80, 160, 255),
    'Santagreen' => array(80, 200, 120),
    'Darkbunny' => array(180, 100, 200),
    'Evil_Rabbit' => array(160, 60, 200),
    'Evil' => array(160, 60, 200),
    'Carnival' => array(255, 100, 180),
);

$skinKey = preg_replace('/[^A-Za-z0-9_]/', '', $skin);
$base = null;
foreach ($palette as $k => $rgb) {
    if (stripos($skinKey, str_replace('_', '', $k)) !== false || stripos($k, $skinKey) !== false) {
        $base = $rgb;
        break;
    }
}
if ($base === null && isset($palette[$skinKey])) {
    $base = $palette[$skinKey];
}
if ($base === null) {
    $base = array(
        (abs(crc32($skin)) % 156) + 60,
        (abs(crc32($skin . 'g')) % 156) + 60,
        (abs(crc32($skin . 'b')) % 156) + 60,
    );
}

$img = imagecreatetruecolor($size, $size);
imagesavealpha($img, true);
$transparent = imagecolorallocatealpha($img, 0, 0, 0, 127);
imagefill($img, 0, 0, $transparent);

$r = $base[0]; $g = $base[1]; $b = $base[2];
$dark = imagecolorallocate($img, max(0, $r - 60), max(0, $g - 60), max(0, $b - 60));
$mid = imagecolorallocate($img, $r, $g, $b);
$light = imagecolorallocate($img, min(255, $r + 50), min(255, $g + 50), min(255, $b + 50));
$border = imagecolorallocate($img, min(255, $r + 80), min(255, $g + 80), min(255, $b + 80));
$white = imagecolorallocate($img, 250, 250, 255);
$gold = imagecolorallocate($img, 255, 220, 120);

$pad = max(2, (int) ($size * 0.08));
$inner = $size - ($pad * 2);
imagefilledrectangle($img, $pad, $pad, $pad + $inner, $pad + $inner, $dark);
imagefilledrectangle($img, $pad + 2, $pad + 2, $pad + $inner - 2, $pad + $inner - 2, $mid);
imagerectangle($img, 1, 1, $size - 2, $size - 2, $border);
imagerectangle($img, 2, 2, $size - 3, $size - 3, $light);

$short = preg_replace('/[^A-Za-z0-9]/', '', $skin);
if ($short === '') {
    $short = preg_replace('/[^A-Za-z0-9]/', '', $name);
}
$short = strtoupper(substr($short, 0, 2));
if ($short === '') {
    $short = 'IT';
}

$fontBig = ($size >= 64) ? 5 : 4;
$fontSm = ($size >= 64) ? 3 : 2;
$sw = imagefontwidth($fontBig) * strlen($short);
$sh = imagefontheight($fontBig);
imagestring($img, $fontBig, (int)(($size - $sw) / 2), (int)(($size - $sh) / 2) - 2, $short, $white);

$slotW = imagefontwidth($fontSm) * strlen($slot);
imagestring($img, $fontSm, (int)(($size - $slotW) / 2), $pad + 2, $slot, $gold);

if (!is_dir($cacheDir)) {
    @mkdir($cacheDir, 0777, true);
}
imagepng($img, $cacheFile, 6);
imagedestroy($img);

header('Content-Type: image/png');
header('Cache-Control: public, max-age=604800');
readfile($cacheFile);
