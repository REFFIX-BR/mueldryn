<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

class Guild extends AbstractRanking
{
    protected $_view = "rankings/guild";
    private $_score = "G_Score";
    public function __construct($params = array())
    {
        parent::__construct($params);
        $key = "rankings.Guild.configs";
        $configs = config($key, array());
        if (isset($params["type"]) && in_array($params["type"], array("daily", "weekly", "monthly")) && isset($configs[$params["type"]])) {
            $this->_score = $configs[$params["type"]];
        }
        $qb = \Morpheus\Database\DriverManager::getConnection()->createQueryBuilder();
        $qb->select("G_Name as name", "G_Name as id", "G_Mark as mark", $this->_score . " as score", "(select count(1) from GuildMember where G_Name = g.G_Name) as members", "G_Master as master")->from("Guild", "g")->orderBy($this->_score, "DESC")->setMaxResults(50);
        $this->_query = $qb;
    }
    public function reset()
    {
        Connection::executeUpdate("update Guild set " . $this->_score . " = 0");
    }
}

?>