<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace DonateRanking\Ranking;

class Donate extends \Morpheus\Ranking\AbstractRanking
{
    protected $_frequency = array();
    public function getQuery()
    {
        $resets = config("columns.resets");
        $qb = \Morpheus\Database\Connection::createQueryBuilder();
        $qb->select(array("o.username as id", "'R\$' + replace(sum(o.value), '.', ',') as score", "sum(o.value) as abc", "(select top 1 Avatar from Character where AccountID = o.username order by " . $resets . " desc) as avatar", "(select top 1 Name from Character where AccountID = o.username order by " . $resets . " desc) as name", "(select top 1 Class from Character where AccountID = o.username order by " . $resets . " desc) as class", "(select top 1 gm.G_Name from Character c join GuildMember gm on c.Name = gm.Name where c.AccountID = o.username order by " . $resets . " desc) as guild", "0 as status"))->from("mw_orders", "o")->groupBy(array("o.username"))->where("o.status in (3, 4)")->orderBy("3", "DESC");
        return $qb;
    }
}

?>