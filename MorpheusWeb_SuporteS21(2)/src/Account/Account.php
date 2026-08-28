<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Account;

class Account
{
    private $_name = NULL;
    private $_username = NULL;
    private $_password = NULL;
    private $_email = NULL;
    private $_confirmedEmail = NULL;
    private $_phone = NULL;
    private $_secretQuestion = NULL;
    private $_secretAnswer = NULL;
    private $_blocked = NULL;
    private $_personalId = NULL;
    private $_binWarehouse = NULL;
    private $_connected = NULL;
    private $_characters = NULL;
    private $_credit = NULL;
    private $_token = NULL;
    private $_vipType = NULL;
    private $_vipExpire = NULL;
    private $_coins = array();
    private $_data = array();
    private $_services = array();
    private $_customFields = array();
    private $_lastCharacter = NULL;
    private $_warehouse = NULL;
    private $_extWarehouses = NULL;
    private $_schema = array();
    public function __construct($username = NULL)
    {
        if ($username !== null) {
            $this->read($username);
        }
    }
    public function setName($name)
    {
        $this->_name = $name;
        return $this;
    }
    public function getName()
    {
        return $this->_name;
    }
    public function setUsername($username)
    {
        $this->_username = strtolower($username);
        return $this;
    }
    public function getUsername()
    {
        return $this->_username;
    }
    public function setPassword($password)
    {
        $this->_password = $password;
        return $this;
    }
    public function getPassword()
    {
        return $this->_password;
    }
    public function setEmail($email)
    {
        $this->_email = $email;
        return $this;
    }
    public function getEmail()
    {
        return $this->_email;
    }
    public function setConfirmedEmail($confirmedEmail)
    {
        $this->_confirmedEmail = (bool) $confirmedEmail;
        return $this;
    }
    public function isConfirmedEmail()
    {
        return $this->_confirmedEmail;
    }
    public function setPhone($phone)
    {
        $this->_phone = $phone;
        return $this;
    }
    public function getPhone()
    {
        return $this->_phone;
    }
    public function setSecretQuestion($question)
    {
        $this->_secretQuestion = $question;
        return $this;
    }
    public function getSecretQuestion()
    {
        return $this->_secretQuestion;
    }
    public function setSecretAnswer($answer)
    {
        $this->_secretAnswer = $answer;
        return $this;
    }
    public function getSecretAnswer()
    {
        return $this->_secretAnswer;
    }
    public function setBlocked($blocked)
    {
        $this->_blocked = (bool) $blocked;
        return $this;
    }
    public function isBlocked()
    {
        return $this->_blocked;
    }
    public function setPersonalId($id)
    {
        $this->_personalId = $id;
        return $this;
    }
    public function getPersonalId()
    {
        return $this->_personalId;
    }
    public function setCredit($credit)
    {
        $this->_credit = $credit;
        return $this;
    }
    public function getCredit()
    {
        return intval($this->_credit);
    }
    public function addCredit($credit)
    {
        $this->_credit += $credit;
        return $this;
    }
    public function setToken($token)
    {
        $this->_token = $token;
        return $this;
    }
    public function getToken()
    {
        return $this->_token;
    }
    public function setBinWarehouse($warehouse)
    {
        $this->_binWarehouse = $warehouse;
        return $this;
    }
    public function getBinWarehouse()
    {
        return $this->_binWarehouse;
    }
    public function setVipType($vipType)
    {
        $this->_vipType = $vipType;
        return $this;
    }
    public function getVipType()
    {
        return intval($this->_vipType);
    }
    public function setVipExpire($vipExpire)
    {
        $this->_vipExpire = $vipExpire instanceof \DateTime ? $vipExpire : new \DateTime($vipExpire);
        return $this;
    }
    public function getVipExpire()
    {
        return $this->_vipExpire;
    }
    public function setCoins(array $coins)
    {
        $this->_coins = $coins;
        return $this;
    }
    public function setCoin($name, $value)
    {
        $this->_coins[$name] = $value;
        return $this;
    }
    public function addCoin($name, $value)
    {
        $coin = config("coins")[$name];
        if (!isset($this->_coins[$name]) && $coin) {
            $this->_coins[$name] = 0;
        }
        $this->_coins[$name] += $value;
        return $this;
    }
    public function getCoins()
    {
        return $this->_coins;
    }
    public function getCoin($coin)
    {
        return isset($this->_coins[$coin]) ? $this->_coins[$coin] : null;
    }
    public function setCustomFields($fields)
    {
        $this->_customFields = $fields;
        return $this;
    }
    public function addCustomField($field, $value)
    {
        $this->_customFields[$field] = $value;
        return $this;
    }
    public function getCustomField($field)
    {
        return $this->_customFields[$field];
    }
    public function getCustomFields()
    {
        return $this->_customFields;
    }
    public function setConnected($connected)
    {
        $this->_connected = $connected;
        return $this;
    }
    public function isConnected()
    {
        return $this->_connected;
    }
    public function toArray()
    {
        return $this->_data;
    }
    public function exists()
    {
        return $this->getUsername() !== null;
    }
    public function getCharacters()
    {
        if ($this->_characters === null) {
            $this->_characters = array();
            $results = \Morpheus\Database\Connection::fetchAll("SELECT\n                Name AS name FROM Character\n                WHERE AccountID = :username\n            ", array("username" => $this->getUsername()));
            foreach ($results as $result) {
                $character = new \Morpheus\Character\Character($result["name"]);
                $this->_characters[] = $character;
                if (!isset($this->_data["characters"])) {
                    $this->_data["characters"] = array();
                }
                $this->_data["characters"][] = $character->toArray();
            }
        }
        return $this->_characters;
    }
    public function getWarehouse()
    {
        if ($this->exists() && $this->_warehouse === null) {
            $slice = 1;
            if (config("warehouse.extended", false)) {
                $slice = 2;
            }
            $this->_warehouse = new Warehouse($this, 8, 15 * $slice, $slice);
        }
        return $this->_warehouse;
    }
    public function getExtWarehouses()
    {
        if ($this->exists() && $this->_extWarehouses === null) {
            $warehouses = \Morpheus\Database\Connection::fetchAll("select CONVERT(VARBINARY(MAX), Items) as Items, \n                from " . config("warehouse.ext.table") . "\n                where " . config("warehouse.ext.account_column") . " = ?\n            ", array($this->getUsername()));
            foreach ($warehouses as $warehouse) {
            }
        }
        return $this->_extWarehouses;
    }
    public function getExtWarehouse($index)
    {
        $warehouses = $this->getExtWarehouses();
        return isset($warehouses[$index]) ? $warehouses[$index] : null;
    }
    public function getSchema()
    {
        if (!empty($this->_schema)) {
            return $this->_schema;
        }
        $dbname = \Morpheus\Database\Connection::getConfig("me", \Morpheus\Database\Connection::getConfig("dbname"));
        $columns = \Morpheus\Database\Connection::fetchAll("select *\n            from " . $dbname . ".INFORMATION_SCHEMA.COLUMNS\n            where TABLE_NAME = ?\n            and TABLE_CATALOG = ?\n        ", array("MEMB_INFO", $dbname));
        foreach ($columns as $column) {
            $this->_schema[$column["COLUMN_NAME"]] = $column;
        }
        return $this->_schema;
    }
    public function read($username = NULL)
    {
        $fields = array("mi.memb_guid as guid", "mi.memb___id as username", "mi.memb__pwd as password", "mi.memb_name as name", "mi.mail_addr as email", "mi.mail_chek as confirmedEmail", "mi.tel__numb as phone", "mi." . config("columns.credit") . " as credit", "mi." . config("columns.token") . " as token", "mi.fpas_ques as secretQuestion", "mi.fpas_answ as secretAnswer", "mi.bloc_code as blocked", "mi.appl_days as createdAt", "ms.ServerName as serverName", "ms.ConnectTM as connectedAt", "ms.DisConnectTM as disconnectedAt", "ms.ConnectStat as connected", "ms.IP AS ip", "CONVERT(VARBINARY(MAX), w.Items) as binWarehouse", "ac.GameIDC as lastUsedCharacter");
        if (config("server.team", "gmo") === "xteam") {
            $fields[] = "replace((case when len(mi.sno__numb) > 7 then substring(mi.sno__numb, 7, 7) else mi.sno__numb end), ' ', '') as personalId";
        } else {
            $fields[] = "replace((case when len(mi.sno__numb) > 7 then substring(mi.sno__numb, len(mi.sno__numb) - 6, len(mi.sno__numb)) else mi.sno__numb end), ' ', '') as personalId";
        }
        foreach (config("coins", array()) as $coin) {
            $fields[] = "(select " . $coin["column"] . " from " . $coin["table"] . " where " . $coin["foreign_key"] . " = :username) as " . $coin["id"];
        }
        $isVipMembInfo = strpos(strtoupper(config("vip.table")), "MEMB_INFO") !== false;
        if (config("vip.column_type")) {
            $fields[] = ($isVipMembInfo ? "mi" : "v") . "." . config("vip.column_type") . " as vipType";
        }
        if (config("vip.column_expire")) {
            $fields[] = ($isVipMembInfo ? "mi" : "v") . "." . config("vip.column_expire") . " as vipExpire";
        }
        foreach (config("register.custom_fields", array()) as $field) {
            $fields[] = "mi." . $field["name"] . " as custom" . util("Inflector")->humanize($field["name"]);
        }
        $sql = "select " . join(",", $fields) . "\n            from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . " mi\n            left join " . \Morpheus\Database\Connection::getTableWithDb("MEMB_STAT") . " ms on mi.memb___id = ms.memb___id COLLATE DATABASE_DEFAULT\n            left join AccountCharacter ac on mi.memb___id = ac.Id COLLATE DATABASE_DEFAULT\n            left join warehouse w on mi.memb___id = w.AccountID COLLATE DATABASE_DEFAULT\n            " . (config("vip.table") && config("vip.table") !== "MEMB_INFO" ? "left join " . config("vip.table") . " v ON v." . config("vip.column_account") . " = mi.memb___id COLLATE DATABASE_DEFAULT" : "") . "\n            WHERE mi.memb___id = :username";
        $result = \Morpheus\Database\Connection::fetchAssoc($sql, array(":username" => $username == null ? $this->getUsername() : $username));
        if (!empty($result)) {
            $result["coins"] = array();
            foreach (config("coins", array()) as $coin) {
                $result["coins"][$coin["id"]] = $result[$coin["id"]];
                unset($result[$coin["id"]]);
            }
            foreach (config("register.custom_fields", array()) as $field) {
                if (isset($result["custom" . util("Inflector")->humanize($field["name"])])) {
                    $result["customField"][$field["name"]] = $result["custom" . util("Inflector")->humanize($field["name"])];
                }
            }
            attach($this, $result);
            $this->_data = $result;
        }
    }
    public function updateCoins()
    {
        foreach (config("coins", array()) as $coin) {
            if ($this->getCoin($coin["id"]) !== null) {
                $total = \Morpheus\Database\Connection::fetchColumn("select count(1) as total \n                    from " . $coin["table"] . " \n                    where " . $coin["foreign_key"] . " = :username\n                ", array("username" => $this->getUsername()));
                $isMembInfo = strpos(strtoupper($coin["table"]), "MEMB_INFO") !== false;
                if (!$isMembInfo && $total == 0) {
                    \Morpheus\Database\Connection::insert($coin["table"], array($coin["column"] => $this->getCoin($coin["id"]), $coin["foreign_key"] => $this->getUsername()), array("integer", "string"));
                } else {
                    \Morpheus\Database\Connection::update($coin["table"], array($coin["column"] => $this->getCoin($coin["id"])), array($coin["foreign_key"] => $this->getUsername()), array("integer"));
                }
            }
        }
    }
    public function updateVip()
    {
        if (config("vip.table")) {
            $total = \Morpheus\Database\Connection::fetchColumn("select count(1) as total \n                from " . config("vip.table") . " \n                where " . config("vip.column_account") . " = ?\n            ", array($this->getUsername()));
            $dbname = \Morpheus\Database\Connection::getConfig("dbname");
            if (strpos(config("vip.table"), ".") !== false) {
                list($dbname) = explode(".", config("vip.table"));
            }
            $type = \Morpheus\Database\Connection::fetchColumn("select DATA_TYPE\n                from INFORMATION_SCHEMA.COLUMNS\n                where TABLE_NAME = ? \n                and TABLE_CATALOG = ?\n                and COLUMN_NAME = ?\n            ", array(config("vip.table"), $dbname, config("vip.column_expire")));
            if ($type !== "datetime") {
                $type = "date";
            }
            $isMembInfo = strpos(strtoupper(config("vip.table")), "MEMB_INFO") !== false;
            if (!$isMembInfo && $total == 0) {
                \Morpheus\Database\Connection::insert(config("vip.table"), array(config("vip.column_type") => $this->getVipType(), config("vip.column_expire") => $this->getVipExpire(), config("vip.column_account") => $this->getUsername()), array("integer", $type, "string"));
            } else {
                \Morpheus\Database\Connection::update(config("vip.table"), array(config("vip.column_type") => $this->getVipType(), config("vip.column_expire") => $this->getVipExpire()), array(config("vip.column_account") => $this->getUsername()), array("integer", $type, "string"));
            }
        }
    }
    public function insert()
    {
        \Morpheus\Database\Connection::transactional(function () {
            $guid = $this->getNextGuid();
            $generate = config("generate_guid", true);
            if ($generate) {
                \Morpheus\Database\Connection::exec("SET IDENTITY_INSERT " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . " ON");
            }
            $columns = array("memb___id" => $this->getUsername(), "memb__pwd" => config("use_md5", false) ? "0" : $this->getPassword(), "memb_name" => substr($this->getName(), 0, $this->getSchema()["memb_name"]["CHARACTER_MAXIMUM_LENGTH"]), "sno__numb" => $this->_getSnoNumb(), "post_code" => 0, "addr_info" => 0, "addr_deta" => 0, "tel__numb" => substr($this->getPhone(), 0, $this->getSchema()["tel__numb"]["CHARACTER_MAXIMUM_LENGTH"]), "phon_numb" => substr($this->getPhone(), 0, $this->getSchema()["phon_numb"]["CHARACTER_MAXIMUM_LENGTH"]), "mail_addr" => $this->getEmail(), "mail_chek" => $this->isConfirmedEmail(), "bloc_code" => $this->isBlocked(), "ctl1_code" => 1, "fpas_ques" => $this->getSecretQuestion(), "fpas_answ" => $this->getSecretAnswer(), "appl_days" => (new \DateTime())->format(DATETIME_ISO_FORMAT), "modi_days" => (new \DateTime())->format(DATETIME_ISO_FORMAT), "out__days" => (new \DateTime())->format(DATETIME_ISO_FORMAT), "true_days" => (new \DateTime())->format(DATETIME_ISO_FORMAT), config("columns.credit") => $this->getCredit(), config("columns.token") => $this->getToken());
            $types = array("string", config("use_md5", false) ? \PDO::PARAM_LOB : "string", "string", "string", "integer", "integer", "integer", "string", "string", "string", "boolean", "boolean", "integer", "string", "string", "string", "string", "string", "string", "float", "string");
            if ($generate) {
                $columns["memb_guid"] = $guid;
                $types += array("integer");
            }
            foreach ($this->getCustomFields() as $column => $value) {
                $columns[$column] = $value;
                $types += array("string");
            }
            \Morpheus\Database\Connection::insert(\Morpheus\Database\Connection::getTableWithDb("MEMB_INFO"), $columns, $types);
            if (config("use_md5", false)) {
                \Morpheus\Database\Connection::executeUpdate("update " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . " set memb__pwd = dbo.hashmd5('" . $this->getUsername() . "', '" . $this->getPassword() . "') where memb___id = '" . $this->getUsername() . "'");
            }
            if ($generate) {
                \Morpheus\Database\Connection::exec("SET IDENTITY_INSERT " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . " OFF");
            }
            if (config("use_vi_curr_info", false)) {
                \Morpheus\Database\Connection::insert("VI_CURR_INFO", array("memb_guid" => $generate ? $guid : "1", "ends_days" => date("Y"), "chek_code" => true, "used_time" => 1234, "memb___id" => $this->getUsername(), "memb_name" => $this->getName(), "sno__numb" => 7, "Bill_Section" => 6, "Bill_value" => 3, "Bill_Hour" => 6, "Surplus_Point" => 6, "Surplus_Minute" => (new \DateTime())->format(DATETIME_ISO_FORMAT), "Increase_Days" => 0), array("string", "string", "boolean", "integer", "string", "string", "integer", "integer", "integer", "integer", "integer", "string", "integer"));
            }
            $this->updateVip();
            $this->updateCoins();
        });
    }
    public function update()
    {
        if ($this->getUsername() != null) {
            \Morpheus\Database\Connection::transactional(function () {
                $columns = array("memb_name" => substr($this->getName(), 0, $this->getSchema()["memb_name"]["CHARACTER_MAXIMUM_LENGTH"]), "sno__numb" => $this->_getSnoNumb(), "tel__numb" => substr($this->getPhone(), 0, $this->getSchema()["tel__numb"]["CHARACTER_MAXIMUM_LENGTH"]), "phon_numb" => substr($this->getPhone(), 0, $this->getSchema()["phon_numb"]["CHARACTER_MAXIMUM_LENGTH"]), "mail_addr" => $this->getEmail(), "mail_chek" => $this->isConfirmedEmail(), "fpas_ques" => $this->getSecretQuestion(), "fpas_answ" => $this->getSecretAnswer(), "bloc_code" => $this->isBlocked(), config("columns.credit") => $this->getCredit(), config("columns.token") => $this->getToken());
                $types = array("string", "string", "string", "string", "string", "boolean", "string", "string", "boolean", "float", "string");
                foreach ($this->getCustomFields() as $column => $value) {
                    $columns[$column] = $value;
                    $types += array("string");
                }
                \Morpheus\Database\Connection::update(\Morpheus\Database\Connection::getTableWithDb("MEMB_INFO"), $columns, array("memb___id" => $this->getUsername()), $types);
                if (config("use_vi_curr_info", false)) {
                    \Morpheus\Database\Connection::update("VI_CURR_INFO", array("memb_name" => $this->getName()), array("memb___id" => $this->getUsername()));
                }
                $this->updateVip();
                $this->updateCoins();
            });
        }
    }
    private function _getSnoNumb()
    {
        if (config("server.team", "gmo") === "xteam") {
            $snonumb = "111111" . $this->getPersonalId() . "11111";
        } else {
            $snonumb = str_pad($this->getPersonalId(), $this->getSchema()["sno__numb"]["CHARACTER_MAXIMUM_LENGTH"], "1", STR_PAD_LEFT);
        }
        return $snonumb;
    }
    public function updatePassword($password)
    {
        if (config("use_md5", false)) {
            \Morpheus\Database\Connection::executeUpdate("update " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . " set memb__pwd = dbo.hashmd5('" . $this->getUsername() . "', '" . $password . "') where memb___id = '" . $this->getUsername() . "'");
        } else {
            \Morpheus\Database\Connection::update(\Morpheus\Database\Connection::getTableWithDb("MEMB_INFO"), array("memb__pwd" => $password), array("memb___id" => $this->getUsername()));
        }
    }
    public function log($log, $type, $table = NULL, $key = NULL, $data = array())
    {
        \Morpheus\Database\Connection::insert("mw_logs", array("log" => $log, "account" => $this->getUsername(), "type" => $type, "table_name" => $table, "primary_key" => $key, "metadata" => json_encode($data)), array("string", "string", "string", "string", "string"));
    }
    public function count($where = NULL)
    {
        switch ($where) {
            case "banneds":
                $total = \Morpheus\Database\Connection::fetchColumn("select\n                    count(1) as total from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO") . "\n                    where bloc_code = 1");
                break;
            case "vips":
                $total = config("vip.table") ? \Morpheus\Database\Connection::fetchColumn("select\n                    count(1) as total from " . config("vip.table") . " where " . config("vip.column_type") . " > 0") : 0;
                break;
            default:
                $total = \Morpheus\Database\Connection::fetchColumn("select\n                    count(1) as total from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO"));
                break;
        }
        return $total;
    }
    public function getLastCharacter()
    {
        if ($this->_lastCharacter === null) {
            $idc = \Morpheus\Database\Connection::fetchColumn("select GameIDC \n                from AccountCharacter \n                where id = ?\n            ", array($this->getUsername()));
            if (!empty($idc)) {
                $this->_lastCharacter = new \Morpheus\Character\Character($idc, $this->getUsername());
            }
        }
        return $this->_lastCharacter;
    }
    public function getLastConnections()
    {
        $results = \Morpheus\Database\Connection::fetchAll("select\n            ac.GameIDC as character,\n            ms.IP as ip,\n            ms.ConnectStat as connect_stat,\n            ms.ServerName as server_name,\n            ms.ConnectTM as connected_at,\n            ms.DisConnectTM as disconnected_at\n            from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_STAT") . " ms\n            join AccountCharacter ac on ms.memb___id = ac.Id\n            where memb___id = ?\n            order by ConnectTM desc\n        ", array($this->getUsername()));
        return $results;
    }
    public function getNextGuid()
    {
        $next = \Morpheus\Database\Connection::fetchColumn("select max(memb_guid)+1 as next from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_INFO"));
        return $next;
    }
    public function getAvaliableServices()
    {
        if (empty($this->_services)) {
            $avaliables = \Morpheus\Database\Connection::fetchAll("select * \n                from mw_services s\n                where (\n                    s.parent_id is null\n                    or s.allowed = 1\n                    or exists (\n                        select 1 \n                        from mw_viptype_services \n                        where viptype = ? \n                        and s.id = service_id\n                    )\n                )\n                and s.active = 1\n                order by s.sequence\n            ", array($this->getVipType()));
            $services = array();
            foreach ($avaliables as $avaliable) {
                $service = Service::getService($avaliable["service"]);
                $services[$avaliable["service"]] = $service;
            }
            $this->_services = $services;
        }
        return $this->_services;
    }
    public function getRootAvaliableServices()
    {
        $services = array();
        foreach ($this->getAvaliableServices() as $service) {
            if (empty($service["parent_id"])) {
                $services[] = $service;
            }
        }
        return $services;
    }
    public function disconnect()
    {
        $socket = new \Morpheus\Network\Socket();
        $socket->connect(config("joinserver.host"), config("joinserver.port"));
        if (config("server.team") === "xteam") {
            $packet = "C10E30{ACCOUNT(10)}00";
        } else {
            $packet = "C11AA00000000000{ACCOUNT(20)}0000000000000000";
        }
        preg_match("/\\{ACCOUNT\\((.*?)\\)\\}/i", $packet, $size);
        $account = str_pad(asciitohex($this->getUsername()), $size[1] * 2, 0, STR_PAD_RIGHT);
        $packet = hextoascii(preg_replace("/\\{ACCOUNT\\((.*?)\\)\\}/i", $account, $packet));
        $socket->send($packet);
        $stmt = \Morpheus\Database\Connection::prepare("execute WZ_DISCONNECT_MEMB '" . $this->getUsername() . "'");
        $stmt->execute();
    }
    public function __toString()
    {
        return $this->getName();
    }
}

?>