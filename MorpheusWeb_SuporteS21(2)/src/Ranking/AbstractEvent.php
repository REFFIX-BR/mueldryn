<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

abstract class AbstractEvent extends AbstractRanking
{
    protected $_view = "rankings/event";
    protected $_configs = array("table" => array("title" => "Table"), "score_column" => array("title" => "Score column"));
    protected $_key = NULL;
    protected $_table = NULL;
    protected $_score = NULL;
    public function __construct($params = array())
    {
        $key = "rankings." . $this->_key . ".configs";
        $configs = config($key, array());
        $this->_table = config($key . ".table", $this->_table);
        $this->_score = config($key . ".score_column", $this->_score);
        if (isset($params["type"]) && in_array($params["type"], array("daily", "weekly", "monthly")) && isset($configs[$params["type"]])) {
            $this->_score = $configs[$params["type"]];
        }
        $this->_params = $params;
        $columns = array();
        if (config("columns.resets")) {
            $columns[] = "c." . config("columns.resets") . " AS resets";
        }
        if (config("columns.avatar")) {
            $columns[] = "c." . config("columns.avatar") . " AS avatar";
        }
        if (config("vip.column_type")) {
            $columns[] = "(select top 1 " . config("vip.column_type") . " from " . config("vip.table") . " where " . config("vip.column_account") . " = c.AccountID) as vipType";
        }
        foreach (config("register.custom_fields", array()) as $field) {
            $columns[] = "mi." . $field["name"] . " as custom" . util("Inflector")->humanize($field["name"]);
        }
        $character = new \Morpheus\Character\Character();
        $schema = $character->getSchema();
        if (isset($schema["Leadership"])) {
            $columns[] = "c.Leadership AS command";
        }
        $score = ($this->_table === "Character" ? "c." : "bc.") . $this->_score;
        $qb = \Morpheus\Database\Connection::createQueryBuilder();
        $qb->select(array_merge(array("s.ConnectStat AS connectStat", "s.ConnectTM AS lastConnection", "s.ServerName AS server", "ac.GameIDC AS gameIdc", $score . " AS score", "c.Name AS id", "c.Name AS name", "c.cLevel AS level", "c.Experience AS experience", "c.Class AS class", "c.PkLevel AS pkLevel", "c.PkCount AS pkCount", "c.AccountID AS account", "c.LevelUpPoint AS points", "c.MapNumber AS map", "c.MapPosY AS positionY", "c.MapPosX AS positionX", "c.CtlCode AS code", "c.Strength AS strength", "c.Dexterity AS agility", "c.Vitality AS vitality", "c.Energy AS energy", "c.Money AS money", "gm.G_Name AS guild", "CASE WHEN s.ConnectStat > 0 and ac.GameIDC = c.Name THEN 1 ELSE 0 END as status"), $columns));
        if ($this->_table == "Character") {
            $qb->from("Character", "c");
        } else {
            $qb->from($this->_table, "bc")->leftJoin("bc", "Character", "c", "bc.Name = c.Name");
        }
        $qb->leftJoin("c", "AccountCharacter", "ac", "c.AccountID = ac.ID COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_INFO"), "mi", "c.AccountID = mi.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_STAT"), "s", "c.AccountID = s.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", "GuildMember", "gm", "c.Name = gm.Name COLLATE DATABASE_DEFAULT")->setMaxResults(50)->orderBy($score, "DESC")->addOrderBy("c.Name")->where("c.CtlCode = 0");
        $this->_query = $qb;
    }
    public function reset()
    {
        \Morpheus\Database\Connection::executeUpdate("update " . $this->_table . " set " . $this->_score . " = 0");
    }
}

?>