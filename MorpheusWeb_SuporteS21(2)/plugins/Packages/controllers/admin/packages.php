<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Morpheus\Menu\Menu::getInstance("admin")->add(array("id" => "configs.packages", "url" => "/admin/packages", "title" => __("Packages")))->add(array("id" => "configs.sep-packages", "title" => "-"));
App::hook("admin.accounts.edit.render", function ($account) {
});
App::hook("admin.dashboard.after.render", function () {
    $data = Connection::fetchAll("SELECT COUNT(1) as total, SUM(p.price) as price, CONVERT(VARCHAR(10), l.create_date, 103) as date\n        FROM mw_logs l\n        JOIN mw_packages p on p.id = l.primary_key\n        WHERE l.type = 'package'\n        AND l.create_date BETWEEN DATEADD(DAY, -15, GETDATE()) and getdate()\n        GROUP BY CONVERT(VARCHAR(10), l.create_date, 103), CONVERT(DATE, l.create_date)\n        ORDER BY CONVERT(DATE, l.create_date)\n    ");
    View::display("Packages.graphic", array("layout" => false, "data" => $data));
});
Route::get("/packages", function () {
    $packages = Connection::fetchAll("SELECT p.*,\n        (SELECT COUNT(1) FROM mw_logs l WHERE l.primary_key = p.id) as total_sales\n        FROM mw_packages p\n        ORDER BY p.sequence\n    ");
    View::display("Packages.index", array("packages" => $packages));
})->name("packages");
Route::map("/packages/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "price" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $image = Input::file("image");
                $filename = NULL;
                if (!empty($image)) {
                    $dir = ROOT . DS . "uploads" . DS . "packages" . DS;
                    list($file) = rework_upload_files($image);
                    $upload = new upload($file, config("locale", "pt_BR"));
                    $upload->file_new_name_body = uniqid();
                    $upload->file_auto_rename = true;
                    $upload->process($dir);
                    if ($upload->processed) {
                        $upload->clean();
                        $filename = $upload->file_dst_name;
                    } else {
                        return error($upload->error);
                    }
                }
                $coins = array();
                foreach (Input::post("coins") as $coin => $amount) {
                    if (!empty($amount)) {
                        $coins[$coin] = $amount;
                    }
                }
                $sequence = Connection::fetchAssoc("SELECT COALESCE(MAX(sequence), 0) + 1 AS next FROM mw_packages");
                Connection::insert("mw_packages", array("package" => Input::post("name"), "description" => Input::post("desc"), "viptype" => Input::post("viptype"), "vipdays" => Input::post("vipdays"), "coins" => json_encode($coins), "price" => util("Number")->toFloat(Input::post("price")), "active" => Input::post("active") == 1, "sequence" => $sequence["next"], "image" => $filename), array("string", "string", "integer", "integer", "string", "float", "boolean", "integer", "string"));
                return success(__("Package has been saved"), array("redirect" => "/admin/packages"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Packages.add");
})->via("GET", "POST")->name("packages.add");
Route::map("/packages/edit/:id", function ($id = NULL) {
    $package = Connection::fetchAssoc("SELECT *\n        FROM mw_packages\n        where id = ?\n    ", array($id));
    $package["coins"] = empty($package["coins"]) ? "" : json_decode($package["coins"], true);
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "price" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $image = Input::file("image");
                $filename = $package["image"];
                if (!empty($image)) {
                    $dir = ROOT . DS . "uploads" . DS . "packages" . DS;
                    list($file) = rework_upload_files($image);
                    $upload = new upload($file, config("locale", "pt_BR"));
                    $upload->file_new_name_body = uniqid();
                    $upload->file_auto_rename = true;
                    $upload->process($dir);
                    if ($upload->processed) {
                        $upload->clean();
                        $filename = $upload->file_dst_name;
                    } else {
                        return error($upload->error);
                    }
                }
                $coins = array();
                foreach (Input::post("coins") as $coin => $amount) {
                    if (!empty($amount)) {
                        $coins[$coin] = $amount;
                    }
                }
                Connection::update("mw_packages", array("package" => Input::post("name"), "description" => Input::post("desc"), "viptype" => Input::post("viptype"), "vipdays" => Input::post("vipdays"), "price" => util("Number")->toFloat(Input::post("price")), "coins" => json_encode($coins), "active" => Input::post("active") == 1, "image" => $filename), array("id" => $id), array("string", "string", "integer", "integer", "float", "string", "boolean", "string", "integer"));
                return success(__("Package has been updated"), array("redirect" => "/admin/packages"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Packages.edit", array("package" => $package));
})->via("GET", "POST")->name("packages.edit");
Route::get("/packages/delete/:id", function ($id = NULL) {
    try {
        Connection::delete("mw_packages", array("id" => $id));
        return success(__("Package has been deleted"), array("redirect" => "/admin/packages"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/packages"));
    }
})->name("packages.delete");
Route::post("/packages/reorder", function () {
    $sequences = Input::post("sequence");
    foreach ($sequences as $sequence => $id) {
        Connection::update("mw_packages", array("sequence" => $sequence + 1), array("id" => $id));
    }
});
Route::map("/packages/config", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("packages.info" => Input::post("info")));
            return success(__("Package settings has been updated"), array("redirect" => "/admin/packages/config"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("Packages.config");
})->via("GET", "POST")->name("packages.config");

?>