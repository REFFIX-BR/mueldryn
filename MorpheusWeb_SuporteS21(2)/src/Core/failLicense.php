<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Core;

class License
{
    private $_key = "YmUzDYWM2sNGU24NbA363zA7IDFGB5aVi35BDFGQ3YNO36ycDFGAATq4sYmSFVDFGDdEzGDDw96OnMW3kjCFJ7M1";
    private $_id = "jDi2309Dnd24is2lHd4";
    private $_begin = "BEGIN LICENSE KEY";
    private $_end = "END LICENSE KEY";
    private $_uris = 2;
    private $_mac = NULL;
    private $_server = array();
    private $_vars = array();
    private $_ips = array();
    private $_local = false;
    public function __construct()
    {
        $this->_mac = $this->getMacAddress();
        $this->_ips = $this->getIpAddress();
        $this->_server = $this->getServerInfo();
    }
    public function update($id)
    {
        $filename = $filename = ROOT . DS . "license.txt";
        $http = \Morpheus\Network\Http::create("GET", LICENSE_SERVER_URL . "license/update", array("query" => array("mac" => $this->getMacAddress(), "ips" => join(",", $this->getIpAddress()), "domain" => $this->_server["SERVER_NAME"], "id" => $id)))->send();
        $response = $http->getResponse();
        if ($response["header"]["status"] === 200) {
            $data = (array) json_decode($response["body"]);
            if ($data["success"]) {
                file_put_contents($filename, $data["license"]);
            }
        }
    }
    public function validate()
    {
        $filename = ROOT . DS . "license.txt";
        /*if (!file_exists($filename)) {
            return array("result" => "ILLEGAL");
        }*/
        $license = file_get_contents($filename);
        $time = filemtime($filename);
        $diff = time() - $time;
        $data = $this->unwrap($license);
        $data["result"] = "OK";
       // $data["result"] = $data["status"];

            foreach (config("plugins", array()) as $plugin) {
                
                    $info = Plugin::getPluginInfo($plugin);
                    
                    
            }
        /*if (3 * 60 * 60 <= $diff || !trim($license)) {
            $this->update($data["id"]);
            $license = file_get_contents($filename);
            $data = $this->unwrap($license);
        } else {
            if (3 * 24 * 60 * 60 <= $diff) {
                return array("result" => "ILLEGAL");
            }
        }*/
        /*if (is_array($data)) {
            if (isset($data["status"])) {
                $data["result"] = $data["status"];
            } else {
                if (!empty($data["date"]["end"]) && strtotime($data["date"]["end"]) - time() < 0) {
                    $data["result"] = "EXPIRED";
                }
                $mac = $data["server"]["mac"] === $this->_mac;
                $domain = $this->compareDomainIp($data["server"]["domain"], $this->_ips);
                $ips = !trim($data["server"]["ips"]) ? array() : explode(",", trim($data["server"]["ips"]));
                $allowIp = false;
                foreach ($ips as $ip) {
                    if (in_array($ip, $this->_ips)) {
                        $allowIp = true;
                        break;
                    }
                }*/
                /*if ($domain && ($mac || $allowIp)) {
                    if (!isset($data["result"])) {
                        $data["result"] = "OK";
                        foreach (config("plugins", array()) as $plugin) {
                            if (!in_array($plugin, $data["plugins"])) {
                                $info = Plugin::getPluginInfo($plugin);
                                if (!isset($info["license"]) || $info["license"]) {
                                    Plugin::deactivate($plugin);
                                }
                            }
                        }
                    }
                }else {
                    $data["result"] = "ILLEGAL";
                }
            }
            switch ($data["result"]) {
                case "CORRUPT":
                    return app()->redirect(MORPHEUS_WEBSITE . "license-corrupt");
                case "EXPIRED":
                    return app()->redirect(MORPHEUS_WEBSITE . "license-expired");
                case "ILLEGAL":
                    return app()->redirect(MORPHEUS_WEBSITE . "license-illegal");
                case "INVALID":
                    return app()->redirect(MORPHEUS_WEBSITE . "license-invalid");
            }
        } else {
            return app()->redirect(MORPHEUS_WEBSITE . "license-invalid");
        }*/
    }
    protected function compareDomainIp($domain, $ips = false)
    {
        if (!$ips) {
            $ips = $this->getIpAddress();
        }
        $domainIps = gethostbynamel(trim($domain));
        if (is_array($domainIps) && 0 < count($domainIps)) {
            foreach ($domainIps as $ip) {
                if (in_array($ip, $ips)) {
                    return true;
                }
            }
        }
        return false;
    }
    private function _pad($str)
    {
        $len = strlen($str);
        $spaces = (80 - $len) / 2;
        $str1 = "";
        for ($i = 0; $i < $spaces; $i++) {
            $str1 = $str1 . "-";
        }
        if ($spaces / 2 !== round($spaces / 2)) {
            $str = substr($str1, 0, strlen($str1) - 1) . $str;
        } else {
            $str = $str1 . $str;
        }
        $str = $str . $str1;
        return $str;
    }
    protected function decrypt($str)
    {
        $iv = substr(hash("sha256", "morpheusmuweb"), 0, 16);
        return json_decode(openssl_decrypt($str, "AES-256-CBC", $this->_key, 0, $iv), true);
    }
    
    public function unwrap($str)
    {
        $begin = $this->_pad($this->_begin);
        $end = $this->_pad($this->_end);
        $str = trim(str_replace(array($begin, $end, "\r", "\n", "\t"), "", $str));
        return $this->decrypt($str);
    }
    protected function getOSVar($varName, $os)
    {
        $varName = strtolower($varName);
        switch ($os) {
            case "freebsd":
            case "netbsd":
            case "solaris":
            case "sunos":
            case "darwin":
                switch ($varName) {
                    case "conf":
                        $var = "/sbin/ifconfig";
                        break;
                    case "mac":
                        $var = "ether";
                        break;
                    case "ip":
                        $var = "inet ";
                        break;
                }
                break;
            case "linux":
                switch ($varName) {
                    case "conf":
                        $var = "/sbin/ifconfig";
                        break;
                    case "mac":
                        $var = "HWaddr";
                        break;
                    case "ip":
                        $var = "inet addr:";
                        break;
                }
                break;
        }
        return $var;
    }
    protected function getConfig()
    {
        if (ini_get("safe_mode")) {
            return "SAFE_MODE";
        }
        $os = strtolower(PHP_OS);
        if (substr($os, 0, 3) === "win") {
            @exec("ipconfig/all", $lines);
            if (count($lines) === 0) {
                return "ERROR_OPEN";
            }
            $conf = implode(PHP_EOL, $lines);
        } else {
            $osFile = $this->getOSVar("conf", $os);
            $fp = @popen($osFile, "rb");
            if (!$fp) {
                return "ERROR_OPEN";
            }
            $conf = @fread($fp, 4096);
            @pclose($fp);
        }
        return $conf;
    }
    public function getIpAddress()
    {
        $ips = array();
        $conf = $this->getConfig();
        if ($conf != "SAFE_MODE" && $conf != "ERROR_OPEN") {
            $os = strtolower(PHP_OS);
            if (substr($os, 0, 3) !== "win") {
                $lines = explode(PHP_EOL, $conf);
                $ipDelim = $this->getOSVar("ip", $os);
                $num = "(\\d|[1-9]\\d|1\\d\\d|2[0-4]\\d|25[0-5])";
                foreach ($lines as $key => $line) {
                    if (!preg_match("/^" . $num . "\\." . $num . "\\." . $num . "\\." . $num . "\$/", $line) && strpos($line, $ipDelim)) {
                        $ip = substr($line, strpos($line, $ipDelim) + strlen($ipDelim));
                        $ip = trim(substr($ip, 0, strpos($ip, " ")));
                        if (!isset($ips[$ip])) {
                            $ips[$ip] = $ip;
                        }
                    }
                }
            }
        }
        $server = env("SERVER_NAME");
        if ($server !== null) {
            $ip = gethostbyname($server);
            if (!isset($ips[$ip])) {
                $ips[$ip] = $ip;
            }
        }
        $addr = env("SERVER_ADDR");
        if ($addr !== null) {
            $name = gethostbyaddr($addr);
            $ip = gethostbyname($name);
            if (!isset($ips[$ip])) {
                $ips[$ip] = $ip;
            }
            if (isset($addr) && $addr != $addr && !isset($ips[$addr])) {
                $ips[$addr] = $addr;
            }
        }
        if (0 < count($ips)) {
            return $ips;
        }
        if ($conf === "SAFE_MODE" || $conf === "ERROR_OPEN") {
            return $conf;
        }
        return "IP_404";
    }
    public function getMacAddress()
    {
        $conf = $this->getConfig();
        $os = strtolower(PHP_OS);
        if (substr($os, 0, 3) === "win") {
            $lines = explode(PHP_EOL, $conf);
            foreach ($lines as $key => $line) {
                if (preg_match("/([0-9a-f][0-9a-f][-:]){5}([0-9a-f][0-9a-f])/i", $line)) {
                    $trimmed = trim($line);
                    return trim(substr($trimmed, strrpos($trimmed, " ")));
                }
            }
        } else {
            $mac = $this->getOSVar("mac", $os);
            $pos = strpos($conf, $mac);
            if ($pos) {
                $str1 = trim(substr($conf, $pos + strlen($mac)));
                return trim(substr($str1, 0, strpos($str1, "\n")));
            }
        }
        return "MAC_404";
    }
    protected function getServerInfo()
    {
        if (empty($this->_vars)) {
            $this->_vars = $_SERVER;
        }
        $a = array();
        if (isset($this->_vars["SERVER_ADDR"]) && (!strrpos($this->_vars["SERVER_ADDR"], "127.0.0.1") || $this->_local)) {
            $a["SERVER_ADDR"] = $this->_vars["SERVER_ADDR"];
        }
        if (isset($this->_vars["HTTP_HOST"]) && (!strrpos($this->_vars["HTTP_HOST"], "127.0.0.1") || $this->_local)) {
            $a["HTTP_HOST"] = $this->_vars["HTTP_HOST"];
        }
        if (isset($this->_vars["SERVER_NAME"])) {
            $a["SERVER_NAME"] = $this->_vars["SERVER_NAME"];
        }
        if (isset($this->_vars["PATH_TRANSLATED"])) {
            $a["PATH_TRANSLATED"] = substr($this->_vars["PATH_TRANSLATED"], 0, strrpos($this->_vars["PATH_TRANSLATED"], "/"));
        } else {
            if (isset($this->_vars["SCRIPT_FILENAME"])) {
                $a["SCRIPT_FILENAME"] = substr($this->_vars["SCRIPT_FILENAME"], 0, strrpos($this->_vars["SCRIPT_FILENAME"], "/"));
            }
        }
        if (isset($this->_vars["SCRIPT_URI"])) {
            $a["SCRIPT_URI"] = substr($this->_vars["SCRIPT_URI"], 0, strrpos($this->_vars["SCRIPT_URI"], "/"));
        }
        if (count($a) < $this->_uris) {
            return "SERVER_FAILED";
        }
        return $a;
    }
    public function xhyt()
    {
    }
}

?>