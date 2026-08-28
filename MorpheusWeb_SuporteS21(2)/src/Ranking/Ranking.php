<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

class Ranking
{
    private static $_rankings = array("BloodCastle", "ChaosCastle", "Character", "DevilSquare", "Duel", "Gens", "Guild", "Hero", "IllusionTemple", "MasterLevel", "MasterResets", "Pk", "Resets");
    public static function availables()
    {
        return static::$_rankings;
    }
    public static function register($name)
    {
        static::$_rankings[] = $name;
    }
    public static function factory($ranking, $params = array())
    {
        $class = "\\Morpheus\\Ranking\\" . $ranking;
        if (class_exists($class)) {
            $factory = new $class($params);
            notify("rankings." . $ranking, $factory);
            return $factory;
        }
        $customs = config("custom.rankings", array());
        if (in_array($ranking, $customs)) {
            $factory = new Custom($params);
            notify("rankings.Custom", $factory);
            return $factory;
        }
        $plugin = new \Morpheus\Core\Plugin();
        foreach ($plugin->getPlugins() as $plugin) {
            $class = "\\" . $plugin . "\\Ranking\\" . $ranking;
            if (class_exists($class)) {
                $factory = new $class($params);
                notify("rankings." . $ranking, $factory);
                return $factory;
            }
        }
        throw new \RuntimeException("Ranking Class \"" . $class . "\" not found");
    }
}

?>