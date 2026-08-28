<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

define("DS", DIRECTORY_SEPARATOR);
define("ROOT", dirname(__DIR__));
require ROOT . DS . "vendor" . DS . "autoload.php";
$mark = $_GET["mark"];
$size = isset($_GET["size"]) ? $_GET["size"] : 64;
$logo = new Morpheus\Guild\Logo();
$logo->setMark($mark)->setSize($size)->toImage();

?>