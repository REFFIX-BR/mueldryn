<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/plugins", function () {
    $checkUpdates = Input::get("check-updates") !== NULL;
    if ($checkUpdates) {
        Morpheus\Core\Plugin::checkUpdates(true);
    }
    $plugins = Morpheus\Core\Plugin::getInstance()->getPluginsInfo();
    View::display("plugins/index", array("plugins" => $plugins));
})->name("plugins");
Route::get("/plugins/update/:plugin", function ($plugin) {
    $plugin = Morpheus\Core\Plugin::getPluginInfo($plugin);
    $update = new VisualAppeal\AutoUpdate(ROOT . DS . "tmp", PLUGIN_PATH . $plugin["name"] . DS);
    $update->setCurrentVersion($plugin["version"]);
    $update->setUpdateUrl(UPDATE_SERVER_URL . "/plugins/" . $plugin["name"] . "/");
    $update->setUpdateFile("update.ini");
    $update->checkUpdate();
    if ($update->newVersionAvailable()) {
        $update->onEachUpdateFinish(function ($version) use($plugin) {
            $install = PLUGIN_PATH . $plugin["name"] . DS . "install.php";
            if (file_exists($install)) {
                try {
                    include $install;
                } catch (Exception $ex) {
                }
                unlink($install);
            }
        });
        $result = $update->update();
        if ($result === true) {
            Morpheus\Core\Plugin::checkUpdates(true);
            return success(__("Plugin %s has been updated to version %s", array($plugin["name"], $update->getLatestVersion())), array("redirect" => "/admin/plugins"));
        }
        return error(__($result));
    }
    return error(__("No new version available"), array("redirect" => "/admin/plugins"));
})->name("plugins.update");
Route::map("/plugins/install", function () {
    if (Request::isPost()) {
        $zip = new ZipArchive();
        list($file) = rework_upload_files(Input::file("file"));
        $fs = new Symfony\Component\Filesystem\Filesystem();
        if ($zip->open($file["tmp_name"])) {
            $folder = $zip->getNameIndex(0);
            $folder = substr($folder, 0, strlen($folder) - 1);
            if (Morpheus\Core\Plugin::exists($folder)) {
                return error(__("Plugin %s already exists", array($folder)));
            }
            if ($zip->getFromName($folder . "/info.php")) {
                $zip->extractTo(PLUGIN_PATH);
                $zip->close();
                $install = PLUGIN_PATH . $folder . DS . "install.php";
                if ($fs->exists($install)) {
                    require_once $install;
                    $fs->remove(PLUGIN_PATH . $folder . DS . "install.php");
                }
                return success(__("Plugin has been installed"), array("redirect" => "/admin/plugins"));
            }
            return error(__("Info file not found in your plugin"));
        }
        return error(__("Zip file not recognised"));
    }
    View::display("plugins/install");
})->via("GET", "POST")->name("plugins.install");
Route::get("/plugins/active/:plugin", function ($plugin) {
    try {
        $info = Morpheus\Core\Plugin::getPluginInfo($plugin);
        if (!isset($info["license"]) || $info["license"]) {
            $licence = new Morpheus\Core\License();
            $data = $licence->getData();
            if (!in_array($plugin, $data["plugins"])) {
                return error(__("You need to buy this plugin before using"));
            }
        }
        Morpheus\Core\Plugin::activate($plugin);
        return success(__("Plugin %s has been activated", $plugin), array("redirect" => "/admin/plugins", "partial" => "sidebar"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/plugins"));
    }
})->name("plugins.active");
Route::get("/plugins/deactivate/:plugin", function ($plugin) {
    try {
        Morpheus\Core\Plugin::deactivate($plugin);
        return success(__("Plugin %s has been deactivated.", $plugin), array("redirect" => "/admin/plugins", "partial" => "sidebar"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/plugins"));
    }
})->name("plugins.deactivate");

?>