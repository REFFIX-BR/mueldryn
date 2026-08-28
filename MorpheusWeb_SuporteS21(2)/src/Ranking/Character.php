<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

class Character extends AbstractRanking
{
    protected $_frequency = array();
    public function __construct(array $params = array())
    {
        parent::__construct($params);
        $columns = array();
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
        $qb = \Morpheus\Database\DriverManager::getConnection()->createQueryBuilder();
        $qb->select(array_merge(array("s.ConnectStat AS connectStat", "s.ConnectTM AS lastConnection", "s.ServerName AS server", "ac.GameIDC AS gameIdc", "c.Name AS id", "c.Name AS name", "c.cLevel AS level", "c.cLevel AS score", "c.Experience AS experience", "c.Experience AS subscore", "c.Class AS class", "c.PkLevel AS pkLevel", "c.PkCount AS pkCount", "c.AccountID AS account", "c.LevelUpPoint AS points", "c.MapNumber AS map", "c.MapPosY AS positionY", "c.MapPosX AS positionX", "c.CtlCode AS code", "c.Strength AS strength", "c.Dexterity AS agility", "c.Vitality AS vitality", "c.Energy AS energy", "c.Money AS money", "gm.G_Name AS guild", "CASE WHEN s.ConnectStat > 0 and ac.GameIDC = c.Name THEN 1 ELSE 0 END as status"), $columns))->from("Character", "c")->leftJoin("c", "AccountCharacter", "ac", "c.AccountID = ac.ID COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_INFO"), "mi", "c.AccountID = mi.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", table_with_db("MEMB_STAT"), "s", "c.AccountID = s.memb___id COLLATE DATABASE_DEFAULT")->leftJoin("c", "GuildMember", "gm", "c.Name = gm.Name COLLATE DATABASE_DEFAULT")->setMaxResults(50)->where("c.CtlCode = 0");
        $qb->orderBy("c.cLevel", "DESC")->addOrderBy("c.Experience", "DESC")->addOrderBy("c.Name");
        $this->_query = $qb;
    }
    public function reset()
    {
        Connection::executeUpdate("update Character set cLevel = 0 and Experience = 0");
    }
}

?>