<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

define("PATH", dirname(__DIR__));
define("DS", DIRECTORY_SEPARATOR);
$width = isset($_GET["width"]) ? $_GET["width"] : 64;
$height = isset($_GET["height"]) ? $_GET["height"] : 64;
$section = $_GET["section"];
$index = $_GET["index"];
$square = isset($_GET["square"]) ? $_GET["square"] : NULL;
header("Content-Type: image/jpeg");
$path = PATH . DS . "resources" . DS . "images" . DS . "items" . DS;
$key = $section . "-" . $index . ".gif";
if (!file_exists($path . $key)) {
    $key = "no-image.png";
    $item = imagecreatefrompng($path . $key);
} else {
    $item = imagecreatefromgif($path . $key);
}
list($x, $y) = getimagesize($path . $key);
if ($square === NULL) {
    $ratio = $x / $y;
    if ($width < $x || $height < $y) {
        if (1 < $ratio) {
            $nwidth = $width;
            $nheight = $height / $ratio;
        } else {
            $nwidth = $width * $ratio;
            $nheight = $height;
        }
    } else {
        $nwidth = $x;
        $nheight = $y;
    }
} else {
    $nwidth = $square;
    $nheight = $square;
}
$dst = imagecreatetruecolor($nwidth, $nheight);
imagecopyresampled($dst, $item, 0, 0, 0, 0, $nwidth, $nheight, $x, $y);
$black = imagecolorallocate($dst, 0, 0, 0);
imagecolortransparent($dst, $black);
$out = imagecreatetruecolor($width, $height);
imagecopyresampled($out, $dst, ($width - $nwidth) / 2, ($height - $nheight) / 2, 0, 0, $width, $height, $width, $height);
$black = imagecolorallocate($out, 0, 0, 0);
imagecolortransparent($out, $black);
imagepng($out, NULL, 9);
imagedestroy($out);
imagedestroy($dst);
imagedestroy($item);

?>