<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

class Resets extends AbstractRanking
{
    protected $_key = "resets";
    private $_score = NULL;
    public function __construct(array $params = array())
    {
        parent::__construct($params);
        $key = \Morpheus\Util\Inflector::classify($this->_key);
        $configs = config("rankings." . $key . ".configs", array());
        $this->_score = config("columns." . $this->_key);
        if (!$this->_score) {
            if ($this->_key === "resets") {
                $this->_score = "ResetCount";
            } elseif ($this->_key === "master_resets") {
                $this->_score = "MasterResetCount";
            }
        }
        if (isset($params["type"]) && in_array($params["type"], array("daily", "weekly", "monthly")) && isset($configs[$params["type"]])) {
            $this->_score = $configs[$params["type"]];
        }
        if (!$this->_score) {
            $this->_score = "cLevel";
        }
        $columns = array();
        if (config("columns.avatar")) {
            $columns[] = "c." . config("columns.avatar") . " AS avatar";
        }
        if (config("vip.column_type")) {
            $columns[] = "(select top 1 " . config("vip.column_type") . " from " . config("vip.table") . " where " . config("vip.column_account") . " = c.AccountID) as vipType";
        }
        foreach (config("register.custom_fields", array()) as $field) {
            $columns[] = "mi." . $field["name"] . " as custom_" . strtolower($field["name"]);
        }
        $character = new \Morpheus\Character\Character();
        $schema = $character->getSchema();
        if (isset($schema["Leadership"])) {
            $columns[] = "c.Leadership AS command";
        }
        $qb = \Morpheus\Database\Connection::createQueryBuilder();
        $qb->select(array_merge(array("s.ConnectStat AS connectStat", "s.ConnectTM AS lastConnection", "s.ServerName AS server", "ac.GameIDC AS gameIdc", "c.Name AS id", "c.Name AS name", "c.cLevel AS level", "c." . $this->_score . " AS score", "c.cLevel AS subscore", "c.Experience AS experience", "c.Class AS class", "c.PkLevel AS pkLevel", "c.PkCount AS pkCount", "c.AccountID AS account", "c.LevelUpPoint AS points", "c.MapNumber AS map", "c.MapPosY AS positionY", "c.MapPosX AS positionX", "c.CtlCode AS code", "c.Strength AS strength", "c.Dexterity AS agility", "c.Vitality AS vitality", "c.Energy AS energy", "c.Money AS money", "gm.G_Name AS guild", "CASE WHEN s.ConnectStat > 0 and ac.GameIDC = c.Name THEN 1 ELSE 0 END as status"), $columns))->from("Character", "c")->leftJoin("c", "AccountCharacter", "ac", "c.AccountID = ac.ID COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_INFO"), "mi", "c.AccountID = mi.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_STAT"), "s", "c.AccountID = s.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", "GuildMember", "gm", "c.Name = gm.Name COLLATE DATABASE_DEFAULT")->setMaxResults(50)->where("c.CtlCode = 0");
        $qb->orderBy("c." . $this->_score, "DESC")->addOrderBy("c.cLevel", "DESC")->addOrderBy("c.Name");
        $this->_query = $qb;
    }
    public function isValid()
    {
        return config("columns.resets", false);
    }
    public function reset()
    {
        \Morpheus\Database\Connection::executeUpdate("update Character set " . $this->_score . " = 0");
    }
}

?>