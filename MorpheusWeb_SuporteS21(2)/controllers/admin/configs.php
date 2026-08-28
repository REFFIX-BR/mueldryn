<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/configs/template", function () {
    $templates = array();
    foreach (new DirectoryIterator(TEMPLATE_PATH) as $file) {
        if ($file->isDir() && !in_array($file->getFilename(), array(".", "..", "admin", "mail"))) {
            $temp = array();
            if (file_exists($file->getPathname() . DS . "info.php")) {
                $temp = (require $file->getPathname() . DS . "info.php");
            } else {
                $temp = array("name" => $file->getFilename(), "author" => "Desconhecido", "description" => "", "website" => "", "version" => "", "screen" => "");
            }
            $temp["active"] = $temp["name"] == config("template");
            $templates[] = $temp;
        }
    }
    View::display("templates/index", array("templates" => Morpheus\Util\Hash::sort($templates, "{n}.active", "desc")));
})->name("configs.template");
Route::get("/configs/template/active/:name", function ($template) {
    try {
        Morpheus\Core\Config::save("template", $template);
        return success(__("Template %s has been activate", array($template)), array("redirect" => "/admin/configs/template"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/configs/template"));
    }
})->name("configs.template.active");
Route::get("/configs/template/edit/:name", function ($template) {
    View::display("templates/edit");
})->name("configs.template.edit");
Route::map("/configs/general", function () {
    if (Request::isPost()) {
        try {
            $data = array();
            $items = Input::file("itemkor");
            if (!empty($items)) {
                $dir = ROOT . DS . "resources" . DS . "files" . DS;
                list($file) = rework_upload_files($items);
                $storage = Morpheus\Item\Storage\Storage::factory("ItemKOR", $file["tmp_name"]);
                $fs = new Symfony\Component\Filesystem\Filesystem();
                if ($fs->exists($dir . "items.mw")) {
                    $fs->remove($dir . "items.mw");
                }
                file_put_contents($dir . "items.mw", serialize($storage->getItems()));
            }
            $servers = array();
            foreach (Input::post("servers") as $index => $server) {
                if (empty($server["server"]) || empty($server["name"]) || empty($server["max"])) {
                    continue;
                }
                $servers[$server["server"]] = array("server" => $server["server"], "name" => $server["name"], "ip" => $server["ip"], "port" => $server["port"], "max" => $server["max"]);
            }
            if (server()->hasCastleSiege()) {
                $data["castlesiege.next_confrontation"] = Input::post("cs_next_confrontation");
            }
            Morpheus\Core\Config::save(array("site.title" => Input::post("site_title"), "server.team" => Input::post("server_team"), "server.name" => Input::post("server_name"), "server.version_name" => (($versionName = trim((string) Input::post("server_version_name"))) !== "" ? $versionName : config("server.version_name", "Season 20")), "server.experience" => Input::post("server_experience"), "server.drop" => Input::post("server_drop"), "server.max_level" => Input::post("server_max_level"), "server.max_status" => Input::post("server_max_status"), "smtp.host" => Input::post("smtp_host"), "smtp.username" => Input::post("smtp_username"), "smtp.password" => Input::post("smtp_password"), "smtp.port" => Input::post("smtp_port"), "smtp.from" => Input::post("smtp_from"), "smtp.secure" => Input::post("smtp_secure") == 1, "connectserver.host" => Input::post("connect_host"), "connectserver.port" => Input::post("connect_port"), "joinserver.host" => Input::post("join_host"), "joinserver.port" => Input::post("join_port"), "debug" => Input::post("debug") == 1, "url_rewrite" => Input::post("url_rewrite") == 1, "maintenance" => Input::post("maintenance") == 1, "locale" => Input::post("language"), "item.db_version" => Input::post("item_db_version"), "warehouse.extended" => Input::post("warehouse_extended") == 1, "servers" => $servers, "warehouse.ext.active" => Input::post("ext_active") == 1, "warehouse.ext.table" => Input::post("ext_table"), "warehouse.ext.column_account" => Input::post("ext_column_account"), "warehouse.ext.column_items" => Input::post("ext_column_items"), "warehouse.ext.column_number" => Input::post("ext_column_number"), "warehouse.ext.column_money" => Input::post("ext_column_money")) + $data);
            return success(__("Settings has been updated"));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    $teams = array("xteam" => "X-Team", "muemu" => "MuEmu/LouisEmulator", "gx" => "GXGaming", "igcn" => "IGCN", "gmo" => "GMO");
    View::display("configs/general", array("teams" => $teams));
})->via("GET", "POST")->name("configs.general");
Route::map("/configs/vip", function () {
    $vip = config("vip");
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("table" => array(Morpheus\Core\Validation::REQUIRED), "column_account" => array(Morpheus\Core\Validation::REQUIRED), "column_type" => array(Morpheus\Core\Validation::REQUIRED), "column_expire" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            $types = array();
            foreach (Input::post("types") as $type) {
                if (!empty($type["code"]) || !empty($type["name"])) {
                    $types[$type["code"]] = $type["name"];
                }
            }
            $vip = array("table" => Input::post("table"), "column_account" => Input::post("column_account"), "column_type" => Input::post("column_type"), "column_expire" => Input::post("column_expire"), "types" => $types);
            try {
                $test = Connection::fetchColumn("SELECT " . $vip["column_type"] . ", " . $vip["column_expire"] . " FROM " . $vip["table"] . " WHERE " . $vip["column_account"] . " = ''");
                Morpheus\Core\Config::save(array("vip.table" => $vip["table"], "vip.column_account" => $vip["column_account"], "vip.column_type" => $vip["column_type"], "vip.column_expire" => $vip["column_expire"], "vip.types" => $vip["types"]));
                return success(__("Vip settings has been updated"), array("redirect" => "/admin/configs/vip"));
            } catch (Doctrine\DBAL\DBALException $ex) {
                return error(__("Unable to validate the data entered next to the database"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("configs/vip", array("vip" => $vip));
})->via("GET", "POST")->name("configs.vip");
Route::map("/configs/register", function () {
    if (Request::isPost()) {
        try {
            $questions = Input::post("secret_questions");
            Morpheus\Core\Config::save(array("register.terms" => Input::post("terms"), "register.confirm_email" => Input::post("confirm_email"), "register.create_blocked" => Input::post("create_blocked"), "register.secret_questions" => !empty($questions) ? array_filter($questions, function ($var) {
                return !empty($var);
            }) : array(), "register.bonus.vip.type" => Input::post("bonus_vip_type"), "register.bonus.vip.days" => Input::post("bonus_vip_days"), "register.bonus.coin.type" => Input::post("bonus_vip_coin"), "register.bonus.coin.amount" => Input::post("bonus_vip_coin_amount")));
            return success(__("Register settings has beed udpated"), array("redirect" => "/admin/configs/register"));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    $register = config("register");
    View::display("configs/register", array("register" => $register));
})->via("GET", "POST")->name("configs.register");
Route::map("/configs/register/add_custom_field", function () {
    if (Request::isPost()) {
        $post = Input::post();
        $validation = new Morpheus\Core\Validation($post, array("name" => Morpheus\Core\Validation::REQUIRED, "label" => Morpheus\Core\Validation::REQUIRED));
        if ($validation->isValid()) {
            try {
                $test = Connection::fetchColumn("SELECT TOP 1 " . $post["name"] . " FROM " . table_with_db("MEMB_INFO"));
                $fields = config("register.custom_fields", array());
                $options = array();
                if ($post["type"] == "select") {
                    $lines = explode("\n", trim(Input::post("options")));
                    foreach ($lines as $line) {
                        list($id, $option) = explode("=", $line);
                        $options[trim($id)] = trim($option);
                    }
                }
                $fields[$post["name"]] = array("name" => $post["name"], "label" => $post["label"], "required" => isset($post["required"]) && $post["required"] == 1, "type" => $post["type"], "options" => $options);
                Morpheus\Core\Config::save(array("register.custom_fields" => $fields));
                return success(__("Custom field has beed added"), array("redirect" => "/admin/configs/register"));
            } catch (Doctrine\DBAL\DBALException $ex) {
                return error(__("Unable to validate the data entered next to the database"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    } else {
        View::display("configs/register-add-custom-field");
    }
})->via("GET", "POST")->name("configs.registers.add_custom_field");
Route::get("/configs/register/delete_custom_field/:field", function ($field) {
    $fields = config("register.custom_fields", array());
    if (isset($fields[$field])) {
        unset($fields[$field]);
        Morpheus\Core\Config::save(array("register.custom_fields" => $fields));
        return success(__("Field %s has beed deleted", array($field)), array("redirect" => "/admin/configs/register"));
    }
})->name("configs.registers.delete_custom_field");
Route::map("/configs/rankings", function () {
    $configs = config("rankings", array());
    $customs = config("custom.rankings", array());
    if (Request::isPost()) {
        $post = Input::post("rankings");
        foreach (array_merge(Morpheus\Ranking\Ranking::availables(), $customs) as $name) {
            if (!isset($configs[$name])) {
                $configs[$name] = array();
            }
            $n = $post[$name]["name"];
            $configs[$name]["name"] = empty($n) ? $name : $n;
            $configs[$name]["active"] = isset($post[$name]["active"]) && $post[$name]["active"] == 1;
            $configs[$name]["custom"] = isset($configs[$name]["custom"]) && $configs[$name]["custom"];
            $configs[$name]["has_frequency"] = isset($configs[$name]["has_frequency"]) && $configs[$name]["has_frequency"];
        }
        try {
            Morpheus\Core\Config::save("rankings", $configs);
            return success(__("Rankings settings has ben updated"), array("redirect" => "/admin/configs/rankings"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    $rankings = array();
    foreach (array_merge(Morpheus\Ranking\Ranking::availables(), $customs) as $index => $ranking) {
        if (isset($configs[$ranking]) && isset($configs[$ranking]["sequence"])) {
            $rankings[$configs[$ranking]["sequence"]] = $ranking;
        } else {
            $rankings[$index] = $ranking;
        }
    }
    ksort($rankings);
    View::display("configs/rankings", array("rankings" => $rankings));
})->via("GET", "POST")->name("configs.rankings");
Route::map("/configs/rankings/add", function () {
    $configs = config("rankings", array());
    $customs = config("custom.rankings", array());
    if (Request::isPost()) {
        $post = Input::post();
        $validates = array("name" => Morpheus\Core\Validation::REQUIRED, "table" => Morpheus\Core\Validation::REQUIRED, "score_column" => Morpheus\Core\Validation::REQUIRED);
        if ($post["table"] !== "Character") {
            $validates += array("character_column" => Morpheus\Core\Validation::REQUIRED);
        }
        $validation = new Morpheus\Core\Validation($post, $validates);
        if ($validation->isValid()) {
            $name = util("Inflector")->classify($post["name"]);
            $rankings = array_merge($customs, Morpheus\Ranking\Ranking::availables());
            if (in_array($name, $rankings)) {
                return error(__("Ranking %s already exists!"));
            }
            $configs[$name] = array();
            $configs[$name]["name"] = $name;
            $configs[$name]["active"] = false;
            $configs[$name]["custom"] = true;
            $configs[$name]["has_frequency"] = false;
            $configs[$name]["configs"] = array("table" => $post["table"], "score_column" => $post["score_column"]);
            if ($post["table"] !== "Character") {
                $configs[$name]["configs"]["character_column"] = $post["character_column"];
            }
            $customs[] = $name;
            try {
                Morpheus\Core\Config::save(array("custom.rankings" => $customs, "rankings" => $configs));
                return success(__("Rankings has ben added"), array("redirect" => "/admin/configs/rankings"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("configs/add-ranking");
})->via("GET", "POST")->name("configs.rankings.add");
Route::get("/configs/rankings/delete/:ranking", function ($ranking) {
    $configs = config("rankings", array());
    $customs = config("custom.rankings", array());
    if (!in_array($ranking, $customs)) {
        return error(__("Custom ranking doen\\t exists"));
    }
    if (isset($configs[$ranking])) {
        unset($configs[$ranking]);
    }
    if (($key = array_search($ranking, $customs)) !== false) {
        unset($customs[$key]);
    }
    try {
        Morpheus\Core\Config::save(array("custom.rankings" => $customs, "rankings" => $configs));
        return success(__("Rankings has ben deleted"), array("redirect" => "/admin/configs/rankings"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("configs.rankings.delete");
Route::post("/configs/rankings/reorder", function () {
    $default = config("rankings", array());
    $sequences = Input::post("sequence");
    ksort($sequences);
    $rankings = array();
    foreach ($sequences as $sequence => $id) {
        $default[$id]["sequence"] = $sequence;
        $rankings[$id] = $default[$id];
    }
    try {
        Morpheus\Core\Config::save("rankings", $rankings);
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
});
Route::map("/configs/rankings/:ranking", function ($ranking) {
    $r = Morpheus\Ranking\Ranking::factory($ranking);
    $configs = $r->getConfigs();
    if (Request::isPost()) {
        $rankings = config("rankings", array());
        $hasFrequency = false;
        foreach ($configs as $f => $config) {
            $post = trim(Input::post($f));
            if (empty($post)) {
                unset($rankings[$ranking]["configs"][$f]);
            } else {
                $rankings[$ranking]["configs"][$f] = $post;
                if (!$hasFrequency) {
                    $hasFrequency = in_array($f, array("daily", "weekly", "monthly"));
                }
            }
        }
        $rankings[$ranking]["has_frequency"] = $hasFrequency;
        try {
            Morpheus\Core\Config::save("rankings", $rankings);
            return success(__("Ranking settings has ben updated"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("configs/edit-ranking", array("ranking" => $ranking, "configs" => $configs));
})->via("GET", "POST")->name("configs.rankings.edit");
Route::get("/configs/emails", function () {
    $emails = array();
    foreach (new DirectoryIterator(TEMPLATE_PATH . "mail" . DS) as $file) {
        if ($file->isFile()) {
            $emails[] = str_replace("." . $file->getExtension(), "", $file->getFilename());
        }
    }
    View::display("configs/emails", compact("emails"));
})->name("configs.emails");
Route::map("/configs/emails/edit/:file", function ($file) {
    if (Request::isPost()) {
        file_put_contents(TEMPLATE_PATH . "mail" . DS . $file . ".html", Input::post("filecontent"));
        return success(__("E-mail %s has been updated", $file), array("redirect" => "/admin/configs/emails/edit/" . $file));
    }
    $content = file_get_contents(TEMPLATE_PATH . "mail" . DS . $file . ".html");
    View::display("configs/edit-email", array("file" => $file, "filecontent" => $content));
})->via("GET", "POST")->name("configs.emails.edit");
Route::map("/configs/move", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("move.maps" => Input::post("maps"), "move.when_pk" => Input::post("when_pk") == 1));
            return success(__("Move settings has ben updated"), array("redirect" => "/admin/configs/move"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("configs/move");
})->via("GET", "POST")->name("configs.move");
Route::map("/configs/change-class", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("change_class.classes" => Input::post("classes"), "change_class.equalize_points" => Input::post("equalize_points") == 1));
            return success(__("Change class settings has ben updated"), array("redirect" => "/admin/configs/change-class"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("configs/change-class");
})->via("GET", "POST")->name("configs.change-class");
Route::map("/configs/master-resets", function () {
    $skillTree = server()->hasSkillTree();
    $requirements = array();
    foreach (config("vip.types", array()) as $code => $vip) {
        $requirements[$code] = array("resets" => 0, "clear_resets" => false, "level" => 0);
    }
    if (Request::isPost()) {
        try {
            $post = Input::post();
            foreach ($post["resets"] as $vip => $reset) {
                $requirements[$vip]["resets"] = $reset;
            }
            if (isset($post["clear_resets"])) {
                foreach ($post["clear_resets"] as $vip => $clear) {
                    $requirements[$vip]["clear_resets"] = $clear == 1;
                }
            }
            foreach ($post["level"] as $vip => $level) {
                $requirements[$vip]["level"] = $level;
            }
            $configs = array("mr.only_full" => isset($post["only_full"]) && $post["only_full"] == 1, "mr.clear_skills" => isset($post["clear_skills"]) && $post["clear_skills"] == 1, "mr.reset_class" => isset($post["reset_class"]) && $post["reset_class"] == 1, "mr.requirements" => $requirements);
            Morpheus\Core\Config::save($configs);
            return success(__("Master resets settings has ben updated"), array("redirect" => "/admin/configs/master-resets"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("configs/master-resets", array("requirements" => array_merge(config("mr.requirements", array()), $requirements), "skillTree" => $skillTree));
})->via("GET", "POST")->name("configs.master-reset");

?>