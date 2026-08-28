<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Team
{
    private static $_availables = array("xteam" => array("name" => "X-Team", "class" => "XTeam"), "muemu" => array("name" => "MuEmu", "class" => "MuEmu"), "gx" => array("name" => "GXGaming", "class" => "GX"), "igcn" => array("name" => "IGCN", "class" => "IGCN"), "gmo" => array("name" => "GMO", "class" => "GMO"));
    public static function classify()
    {
        $team = static::$_availables[config("server.team", "gmo")];
        return $team["class"];
    }
    public static function is($teams)
    {
        $team = config("server.team", "gmo");
        if (!is_array($teams)) {
            $teams = array($teams);
        }
        return in_array($team, $teams);
    }
}

?>