<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character;

class Character
{
    private $_name = NULL;
    private $_username = NULL;
    private $_klass = NULL;
    private $_level = NULL;
    private $_experience = NULL;
    private $_points = NULL;
    private $_strength = NULL;
    private $_agility = NULL;
    private $_energy = NULL;
    private $_vitality = NULL;
    private $_command = NULL;
    private $_code = NULL;
    private $_pkLevel = NULL;
    private $_money = NULL;
    private $_map = NULL;
    private $_positionX = NULL;
    private $_positionY = NULL;
    private $_life = NULL;
    private $_maxLife = NULL;
    private $_mana = NULL;
    private $_maxMana = NULL;
    private $_avatar = NULL;
    private $_guild = NULL;
    private $_connected = NULL;
    private $_resets = NULL;
    private $_masterResets = NULL;
    private $_account = NULL;
    private $_inventory = NULL;
    private $_binInventory = NULL;
    private $_extInventory = NULL;
    private $_magicList = NULL;
    private $_ruud = NULL;
    private $_data = array();
    private $_masterSkill = NULL;
    private $_schema = array();
    public function __construct($name = NULL, $account = NULL)
    {
        if ($name !== null) {
            $this->read($name, $account);
        }
        $class = "\\Morpheus\\Character\\MasterSkill\\" . \Morpheus\Util\Team::classify();
        if (class_exists($class)) {
            $this->_masterSkill = new $class($this);
        }
    }
    public function setName($name)
    {
        $this->_name = $name;
        return $this;
    }
    public function getSchema()
    {
        if (!empty($this->_schema)) {
            return $this->_schema;
        }
        $columns = \Morpheus\Database\Connection::fetchAll("select *\n            from INFORMATION_SCHEMA.COLUMNS\n            where TABLE_NAME = ?\n        ", array("Character"));
        foreach ($columns as $column) {
            $this->_schema[$column["COLUMN_NAME"]] = $column;
        }
        return $this->_schema;
    }
    public function getDefaultProperties()
    {
        for ($initial = $this->getClass(); $initial % 16 != 0; $initial--) {
        }
        $defaults = \Morpheus\Database\Connection::fetchAssoc("SELECT Vitality\n            , Strength\n            , Dexterity\n            , Energy\n            " . (isset($this->getSchema()["Leadership"]) ? ", Leadership" : "") . "\n            , MapNumber\n            , MapPosX\n            , MapPosY\n            , Life\n            , MaxLife\n            , Mana\n            , MaxMana\n            , Class\n            FROM DefaultClassType\n            WHERE Class = ?", array($initial));
        return $defaults;
    }
    public function getMasterSkill()
    {
        return $this->_masterSkill;
    }
    public function getName()
    {
        return $this->_name;
    }
    public function setUsername($username)
    {
        $this->_username = $username;
        return $this;
    }
    public function getUsername()
    {
        return $this->_username;
    }
    public function setClass($class)
    {
        $this->_klass = $class;
        return $this;
    }
    public function getClass()
    {
        return $this->_klass;
    }
    public function setLevel($level)
    {
        $this->_level = $level;
        return $this;
    }
    public function getLevel()
    {
        return $this->_level;
    }
    public function setExperience($experience)
    {
        $this->_experience = $experience;
        return $this;
    }
    public function getExperience()
    {
        return $this->_experience;
    }
    public function setPoints($points)
    {
        $this->_points = $points;
        return $this;
    }
    public function getPoints()
    {
        return $this->_points;
    }
    public function setStrength($strength)
    {
        $this->_strength = $strength;
        return $this;
    }
    public function getStrength()
    {
        return $this->_strength;
    }
    public function setAgility($agility)
    {
        $this->_agility = $agility;
        return $this;
    }
    public function getAgility()
    {
        return $this->_agility;
    }
    public function setEnergy($energy)
    {
        $this->_energy = $energy;
        return $this;
    }
    public function getEnergy()
    {
        return $this->_energy;
    }
    public function setVitality($vitality)
    {
        $this->_vitality = $vitality;
        return $this;
    }
    public function getVitality()
    {
        return $this->_vitality;
    }
    public function setCommand($command)
    {
        $this->_command = $command;
        return $this;
    }
    public function getCommand()
    {
        return $this->_command;
    }
    public function setCode($code)
    {
        $this->_code = $code;
        return $this;
    }
    public function getCode()
    {
        return $this->_code;
    }
    public function setPkLevel($level)
    {
        $this->_pkLevel = $level;
        return $this;
    }
    public function getPkLevel()
    {
        return $this->_pkLevel;
    }
    public function isPk()
    {
        return 3 < $this->_pkLevel;
    }
    public function isHero()
    {
        return $this->_pkLevel < 3;
    }
    public function setMoney($money)
    {
        $this->_money = $money;
        return $this;
    }
    public function getMoney()
    {
        return $this->_money;
    }
    public function addMoney($money)
    {
        $this->_money += $money;
        return $this;
    }
    public function setRuud($ruud)
    {
        $this->_ruud = $ruud;
        return $this;
    }
    public function getRuud()
    {
        return $this->_ruud;
    }
    public function addRuud($ruud)
    {
        $this->_ruud += $ruud;
        return $this;
    }
    public function setMap($map)
    {
        $this->_map = $map;
        return $this;
    }
    public function getMap()
    {
        return $this->_map;
    }
    public function setPositionX($x)
    {
        $this->_positionX = $x;
        return $this;
    }
    public function getPositionX()
    {
        return $this->_positionX;
    }
    public function setPositionY($y)
    {
        $this->_positionY = $y;
        return $this;
    }
    public function getPositionY()
    {
        return $this->_positionY;
    }
    public function setLife($life)
    {
        $this->_life = $life;
        return $this;
    }
    public function getLife()
    {
        return $this->_life;
    }
    public function setMaxLife($maxLife)
    {
        $this->_maxLife = $maxLife;
        return $this;
    }
    public function getMaxLife()
    {
        return $this->_maxLife;
    }
    public function setMana($mana)
    {
        $this->_mana = $mana;
        return $this;
    }
    public function getMana()
    {
        return $this->_mana;
    }
    public function setMaxMana($maxMana)
    {
        $this->_maxMana = $maxMana;
        return $this;
    }
    public function getMaxMana()
    {
        return $this->_maxMana;
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
    public function setAvatar($avatar)
    {
        $this->_avatar = $avatar;
        return $this;
    }
    public function getAvatar()
    {
        return $this->_avatar;
    }
    public function getAvatarUrl($default)
    {
        if ($this->getAvatar() && file_exists(ROOT . DS . "uploads" . DS . "avatar" . DS . $this->getAvatar())) {
            return upload_to("/avatar/" . $this->getAvatar());
        }
        return $default;
    }
    public function setGuild($guild)
    {
        if ($guild instanceof \Morpheus\Guild\Guild) {
            $this->_guild = $guild;
        } else {
            $this->_guild = new \Morpheus\Guild\Guild($guild);
        }
        return $this;
    }
    public function getGuild()
    {
        if ($this->_guild === null && isset($this->_data["_guild"])) {
            $this->_guild = new \Morpheus\Guild\Guild($this->_data["_guild"]);
        }
        return $this->_guild;
    }
    public function inGuild()
    {
        return $this->getGuild() !== null;
    }
    public function setAccount($account)
    {
        if ($account instanceof \Morpheus\Account\Account) {
            $this->_account = $account;
        } else {
            $this->_account = new \Morpheus\Account\Account($account);
        }
        return $this;
    }
    public function getAccount()
    {
        if ($this->_account === null && !empty($this->_data["_account"])) {
            $this->_account = new \Morpheus\Account\Account($this->_data["_account"]);
        }
        if ($this->_account === null && $this->getUsername()) {
            $this->_account = new \Morpheus\Account\Account($this->getUsername());
        }
        return $this->_account;
    }
    public function setResets($resets)
    {
        $this->_resets = $resets;
        return $this;
    }
    public function getResets()
    {
        return $this->_resets;
    }
    public function addResets($resets)
    {
        $this->_resets += $resets;
        return $this;
    }
    public function setMasterResets($masterResets)
    {
        $this->_masterResets = $masterResets;
        return $this;
    }
    public function getMasterResets()
    {
        return $this->_masterResets;
    }
    public function addMasterResets($masterResets)
    {
        $this->_masterResets += $masterResets;
        return $this;
    }
    public function setBinInventory($inventory)
    {
        $this->_binInventory = $inventory;
        return $this;
    }
    public function getBinInventory()
    {
        return $this->_binInventory;
    }
    public function setMagicList($magicList)
    {
        $this->_magicList = $magicList;
        return $this;
    }
    public function getMagicList()
    {
        return $this->_magicList;
    }
    public function exists()
    {
        return $this->getName() !== null;
    }
    public function setExtInventory($ex)
    {
        $this->_extInventory = $ex;
        return $this;
    }
    public function getExtInventory()
    {
        return $this->_extInventory;
    }
    public function getColumnGameIDC()
    {
        $slot = null;
        $gameidc = \Morpheus\Database\Connection::fetchAssoc("SELECT GameID1, GameID2, GameID3, GameID4, GameID5 FROM AccountCharacter WHERE Id = ?", array($this->_data["username"]));
        foreach (array("GameID1", "GameID2", "GameID3", "GameID4", "GameID5") as $column) {
            if ($gameidc[$column] === $this->getName()) {
                $slot = $column;
                break;
            }
        }
        return $slot;
    }
    public function count($where = NULL)
    {
        if ($where === "onlines") {
            $total = \Morpheus\Database\Connection::fetchColumn("select\n                count(1) as total from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_STAT") . "\n                where ConnectStat > 0\n            ");
            return $total;
        }
        if ($where === "onlines:record") {
            $record = \Morpheus\Database\Connection::fetchColumn("select max(record) \n                from mw_records \n                where type = ?\n            ", array("online"));
            $total = $this->count("onlines");
            if ($record < $total) {
                \Morpheus\Database\Connection::insert("mw_records", array("record" => $total, "type" => "online", "record_date" => (new \DateTime())->format(DATETIME_ISO_FORMAT)));
                return $total;
            }
            return $record;
        }
        switch ($where) {
            case "banneds":
                $total = \Morpheus\Database\Connection::fetchColumn("select\n                        count(1) AS total \n                        from Character \n                        where CtlCode = 1\n                    ");
                break;
            default:
                $total = \Morpheus\Database\Connection::fetchColumn("select\n                        count(1) as total \n                        from Character\n                    ");
        }
        return $total;
    }
    public function rename($name)
    {
        \Morpheus\Database\Connection::transactional(function ($conn) use($name) {
            $column = $this->getColumnGameIDC();
            $conn->update("AccountCharacter", array($column => $name), array($column => $this->getName()));
            $conn->update("Character", array("Name" => $name), array("Name" => $this->getName()));
            $this->getMasterSkill()->changeTo($name);
        });
    }
    public function update()
    {
        if ($this->exists()) {
            \Morpheus\Database\Connection::transactional(function ($conn) {
                $columns = array("cLevel" => $this->getLevel(), "Experience" => $this->getExperience(), "MapNumber" => $this->getMap(), "MapPosY" => $this->getPositionY(), "MapPosX" => $this->getPositionX(), "CtlCode" => $this->getCode(), "PkLevel" => $this->getPkLevel(), "LevelUpPoint" => $this->getPoints(), "Strength" => $this->getStrength(), "Dexterity" => $this->getAgility(), "Vitality" => $this->getVitality(), "Energy" => $this->getEnergy(), "Money" => $this->getMoney(), "Life" => $this->getLife(), "MaxLife" => $this->getMaxLife(), "Mana" => $this->getMana(), "MaxMana" => $this->getMaxMana());
                $types = array("integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer");
                if (config("columns.avatar")) {
                    $columns[config("columns.avatar")] = $this->getAvatar();
                    $types[] = "string";
                }
                if (config("columns.resets")) {
                    $columns[config("columns.resets")] = $this->getResets();
                    $types[] = "integer";
                }
                if (config("columns.master_resets")) {
                    $columns[config("columns.master_resets")] = $this->getMasterResets();
                    $types[] = "integer";
                }
                if (isset($this->getSchema()["Leadership"])) {
                    $columns["Leadership"] = $this->getCommand();
                    $types[] = "integer";
                }
                if (isset($this->getSchema()["Ruud"])) {
                    $columns["Ruud"] = $this->getRuud();
                    $types[] = "integer";
                }
                $conn->update("Character", $columns, array("Name" => $this->getName()), $types);
            });
        }
    }
    public function toArray()
    {
        return $this->_data;
    }
    public function read($name, $account = NULL)
    {
        $where = "";
        if ($account !== null) {
            $where = " AND c.AccountID = :account";
        }
        $resets = "";
        if (config("columns.resets")) {
            $resets .= "c." . config("columns.resets") . " AS resets,";
        }
        if (config("columns.master_resets")) {
            $resets .= "c." . config("columns.master_resets") . " AS masterResets,";
        }
        $result = \Morpheus\Database\Connection::fetchAssoc("SELECT\n            s.ConnectStat AS connectStat,\n            s.ConnectTM AS lastConnection,\n            s.ServerName AS server,\n            ac.GameIDC AS gameIdc,\n            c.Name AS name,\n            c.AccountID AS _account,\n            c.cLevel AS level,\n            c.Experience AS experience,\n            CONVERT(VARBINARY(MAX), c.Inventory) AS binInventory,\n            CONVERT(VARBINARY(MAX), c.MagicList) AS magicList,\n            c.Class AS class,\n            c.PkLevel AS pkLevel,\n            c.PkCount AS pkCount,\n            c.AccountID AS username,\n            c.LevelUpPoint AS points,\n            c.MapNumber AS map,\n            c.MapPosY AS positionY,\n            c.MapPosX AS positionX,\n            c.CtlCode AS code,\n            c.Strength AS strength,\n            c.Dexterity AS agility,\n            c.Vitality AS vitality,\n            c.Energy AS energy,\n            " . (isset($this->getSchema()["c.ExtInventory"]) ? "c.ExtInventory AS extInventory" : "") . "\n            " . (isset($this->getSchema()["Leadership"]) ? "c.Leadership AS command," : "") . "\n            c.Money AS money,\n            " . (isset($this->getSchema()["Ruud"]) ? "c.Ruud AS ruud," : "") . "\n            c.Life AS life,\n            c.MaxLife AS maxLife,\n            c.Mana AS mana,\n            c.MaxMana AS maxMana,\n            " . $resets . "\n            gm.G_Name AS _guild,\n            " . (config("columns.avatar") ? "c." . config("columns.avatar") . " AS avatar," : "") . "\n            CASE WHEN s.ConnectStat > 0 and ac.GameIDC = c.Name THEN 1 ELSE 0 END as connected\n            from Character c\n            left join AccountCharacter ac on c.AccountID = ac.ID COLLATE DATABASE_DEFAULT\n            left join " . \Morpheus\Database\Connection::getTableWithDb("MEMB_STAT") . " s on c.AccountID = s.memb___id COLLATE DATABASE_DEFAULT\n            left join GuildMember gm on c.Name = gm.Name COLLATE DATABASE_DEFAULT\n            where c.Name = :name\n        " . $where, array("name" => $name, "account" => $account));
        if (!empty($result)) {
            $this->_data = $result;
            attach($this, $result);
        }
    }
    public function getInventory()
    {
        if ($this->exists() && $this->_inventory === null) {
            $this->_inventory = new Inventory($this, 8, 8);
        }
        return $this->_inventory;
    }
    public function move($map, $x, $y)
    {
        $this->setMap($map);
        $this->setPositionX($x);
        $this->setPositionY($y);
        $this->update();
    }
    public function changeClass($class)
    {
        \Morpheus\Database\Connection::transactional(function ($conn) use($class) {
            $conn->update("Character", array("Class" => $class), array("Name" => $this->getName()), array("integer"));
            $this->clearSkills();
            $this->clearQuests();
        });
    }
    public function clearSkills()
    {
        \Morpheus\Database\Connection::executeUpdate("update Character set\n            MagicList = CAST(REPLICATE(CHAR(0xff), " . $this->getSchema()["MagicList"]["CHARACTER_MAXIMUM_LENGTH"] . ") as varbinary(" . $this->getSchema()["MagicList"]["CHARACTER_MAXIMUM_LENGTH"] . "))\n            where Name = ?\n        ", array($this->getName()));
        $this->getMasterSkill()->reset();
    }
    public function clearQuests()
    {
        \Morpheus\Database\Connection::executeUpdate("update Character set\n            Quest = CAST(REPLICATE(CHAR(0xff), " . $this->getSchema()["Quest"]["CHARACTER_MAXIMUM_LENGTH"] . ") as varbinary(" . $this->getSchema()["Quest"]["CHARACTER_MAXIMUM_LENGTH"] . "))\n            where Name = ?\n        ", array($this->getName()));
    }
}

?>