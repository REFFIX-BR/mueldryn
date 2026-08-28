<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/partial/:partial", function ($partial) {
    partial($partial);
});
Route::get("/forbidden", function () {
    View::display("forbidden");
});
Route::post("/editor/upload_files", function () {
    $dir = ROOT . DS . "uploads" . DS . "editor" . DS;
    $error = false;
    $file = $_FILES["file"];
    $upload = new upload($file, "pt_BR");
    $upload->file_auto_rename = true;
    $upload->file_max_size = "2M";
    $upload->allowed = array("image/*");
    $upload->process($dir);
    if ($upload->processed) {
        $files[] = $upload->file_dst_name;
        $upload->clean();
    } else {
        $error = $upload->error;
    }
    if (!$error) {
        echo upload_to("/editor/" . $upload->file_dst_name, true);
    }
});

?>