<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/rankings", function () {
    View::display("rankings/index");
});
Route::get("/rankings(/:type(/:page))", function ($type = "level", $page = 1) {
    $max = config("ranking.limit", 99);
    $params = explode(":", $type);
    $type = $params[0];
    array_shift($params);
    $class = Morpheus\Util\Inflector::classify($type);
    $rankings = config("rankings", array());
    if (isset($rankings[$class]) && $rankings[$class]["active"]) {
        try {
            $condition = !empty($params) ? $params[0] : "";
            $key = "ranking_" . str_replace(array("-"), "_", $type);
            if (!empty($params)) {
                $key = $key . "_" . $condition;
            }
            $file = CACHE_PATH . "morpheus---" . $key;
            $filemtime = file_exists($file) ? filemtime($file) : 0;
            $lifetime = config("ranking.cache_time", 60 * 15);
            $cache = Morpheus\Core\Cache::factory("Output", "File", array("lifetime" => 86400 * 30, "automatic_serialization" => true));
            $ranking = Morpheus\Ranking\Ranking::factory($class, array("type" => $condition, "name" => $class));
            $result = $cache->load($key);
            if ($filemtime + $lifetime <= time()) {
                $qb = $ranking->getQuery();
                $qb->setMaxResults($max);
                $formatted = array();
                $counter = 1;
                foreach ($qb->execute() as $d) {
                    $data = $d;
                    $data["rownum"] = isset($d["doctrine_rownum"]) ? $d["doctrine_rownum"] : $counter++;
                    if (isset($data["doctrine_rownum"])) {
                        unset($data["doctrine_rownum"]);
                    }
                    if (isset($result[$data["id"]])) {
                        $data["rank"] = $result[$data["id"]]["rownum"] - $data["rownum"];
                    } else {
                        $data["rank"] = 0;
                    }
                    $formatted[$data["id"]] = $data;
                }
                $result = $formatted;
                $cache->save($result, $key);
                $filemtime = filemtime($file);
            }
            $rankings[$class] += array("id" => $class, "type" => $condition);
            View::display($ranking->getView(), array("type" => $type, "data" => $result, "filemtime" => $filemtime, "lifetime" => $lifetime, "ranking" => $rankings[$class]));
        } catch (RuntimeException $ex) {
            return App::notFound();
        }
    } else {
        return App::notFound();
    }
});

?>