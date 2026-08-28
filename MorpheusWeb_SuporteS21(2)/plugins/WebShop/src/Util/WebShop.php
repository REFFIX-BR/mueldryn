<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace WebShop\Util;

class WebShop
{
    public static function image($product)
    {
        if (empty($product["image"]) || !file_exists(ROOT . DS . "uploads" . DS . "webshop" . DS . $product["image"])) {
            return resource_to("/images/items/" . $product["section"] . "-" . $product["index_"] . ".gif");
        }
        return upload_to("/webshop/" . $product["image"]);
    }
}

?>