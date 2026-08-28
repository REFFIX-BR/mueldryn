<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Avatar\Util;

class Avatar
{
    public static function url($avatar, $default = "/images/no-avatar.png")
    {
        if (empty($avatar)) {
            return asset_to($default);
        }
        if (!file_exists(ROOT . DS . "uploads" . DS . "avatar" . DS . $avatar)) {
            return asset_to($default);
        }
        return upload_to("/avatar/" . $avatar);
    }
}

?>