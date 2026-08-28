<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Server;

class Server
{
    public function getCharactersOnline($server = NULL)
    {
        $result = \Morpheus\Database\Connection::fetchAll("select\n            ac.GameIDC as character,\n            ms.IP as ip,\n            ms.memb___id as account,\n            ms.ConnectStat as connect_stat,\n            c.MapNumber as map,\n            c.MapPosX as x,\n            c.MapPosY as y,\n            c.Class as character_class,\n            ms.ServerName as server_name,\n            " . (config("vip.column_type") ? "(select top 1 " . config("vip.column_type") . " from " . config("vip.table") . " where " . config("vip.column_account") . " = c.AccountID) as vipType," : "") . "\n            ms.ConnectTM as connected_at,\n            ms.DisConnectTM as disconnected_at,\n            gm.G_Name as guild\n            from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_STAT") . " ms\n            join AccountCharacter ac ON ms.memb___id = ac.Id collate DATABASE_DEFAULT\n            join Character c ON ms.memb___id = c.AccountID collate DATABASE_DEFAULT\n            left join GuildMember gm ON c.Name = gm.Name collate DATABASE_DEFAULT\n            where ms.ConnectStat > 0\n            and ac.GameIDC = c.Name\n            " . ($server !== null ? "and ms.ServerName = ?" : "") . "\n            order by ConnectTM desc\n        ", array($server));
        return $result;
    }
    public function getMembersTeam()
    {
        $results = \Morpheus\Database\Connection::fetchAll("select\n            c.Name as name,\n            c.Class as class,\n            c.CtlCode as code,\n            case when s.ConnectStat > 0 and ac.GameIDC = c.Name then 1 else 0 end as status\n            from " . \Morpheus\Database\Connection::getTableWithDb("MEMB_STAT") . " s\n            join AccountCharacter ac on s.memb___id = ac.Id collate DATABASE_DEFAULT\n            join Character c on s.memb___id = c.AccountID collate DATABASE_DEFAULT\n            where c.CtlCode > 7\n        ");
        return $results;
    }
    public function hasSkillTree()
    {
        $table = "T_SkillTree_Info";
        if (in_array(config("server.team", "gmo"), array("xteam", "muemu"))) {
            $table = "MasterSkillTree";
        }
        $tree = \Morpheus\Database\Connection::fetchAll("SELECT *\n              FROM INFORMATION_SCHEMA.COLUMNS\n              WHERE TABLE_NAME = ?", array($table));
        return !empty($tree);
    }
    public function hasCastleSiege()
    {
        $siege = \Morpheus\Database\Connection::fetchAll("select *\n              from INFORMATION_SCHEMA.COLUMNS\n              where TABLE_NAME = ?", array("MuCastle_DATA"));
        return !empty($siege);
    }
    public function getSiegeInfo()
    {
        $siege = \Morpheus\Database\Connection::fetchAssoc("select \n            cs.CASTLE_OCCUPY as occupy,\n            cs.SIEGE_START_DATE as start,\n            cs.SIEGE_END_DATE as finish,\n            cs.OWNER_GUILD as guild_owner,\n            g.G_Master as guild_owner_master,\n            g.G_Mark as guild_owner_mark,\n            (select count(1) from GuildMember where G_Name = cs.OWNER_GUILD) as guild_members,\n            c." . config("columns.avatar") . " as avatar \n            from MuCastle_DATA cs\n            left join Guild g on g.G_Name = cs.OWNER_GUILD collate DATABASE_DEFAULT\n            left join Character c on c.Name = g.G_Master collate DATABASE_DEFAULT\n        ");
        return $siege;
    }
    public function getServers($version = 2)
    {
        $socket = new \Morpheus\Network\Socket();
        $socket->connect(config("connectserver.host"), config("connectserver.port"));
        $timer = 0;
        $start = time();
        $servers = array();
        while ($hex = $socket->read(4096)) {
            $temp = time() - $start;
            if ($temp <= $timer) {
                break;
            }
            $hex = strtoupper(bin2hex($temp));
            if ($hex == "C1040001") {
                $socket->send("\\xC1\\x04\\xF4" . ($version == 1 ? "\\x02" : "\\x06"));
            } else {
                if (substr($hex, 0, 2) == "C2") {
                    $rooms = hexdec(substr($hex, 10, $version == 1 ? 2 : 4));
                    for ($count = 0; $count < $rooms; $count++) {
                        $server = substr($hex, $count * 8 + ($version == 1 ? 12 : 14), 8);
                        $low = substr($server, 0, 2);
                        $high = substr($server, 2, 2);
                        $id = hexdec($high . $low);
                        $servers[$count] = array("id" => $id, "low" => $low, "high" => $high, "users" => hexdec(substr($server, 4, 2)));
                    }
                    break;
                }
            }
        }
        $socket->close();
        return $servers;
    }
}

?>