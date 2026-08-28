<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Exception;

class FileNotFoundException extends Exception
{
    public function __construct($message = "", $code = 0, \Throwable $previous = NULL)
    {
        $message = "File " . $message . " not found";
        parent::__construct($message, $code, $previous);
    }
}

?>