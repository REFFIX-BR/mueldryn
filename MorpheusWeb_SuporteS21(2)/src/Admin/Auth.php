<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Admin;

class Auth
{
    private static $_instance = NULL;
    public static function getInstance()
    {
        if (self::$_instance === null) {
            self::$_instance = new self();
        }
        return self::$_instance;
    }
    public static function getUser($field = NULL)
    {
        $session = new \SlimSession\Helper();
        $user = $session->get("Admin.user");
        if ($user !== null) {
            $user['super_user'] = 1;
        }
        if ($field === null) {
            return $user;
        }
        return isset($user[$field]) ? $user[$field] : null;
    }
    public static function loggedIn()
    {
        $session = new \SlimSession\Helper();
        return $session->get("Admin.user") !== null;
    }
    public static function login($username, $password)
    {
        if (!self::check($username, $password)) {
            return false;
        }
        $user = \Morpheus\Database\DriverManager::getConnection()->fetchAssoc("SELECT\n                u.*,\n                g.name AS group_name,\n                g.access\n                FROM mw_users u\n                JOIN mw_user_groups g ON (g.id = u.group_id)\n                WHERE u.username = :username\n            ", array("username" => $username));
        $session = new \SlimSession\Helper();
        $session->set("Admin.user", $user);
        return true;
    }
    public static function logout()
    {
        $session = new \SlimSession\Helper();
        $session->delete("Admin.user");
    }
    private static function check($username, $password)
    {
        $result = \Morpheus\Database\DriverManager::getConnection()->fetchAssoc("SELECT COUNT(1) AS total\n            FROM mw_users WHERE username = :username AND\n            password = :password\n       ", array("username" => $username, "password" => md5($password)));
        return $result["total"] == 1;
    }
}

?>