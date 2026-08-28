<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::hook(array("home", "top-players"), function () {
    $rankings = array();
    $max = config("hallfame.max_results", 1);
    foreach (config("hallfame.rankings", array()) as $id => $config) {
        if (!isset($config["active"]) || $config["active"]) {
            $rankings[$id] = array("name" => $config["name"], "fullname" => $config["name"], "data" => Morpheus\Ranking\Ranking::factory($id, array("name" => $id))->getQuery()->setMaxResults($max)->execute()->fetchAll());
        }
        if ($config["daily"]) {
            $rankings[$id . "Daily"] = array("name" => $config["name"], "subname" => __("Daily"), "fullname" => $config["name"] . " " . __("Daily"), "data" => Morpheus\Ranking\Ranking::factory($id, array("type" => "daily", "name" => $id))->getQuery()->setMaxResults($max)->execute()->fetchAll());
        }
        if ($config["weekly"]) {
            $rankings[$id . "Weekly"] = array("name" => $config["name"], "subname" => __("Weekly"), "fullname" => $config["name"] . " " . __("Weekly"), "data" => Morpheus\Ranking\Ranking::factory($id, array("type" => "weekly", "name" => $id))->getQuery()->setMaxResults($max)->execute()->fetchAll());
        }
        if ($config["monthly"]) {
            $rankings[$id . "Monthly"] = array("name" => $config["name"], "subname" => __("Monthly"), "fullname" => $config["name"] . " " . __("Monthly"), "data" => Morpheus\Ranking\Ranking::factory($id, array("type" => "monthly", "name" => $id))->getQuery()->setMaxResults($max)->execute()->fetchAll());
        }
    }
    View::display("HallFame.home", array("layout" => false, "rankings" => $rankings, "max" => $max));
}, 30);

?>