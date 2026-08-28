<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/panel/character/:character/change-avatar", service("character.avatar"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        $file = Input::file("avatar");
        if (empty($file)) {
            return error(array("avatar" => __("Required field")));
        }
        $dir = ROOT . DS . "uploads" . DS . "avatar" . DS;
        list($file) = rework_upload_files($file);
        $error = false;
        $upload = new upload($file, "pt_BR");
        $upload->file_new_name_body = uniqid();
        $upload->file_auto_rename = true;
        $upload->file_max_size = config("avatar.filesize", 100) . "K";
        $upload->allowed = array_merge(array("image/png", "image/jpeg"), config("avatar.allow_gif", false) ? array("image/gif") : array());
        $upload->process($dir);
        if ($upload->processed) {
            $upload->clean();
        } else {
            $error = $upload->error;
        }
        if (!$error) {
            try {
                $old = ROOT . DS . "uploads" . DS . "avatar" . DS . $character->getAvatar();
                $character->setAvatar($upload->file_dst_name);
                Connection::transactional(function () use($character) {
                    $character->update();
                    Morpheus\Account\Service::payRates("character.avatar", user(), $character);
                });
                if (file_exists($old) && !is_dir($old)) {
                    unlink($old);
                }
                return success(__("Avatar has been updated"), array("redirect" => "/panel/character/" . $character->getName() . "/change-avatar", "partial" => "login"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($error);
        }
    }
    $extensions = array(".png", ".jpg");
    if (config("avatar.allow_gif")) {
        $extensions[] = ".gif";
    }
    View::service("Avatar.change-avatar", "character.avatar", array("character" => $character, "extensions" => $extensions));
})->via("GET", "POST");

?>