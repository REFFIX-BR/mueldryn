<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

class Gens extends AbstractRanking
{
    protected $_view = "rankings/gens";
    protected $_frequency = array();
    public function __construct(array $params = array())
    {
        parent::__construct($params);
        $params = $this->getParams();
        $columns = array();
        if (config("columns.resets")) {
            $columns[] = "c." . config("columns.resets") . " AS resets";
        }
        if (config("columns.avatar")) {
            $columns[] = "c." . config("columns.avatar") . " AS avatar";
        }
        if (config("vip.column_type")) {
            $columns[] = "(select " . config("vip.column_type") . " from " . config("vip.table") . " where " . config("vip.column_account") . " = c.AccountID) as vipType";
        }
        foreach (config("register.custom_fields", array()) as $field) {
            $columns[] = "mi." . $field["name"] . " as custom" . util("Inflector")->humanize($field["name"]);
        }
        $family = 1;
        if (isset($params["type"]) && in_array($params["type"], array("duprian", "vanert"))) {
            switch ($params["type"]) {
                case "duprian":
                    $family = 1;
                    break;
                case "vanert":
                    $family = 2;
                    break;
            }
        }
        $rank = \Morpheus\Database\Connection::fetchAll("SELECT *\n              FROM INFORMATION_SCHEMA.COLUMNS\n              WHERE TABLE_NAME = ?", array("Gens_Rank"));
        $character = new \Morpheus\Character\Character();
        $schema = $character->getSchema();
        if (isset($schema["Leadership"])) {
            $columns[] = "c.Leadership AS command";
        }
        $qb = \Morpheus\Database\Connection::createQueryBuilder();
        if (!empty($rank)) {
            $qb->select(array_merge(array("g.Name AS id", "g.Name AS name", "g.Family AS family", "g.Contribution AS contribution", "gm.G_Name AS guild", "c.cLevel AS subscore", "c.Experience AS experience", "c.Class AS class", "c.PkLevel AS pkLevel", "c.PkCount AS pkCount", "c.AccountID AS account", "c.LevelUpPoint AS points", "c.MapNumber AS map", "c.MapPosY AS positionY", "c.MapPosX AS positionX", "c.CtlCode AS code", "c.Strength AS strength", "c.Dexterity AS agility", "c.Vitality AS vitality", "c.Energy AS energy", "c.Money AS money", "CASE WHEN s.ConnectStat > 0 and ac.GameIDC = c.Name THEN 1 ELSE 0 END AS status"), $columns))->from("Gens_Rank", "g")->leftJoin("g", "Character", "c", "c.Name = g.Name COLLATE DATABASE_DEFAULT")->leftJoin("c", "AccountCharacter", "ac", "c.AccountID = ac.ID COLLATE DATABASE_DEFAULT")->leftJoin("c", "MEMB_INFO", "mi", "c.AccountID = mi.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", "MEMB_STAT", "s", "c.AccountID = s.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", "GuildMember", "gm", "c.Name = gm.Name COLLATE DATABASE_DEFAULT")->where("g.Family = " . $family . " AND c.CtlCode = 0")->orderBy("g.Contribution", "DESC")->addOrderBy("g.Name", "ASC");
        } else {
            $qb->select(array_merge(array("g.CHAR_NAME AS id", "g.CHAR_NAME AS name", "g.FAMILY AS family", "g.CONTRIBUITION AS contribution", "g.GRADUATION AS graduation", "gm.G_Name AS guild", "c.cLevel AS subscore", "c.Experience AS experience", "c.Class AS class", "c.PkLevel AS pkLevel", "c.PkCount AS pkCount", "c.AccountID AS account", "c.LevelUpPoint AS points", "c.MapNumber AS map", "c.MapPosY AS positionY", "c.MapPosX AS positionX", "c.CtlCode AS code", "c.Strength AS strength", "c.Dexterity AS agility", "c.Vitality AS vitality", "c.Energy AS energy", "c.Money AS money", "CASE WHEN s.ConnectStat > 0 and ac.GameIDC = c.Name THEN 1 ELSE 0 END AS status"), $columns))->from("T_GensSystem", "g")->leftJoin("g", "Character", "c", "c.Name = g.CHAR_NAME COLLATE DATABASE_DEFAULT")->leftJoin("c", "AccountCharacter", "ac", "c.AccountID = ac.ID COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_INFO"), "mi", "c.AccountID = mi.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_STAT"), "s", "c.AccountID = s.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", "GuildMember", "gm", "c.Name = gm.Name COLLATE DATABASE_DEFAULT")->where("g.FAMILY = " . $family)->orderBy("g.CONTRIBUITION", "DESC")->addOrderBy("g.CHAR_NAME", "ASC");
        }
        $this->_query = $qb;
    }
}

?>