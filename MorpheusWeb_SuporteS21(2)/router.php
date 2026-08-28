<?php
$uri = urldecode(parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH));
$file = __DIR__ . $uri;
if ($uri !== '/' && is_file($file)) {
    $base = basename($file);
    if (!in_array($base, ['index.php', 'admin.php', 'router.php'], true)) {
        return false;
    }
}
require __DIR__ . '/index.php';
return true;
