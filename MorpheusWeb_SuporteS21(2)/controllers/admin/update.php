<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/update/changelog", function () {
    $q = Input::get("versions");
    $http = Morpheus\Network\Http::create("GET", MORPHEUS_WEBSITE . "versions/changelog/" . $q)->send();
    $response = $http->getResponse();
    if ($response["header"]["status"] === 200) {
        $data = (array) json_decode($response["body"], true);
        $versions = $data;
    }
    View::display("update/changelog", array("versions" => $versions));
});
Route::get("/update(/:action)", function ($action = "view") {
    if (admin("super_user")) {
        $update = new VisualAppeal\AutoUpdate(ROOT . DS . "tmp", ROOT . DS);
        $update->setCurrentVersion(MORPHEUS_VERSION);
        $update->setUpdateUrl(UPDATE_SERVER_URL);
        $update->setUpdateFile("update.ini");
        $update->addLogHandler(new Monolog\Handler\StreamHandler(LOGS_PATH . "update.log"));
        if ($action === "view") {
            $status = 1;
            if ($update->checkUpdate() === false) {
                $status = 2;
            }
            if ($update->newVersionAvailable()) {
                $status = 3;
                $versions = array_map(function ($version) {
                    return (string) $version;
                }, $update->getVersionsToUpdate());
                View::set("version", $update->getLatestVersion());
                View::set("versions", $versions);
            }
            View::display("update/index", array("status" => $status));
        } else {
            if ($action === "apply") {
                $update->onEachUpdateFinish(function ($version) {
                    $instal = ROOT . DS . "install.php";
                    if (file_exists($instal)) {
                        try {
                            include $instal;
                        } catch (Exception $ex) {
                        }
                        unlink($instal);
                    }
                });
                $result = $update->update();
                if ($result === true) {
                    return success(__("Morpheus MuWeb atualizado para versão %s com sucesso", $update->getLatestVersion()), array("redirect" => "/admin/update"));
                }
                return error(__("Não foi possível atualizar a Morpheus MuWeb atualize manualmente: %s", array(UPDATE_SERVER_URL . "versions/" . $update->getLatestVersion() . ".zip")));
            }
            App::notFound();
        }
    } else {
        App::notFound();
    }
});

?>