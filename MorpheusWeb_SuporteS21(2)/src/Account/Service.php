<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Account;

class Service
{
    private static $_instance = NULL;
    private $_services = array();
    public static function getInstance()
    {
        if (static::$_instance === null) {
            static::$_instance = new self();
        }
        return static::$_instance;
    }
    public static function getAllServices()
    {
        $instance = static::getInstance();
        if (empty($instance->_services)) {
            $services = \Morpheus\Database\Connection::fetchAll("select *,\n                0 as notification \n                from mw_services \n                order by sequence\n            ");
            foreach ($services as $service) {
                $types = config("vip.types");
                $viptypes = array();
                foreach (\Morpheus\Database\Connection::fetchAll("select * from mw_viptype_services where service_id = ?", array($service["id"])) as $viptype) {
                    $viptypes[$viptype["viptype"]] = $types[$viptype["viptype"]];
                }
                $instance->_services[$service["service"]] = array_merge($service, array("viptypes" => $viptypes));
            }
        }
        return $instance->_services;
    }
    public static function getServiceById($id)
    {
        $services = static::getAllServices();
        $service = null;
        foreach ($services as $s) {
            if ($s["id"] === $id) {
                $service = $s;
                break;
            }
        }
        return $service;
    }
    public static function getService($service)
    {
        if (isset(static::getAllServices()[$service])) {
            return static::getAllServices()[$service];
        }
        return null;
    }
    public static function setNotification($service, $notification)
    {
        $instance = self::getInstance();
        static::getAllServices();
        $instance->_services[$service]["notification"] = $notification;
    }
    public static function newService($service)
    {
        \Morpheus\Database\Connection::transactional(function ($conn) use($service) {
            $parent = 0;
            if (isset($service["parent_id"])) {
                $parent = $conn->fetchColumn("SELECT id FROM mw_services WHERE service = ?", array($service["parent_id"]));
            }
            $data = array("name" => $service["name"], "allowed" => isset($service["allowed"]) && $service["allowed"] === true, "service" => $service["service"], "active" => true, "sequence" => 0, "parent_id" => $parent);
            $types = array("string", "boolean", "string", "boolean", "integer", "integer");
            if (isset($service["url"])) {
                $data += array("url" => $service["url"]);
                $types += array("string");
            }
            if (isset($service["config_url"])) {
                $data += array("config_url" => $service["config_url"]);
                $types += array("string");
            }
            $conn->insert("mw_services", $data, $types);
        });
    }
    public static function deactivate($service)
    {
        \Morpheus\Database\Connection::update("mw_services", array("active" => false), array("service" => $service), array("boolean"));
    }
    public static function activate($service)
    {
        \Morpheus\Database\Connection::update("mw_services", array("active" => true), array("service" => $service), array("boolean"));
    }
    public static function addRate($service, $data = array())
    {
        $service = static::getService($service);
        $rates = json_decode($service["rates"], true);
        $rate = array("conditions" => array_keys(empty($data["conditions"]) ? array() : $data["conditions"]), "type" => $data["type"], "amount" => $data["amount"], "coin" => $data["type"] === "coin" ? $data["coin"] : "");
        $rates[] = $rate;
        \Morpheus\Database\Connection::update("mw_services", array("rates" => json_encode($rates)), array("service" => $service["service"]));
    }
    public static function deleteRate($service, $index)
    {
        $service = static::getService($service);
        $rates = json_decode($service["rates"], true);
        unset($rates[$index]);
        \Morpheus\Database\Connection::update("mw_services", array("rates" => json_encode($rates)), array("service" => $service["service"]));
    }
    public static function getRatesForAccount($service, Account $account)
    {
        $service = static::getService($service);
        $rates = json_decode($service["rates"], true);
        $data = array();
        if (!empty($rates)) {
            foreach ($rates as $rate) {
                if (in_array($account->getVipType(), $rate["conditions"])) {
                    $data[] = $rate;
                }
            }
        }
        return $data;
    }
    public static function payRates($service, Account $account, \Morpheus\Character\Character $character = NULL)
    {
        $rates = static::getRatesForAccount($service, $account);
        $error = false;
        foreach ($rates as $rate) {
            if ($rate["amount"] < 0) {
                $amount = $rate["amount"] * -1;
                if ($rate["type"] === "coin") {
                    if ($account->getCoin($rate["coin"]) < $amount) {
                        $error = __("You do not have enough %s", array(config("coins")[$rate["coin"]]["name"]));
                        break;
                    }
                } else {
                    if ($rate["type"] === "zen" && $character !== null && $character->getMoney() < $amount) {
                        $error = __("You do not have enough %s", array("zen"));
                        break;
                    }
                }
            }
        }
        if (!$error) {
            if (!empty($rates)) {
                foreach ($rates as $rate) {
                    if ($rate["type"] === "coin") {
                        $account->addCoin($rate["coin"], $rate["amount"]);
                    } else {
                        if ($rate["type"] === "zen" && $character !== null) {
                            $character->addMoney($rate["amount"]);
                        }
                    }
                }
                $account->update();
                if ($character !== null) {
                    $character->update();
                }
            }
            return true;
        } else {
            throw new \Exception($error);
        }
    }
}

?>