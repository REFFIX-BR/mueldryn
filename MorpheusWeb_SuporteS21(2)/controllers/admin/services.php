<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/services", function () {
    View::display("services/index", array("services" => util("Hash")->nest(Morpheus\Account\Service::getAllServices())));
})->name("configs.services");
Route::get("/services/order", function () {
    $services = Connection::fetchAll("SELECT * FROM mw_services WHERE parent_id IS NULL OR parent_id = 0 ORDER BY sequence");
    View::display("services/order", array("services" => $services));
});
Route::map("/services/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "service" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::transactional(function () {
                    Connection::insert("mw_services", array("name" => Input::post("name"), "service" => Input::post("service"), "allowed" => Input::post("allowed") == 1, "parent_id" => Input::post("parent_id"), "url" => Input::post("url"), "active" => Input::post("active") == 1, "sequence" => 1), array("string", "string", "boolean", "integer", "string", "boolean", "integer"));
                    if (Input::post("viptype")) {
                        foreach (Input::post("viptype") as $id => $name) {
                            Connection::insert("mw_viptype_services", array("viptype" => $id, "service_id" => Connection::lastInsertId()), array("integer", "integer"));
                        }
                    }
                });
                return success(__("Service has been saved", Input::post("name")), array("redirect" => "/admin/services"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $services = Connection::fetchAll("select * \n        from mw_services \n        where parent_id is null \n        or parent_id = 0 \n        order by sequence\n    ");
    View::display("services/add", array("services" => $services));
})->via("GET", "POST")->name("configs.services.add");
Route::map("/services/edit/:id", function ($id) {
    $service = Connection::fetchAssoc("SELECT * FROM mw_services WHERE id = :id", array("id" => $id));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::transactional(function () use($service) {
                    Connection::delete("mw_viptype_services", array("service_id" => $service["id"]));
                    Connection::update("mw_services", array("name" => Input::post("name"), "url" => Input::post("url"), "active" => Input::post("active") == 1, "allowed" => Input::post("allowed") == 1, "parent_id" => Input::post("parent_id")), array("id" => $service["id"]), array("string", "string", "boolean", "boolean", "integer"));
                    if (Input::post("viptype")) {
                        foreach (Input::post("viptype") as $id => $name) {
                            Connection::insert("mw_viptype_services", array("viptype" => $id, "service_id" => $service["id"]), array("integer", "integer"));
                        }
                    }
                });
                return success(__("Service has been updated", $service["name"]), array("redirect" => "/admin/services/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $services = Connection::fetchAll("SELECT * FROM mw_services WHERE parent_id is null OR parent_id = 0 ORDER BY sequence");
    $service = Morpheus\Account\Service::getService($service["service"]);
    $service["rates"] = json_decode($service["rates"], true);
    View::display("services/edit", array("service" => $service, "services" => $services));
})->via("GET", "POST")->name("configs.services.edit");
Route::get("/services/deactive/:service", function ($id) {
    $service = Connection::fetchAssoc("SELECT * FROM mw_services WHERE id = :id", array("id" => $id), array("integer"));
    try {
        Morpheus\Account\Service::deactivate($service["service"]);
        return success(__("Service has been deactivated", $service["name"]), array("redirect" => "/admin/services"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/services"));
    }
})->name("configs.services.deactive");
Route::get("/services/active/:service", function ($id) {
    $service = Connection::fetchAssoc("SELECT * FROM mw_services WHERE id = :id", array("id" => $id), array("integer"));
    try {
        Morpheus\Account\Service::activate($service["service"]);
        return success(__("Service has been activated", $service["name"]), array("redirect" => "/admin/services"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/services"));
    }
})->name("configs.services.active");
Route::post("/services/reorder", function () {
    $sequences = Input::post("sequence");
    foreach ($sequences as $sequence => $id) {
        Connection::update("mw_services", array("sequence" => $sequence + 1), array("id" => $id), array("integer", "integer"));
    }
});
Route::map("/services/add_rate/:service", function ($service) {
    $service = Connection::fetchAssoc("SELECT * FROM mw_services WHERE id = :id", array("id" => $service), array("integer"));
    if (Request::isPost()) {
        try {
            Morpheus\Account\Service::addRate($service["service"], array("conditions" => Input::post("conditions"), "type" => Input::post("type"), "amount" => Input::post("amount"), "coin" => Input::post("coin")));
            return success(__("Rate has been saved"), array("redirect" => "/admin/services/edit/" . $service["id"]));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    View::display("services/add_rate", array("layout" => false, "service" => $service));
})->via("GET", "POST")->name("services.rates.add");
Route::get("/services/delete_rate/:service/:index", function ($service, $index) {
    $service = Connection::fetchAssoc("SELECT * FROM mw_services WHERE id = :id", array("id" => $service), array("integer"));
    try {
        Morpheus\Account\Service::deleteRate($service["service"], $index);
        return success(__("Rate has been deleted"), array("redirect" => "/admin/services/edit/" . $service["id"]));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("service.rates.delete");

?>