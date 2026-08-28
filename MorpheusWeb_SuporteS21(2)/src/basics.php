<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

function template()
{
    if (!config("fix_template", false) && !in_admin()) {
        $template = app()->request()->get("template");
        if (!$template) {
            $template = app()->session->get("template", config("template", "default"));
        }
    } else {
        $template = config("template", "default");
    }
    return $template;
}
function config($key = NULL, $default = NULL)
{
    return Morpheus\Core\Config::get($key, $default);
}
function full_url()
{
    $https = env("HTTPS");
    $ssl = !empty($https) && $https == "on";
    $sp = strtolower(env("SERVER_PROTOCOL"));
    $protocol = substr($sp, 0, strpos($sp, "/")) . ($ssl ? "s" : "");
    $port = env("SERVER_PORT");
    $port = !$ssl && $port == "80" || $ssl && $port == "443" ? "" : ":" . $port;
    $host = env("SERVER_NAME") . $port;
    return $protocol . "://" . $host;
}
function base($full = false)
{
    $base = config("base_path");
    if (!empty($base)) {
        return ($full ? full_url() : "") . "/" . $base;
    }
    return $full ? full_url() : "";
}
function url_to($path, $full = false)
{
    $prefix = "";
    if (!config("url_rewrite", true)) {
        $prefix = "/index.php";
    }
    return base($full) . $prefix . $path;
}
function is_home()
{
    $pi = env("PATH_INFO");
    if ($pi === NULL || $pi === '' || $pi === '/') {
        return true;
    }
    $uri = isset($_SERVER['REQUEST_URI']) ? $_SERVER['REQUEST_URI'] : '';
    $path = parse_url($uri, PHP_URL_PATH);
    return $path === '/' || $path === '/index.php' || $path === '/index.php/';
}
function asset_to($file, $full = false, $folder = "assets")
{
    if (strpos($file, ".") !== false) {
        $parts = explode(".", $file);
        $plugin = $parts[0];
        $active = plugin_active($plugin);
        if ($active) {
            $file = join(".", array_slice($parts, 1));
            return base($full) . "/plugins/" . $plugin . "/" . $folder . $file;
        }
    }
    return base($full) . "/templates/" . (in_admin() ? "admin" : template()) . "/" . $folder . $file;
}
function upload_to($file, $full = false, $folder = "uploads")
{
    return base($full) . "/" . $folder . $file;
}
function resource_to($file, $full = false, $folder = "resources")
{
    return base($full) . "/" . $folder . $file;
}
function ife($condition, $notempty, $empty = "")
{
    if ($condition) {
        return $notempty;
    }
    return $empty;
}
function timee($script)
{
    list($usec, $sec) = explode(" ", microtime());
    $script_start = (double) $sec + (double) $usec;
    $script();
    list($usec, $sec) = explode(" ", microtime());
    $script_end = (double) $sec + (double) $usec;
    $elapsed_time = round($script_end - $script_start, 5);
    echo "Elapsed time: ";
    echo $elapsed_time;
    echo " secs. Memory usage: ";
    echo round(memory_get_peak_usage(true) / 1024 / 1024, 2);
    echo "Mb <br />";
}
function h($text, $double = true, $charset = NULL)
{
    if (is_string($text)) {
    } else {
        if (is_array($text)) {
            $texts = array();
            foreach ($text as $k => $t) {
                $texts[$k] = h($t, $double, $charset);
            }
            return $texts;
        } else {
            if (is_object($text)) {
                if (method_exists($text, "__toString")) {
                    $text = (string) $text;
                } else {
                    $text = "(object)" . get_class($text);
                }
            } else {
                if (is_bool($text)) {
                    return $text;
                }
            }
        }
    }
    static $defaultCharset = false;
    if ($defaultCharset === false) {
        $defaultCharset = mb_internal_encoding();
        if ($defaultCharset === NULL) {
            $defaultCharset = "UTF-8";
        }
    }
    if (is_string($double)) {
        $charset = $double;
    }
    return htmlspecialchars($text, ENT_QUOTES | ENT_SUBSTITUTE, $charset ? $charset : $defaultCharset, $double);
}
function pr($var)
{
    if (config("debug")) {
        printf("<pre class=\"pr\">%s</pre>", trim(print_r($var, true)));
    }
}
function dd($var)
{
    pr($var);
    exit;
}
function env($key)
{
    if ($key === "HTTPS") {
        if (isset($_SERVER["HTTPS"])) {
            return !empty($_SERVER["HTTPS"]) && $_SERVER["HTTPS"] !== "off";
        }
        return strpos(env("SCRIPT_URI"), "https://") === 0;
    }
    if ($key === "SCRIPT_NAME" && env("CGI_MODE") && isset($_ENV["SCRIPT_URL"])) {
        $key = "SCRIPT_URL";
    }
    $val = NULL;
    if (isset($_SERVER[$key])) {
        $val = $_SERVER[$key];
    } else {
        if (isset($_ENV[$key])) {
            $val = $_ENV[$key];
        } else {
            if (getenv($key) !== false) {
                $val = getenv($key);
            }
        }
    }
    if ($key === "REMOTE_ADDR" && $val === env("SERVER_ADDR")) {
        $addr = env("HTTP_PC_REMOTE_ADDR");
        if ($addr !== NULL) {
            $val = $addr;
        }
    }
    if ($val !== NULL) {
        return $val;
    }
    switch ($key) {
        case "DOCUMENT_ROOT":
            $name = env("SCRIPT_NAME");
            $filename = env("SCRIPT_FILENAME");
            $offset = 0;
            if (!strpos($name, ".php")) {
                $offset = 4;
            }
            return substr($filename, 0, 0 - (strlen($name) + $offset));
        case "PHP_SELF":
            return str_replace(env("DOCUMENT_ROOT"), "", env("SCRIPT_FILENAME"));
        case "CGI_MODE":
            return PHP_SAPI === "cgi";
    }
}
function bintohex($bin)
{
    if (ctype_xdigit($bin)) {
        return $bin;
    }
    return bin2hex($bin);
}
function asciitohex($ascii)
{
    $hex = NULL;
    for ($i = 0; $i < strlen($ascii); $i++) {
        $byte = strtoupper(dechex(ord($ascii[$i])));
        $hex .= str_pad($byte, 2, 0, STR_PAD_LEFT);
    }
    return $hex;
}
function hextoascii($hex)
{
    $ascii = NULL;
    $string = str_replace(" ", NULL, $hex);
    $i = 0;
    while ($i < strlen($string)) {
        $ascii .= chr(hexdec(substr($string, $i, 2)));
        $i += 2;
    }
    return $ascii;
}
function char($input, $length)
{
    $result = array();
    for ($i = 0; $i < strlen($input); $i++) {
        $result[] = ord($input[$i]);
    }
    for ($i = strlen($input); $i < $length; $i++) {
        $result[] = 0;
    }
    return $result;
}
function password($num = 8, $lmin = true, $lmai = true, $numbers = true, $simb = false)
{
    $lemin = "abcdefghijklmnopqrstuvwxyz";
    $lemai = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    $nmeros = "0123456789";
    $simbolos = "^~`&()";
    $password = "";
    $chars = "";
    if ($lmin) {
        $chars .= $lemin;
    }
    if ($lmai) {
        $chars .= $lemai;
    }
    if ($numbers) {
        $chars .= $nmeros;
    }
    if ($simb) {
        $chars .= $simbolos;
    }
    $length = strlen($chars);
    for ($i = 1; $i <= $num; $i++) {
        $rand = mt_rand(1, $length);
        $password .= $chars[$rand - 1];
    }
    return $password;
}
function __($string, $args = array())
{
    return vsprintf(registry("translate")->_($string), $args);
}
function _p($singular, $plural, $number, $args = array())
{
    return vsprintf(registry("translate")->plural($singular, $plural, $number), $args);
}
function _e($string, $args = array())
{
    $translate = registry("translate");
    $locale = $translate->getAdapter()->getLocale();
    $translate->setLocale(config("locale", "pt_BR"));
    $text = __($string, $args);
    $translate->setLocale($locale);
    return $text;
}
function partial($partial, $data = array())
{
    app()->view()->partial($partial, $data);
}
function logged_in()
{
    return Morpheus\Account\Auth::loggedIn();
}
function admin_logged_in()
{
    return Morpheus\Admin\Auth::loggedIn();
}
function is_ajax()
{
    $xhr = env("HTTP_X_REQUESTED_WITH");
    return !empty($xhr) && strtolower($xhr) == "xmlhttprequest";
}
function json($json = array())
{
    header("Content-type: application/json");
    $json = json_encode($json);
    if (ob_get_level() !== 0) {
        ob_clean();
    }
    echo $json;
    exit;
}
function message($message, $type = "success", $options = array())
{
    if (is_ajax()) {
        header("Content-type: application/json");
        $json = json_encode(array_merge(array("success" => $type === "success", is_array($message) ? "errors" : "message" => $message), $options));
        if (ob_get_level() !== 0) {
            ob_clean();
        }
        echo $json;
        exit;
    }
    $msg = new Plasticbrain\FlashMessages\FlashMessages();
    $msg->{$type}($message);
    if (isset($options["redirect"])) {
        if (ob_get_level() !== 0) {
            ob_clean();
        }
        header("Location: " . $options["redirect"], true, 302);
        exit;
    }
}
function success($message, $options = array())
{
    return message($message, "success", $options);
}
function error($message, $options = array())
{
    if ($message instanceof Exception) {
        $ex = $message;
        $message = $ex->getMessage();
        if (config("debug", false)) {
            $message .= "\n" . "line: " . $ex->getLine() . "\n" . "file: " . $ex->getFile();
        }
    }
    return message($message, "error", $options);
}
function flash($close = false)
{
    $msg = new Plasticbrain\FlashMessages\FlashMessages();
    if (!$close) {
        $msg->setCloseBtn("");
    }
    $msg->display();
}
function in_admin()
{
    return strpos(env("REQUEST_URI"), "/admin") !== false;
}
function util($class)
{
    if (strpos($class, ".")) {
        list($plugin, $class) = explode(".", $class);
        $class = $plugin . "\\Util\\" . $class;
    } else {
        $class = "Morpheus\\Util\\" . $class;
    }
    if (class_exists($class)) {
        return new $class();
    }
}
function user()
{
    return Morpheus\Account\Auth::getAccount();
}
function admin($field = NULL)
{
    return Morpheus\Admin\Auth::getUser($field);
}
function account($account = NULL)
{
    return new Morpheus\Account\Account($account);
}
function character($character = NULL, $account = NULL)
{
    return new Morpheus\Character\Character($character, $account);
}
function guild($guild = NULL)
{
    return new Morpheus\Guild\Guild($guild);
}
function server()
{
    return new Morpheus\Server\Server();
}
function has_skill_tree()
{
    return server()->hasSkillTree();
}
function has_castle_siege()
{
    return server()->hasCastleSiege();
}
function castle_siege()
{
    return server()->getSiegeInfo();
}
function onlines($server = NULL)
{
    return server()->getCharactersOnline($server);
}
function team()
{
    return server()->getMembersTeam();
}
function ranking($type, $options = array())
{
    $ranking = Morpheus\Ranking\Ranking::factory($type, $options);
    return $ranking->getQuery();
}
function server_status($server = NULL)
{
    $match = array();
    $servers = config("servers", array());
    if (isset($servers[$server])) {
        $match = $servers[$server];
    }
    if (empty($match) && 0 < count($servers)) {
        list($match) = array_values($servers);
    }
    $fs = @fsockopen($match["ip"], $match["port"], $error, $msg, 1);
    if ($fs) {
        fclose($fs);
    }
    return $fs;
}
function notify($name)
{
    if (app()) {
        $args = func_get_args();
        array_shift($args);
        Morpheus\Core\Plugin::getInstance()->notify($name, ...$args);
    }
}
function rework_upload_files($post)
{
    $files = array();
    $count = count($post["name"]);
    $keys = array_keys($post);
    for ($i = 0; $i < $count; $i++) {
        foreach ($keys as $key) {
            $files[$i][$key] = $post[$key][$i];
        }
    }
    return $files;
}
function authenticate()
{
    return function () {
        if (!logged_in()) {
            if (is_ajax()) {
                app()->contentType("application/json");
                return app()->halt(401, json_encode(array("success" => false, "partial" => "login", "error" => __("You must be logged"))));
            }
            return app()->redirect("/");
        }
    };
}
function plugin_license($plugin)
{
    return function () use($plugin) {
        $license = new Morpheus\Core\License();
        $data = $license->getData();
        if (!in_array($plugin, $data["plugins"])) {
            Morpheus\Core\Plugin::deactivate($plugin);
            return App::notFound();
        }
    };
}
function service($id)
{
    return function () use($id) {
        if (user() !== NULL) {
            $services = user()->getAvaliableServices();
            $service = Morpheus\Account\Service::getService($id);
            if ($service != NULL && !$service["allowed"] && !isset($services[$id]) || !$service["active"]) {
                return app()->redirect("/");
            }
            $rates = Morpheus\Account\Service::getRatesForAccount($id, user());
            app()->view()->appendData(array("rates" => $rates));
        } else {
            if (is_ajax()) {
                app()->contentType("application/json");
                return app()->halt(401, json_encode(array("success" => false, "partial" => "login", "error" => __("You must be logged"))));
            }
            return app()->redirect("/");
        }
    };
}
function has_vip_system()
{
    return config("vip.table");
}
function fetch($type)
{
    return app()->view()->assign($type);
}
function attach($class, $data)
{
    foreach ($data as $key => $value) {
        $method = "set" . ucfirst($key);
        if (method_exists($class, $method)) {
            $class->{$method}($value);
        }
    }
}
function app($name = "default")
{
    return Morpheus\Slim\Application::getInstance($name);
}
function plugin_active($plugin)
{
    return Morpheus\Core\Plugin::isActive($plugin);
}
function registry($name, $value = NULL)
{
    if ($value == NULL) {
        return Zend_Registry::get($name);
    }
    Zend_Registry::set($name, $value);
}
function pe($ex, $die = false)
{
    $message = $ex instanceof Exception ? $ex->getMessage() : $ex;
    echo "<div style=\"padding:5px;color:#fff;background:red;text-align:center\">" . $message . "</div>";
    if ($die) {
        exit;
    }
}
function upload($file, array $options = array())
{
    $options = $options + array("rename" => true, "name" => uniqid(), "filesize" => NULL, "allowed" => NULL);
    if (!empty($file)) {
        $dir = ROOT . DS . $options["dir"];
        $upload = new upload($file, config("locale", "pt_BR"));
        $upload->file_new_name_body = $options["name"];
        $upload->file_auto_rename = $options["rename"];
        if ($options["filesize"]) {
            $upload->file_max_size = $options["filesize"];
        }
        if ($options["allowed"]) {
            $upload->allowed = $options["allowed"];
        } else {
            $upload->allowed = array("image/png", "image/jpeg");
        }
        $upload->process($dir);
        if ($upload->processed) {
            $upload->clean();
            return array("success" => true, "filename" => $upload->file_dst_name);
        }
        return array("success" => false, "error" => $upload->error);
    }
    return array("success" => false, "error" => __("Empty file"));
}
function generate_token($length = 20)
{
    return bin2hex(random_bytes($length));
}
function sql_tables()
{
    return Connection::fetchAll("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = ? ORDER BY TABLE_NAME", array("BASE TABLE"));
}
function sql_table_columns($table)
{
    return Connection::fetchAll("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ?", array($table));
}
function table_with_db($table = "")
{
    return Morpheus\Database\Connection::getTableWithDb($table);
}

?>