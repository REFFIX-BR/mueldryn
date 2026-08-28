<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Account;

class Auth
{
    private static $_instance = NULL;
    private $_account = NULL;
    private static $_needValidation = true;
    public static function getInstance()
    {
        if (static::$_instance === null) {
            static::$_instance = new self();
        }
        return static::$_instance;
    }
    public static function getAccount()
    {
        $instance = static::getInstance();
        if (self::loggedIn()) {
            if ($instance->_account === null) {
                $session = new \SlimSession\Helper();
                $instance->_account = new Account($session->get("Morpheus.username"));
            }
            return $instance->_account;
        }
    }
    public static function loggedIn()
    {
        $session = new \SlimSession\Helper();
        if (static::$_needValidation) {
            static::$_needValidation = false;
            notify("auth.before_validate");
        }
        return $session->get("Morpheus.username") !== null;
    }
    public static function login($username, $password)
    {
        if (!static::check($username, $password)) {
            return false;
        }
        $session = new \SlimSession\Helper();
        $session->set("Morpheus.username", $username);
        return true;
    }
    public static function logout()
    {
        $session = new \SlimSession\Helper();
        $session->delete("Morpheus.username");
    }
    private static function check($username, $password)
    {
        // 1) OpenMU (Postgres / MuMain) — fonte real das contas
        if (\Morpheus\OpenMU\Bridge::enabled()) {
            try {
                if (\Morpheus\OpenMU\Bridge::verifyPassword($username, $password)) {
                    \Morpheus\OpenMU\Bridge::syncToMorpheus($username);
                    return true;
                }
                // Se a conta existe no OpenMU e a senha falhou, não cai no MSSQL legado
                if (\Morpheus\OpenMU\Bridge::accountExists($username)) {
                    return false;
                }
            } catch (\Exception $e) {
                error_log("OpenMU auth error: " . $e->getMessage());
            }
        }
        // 2) Fallback MSSQL clássico (legado)
        $total = \Morpheus\Database\Connection::fetchColumn("select count(1) as total\n           from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . " where memb___id = ? and\n           memb__pwd = " . (config("use_md5", false) ? "dbo.hashmd5('" . $username . "', ?)" : "?") . "\n       ", array($username, $password));
        return $total == 1;
    }
}

?>