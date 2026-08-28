<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item\Storage;

class Serialize extends AbstractStorage
{
    protected function parse()
    {
        $this->_items = unserialize(file_get_contents($this->getFile()));
        return $this->_items;
    }
}

?>