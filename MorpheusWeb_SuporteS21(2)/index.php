<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

require "constants.php";
if (php_sapi_name() === "cli-server") {
    $_SERVER["PHP_SELF"] = "/" . basename(__FILE__);
    $url = parse_url(urldecode($_SERVER["REQUEST_URI"]));
    $file = __DIR__ . $url["path"];
    if (strpos($url["path"], "..") === false && strpos($url["path"], ".") !== false && is_file($file)) {
        return false;
    }
}
require "bootstrap.php";

?>