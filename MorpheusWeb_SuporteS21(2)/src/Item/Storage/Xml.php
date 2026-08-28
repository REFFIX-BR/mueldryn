<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item\Storage;

class Xml extends AbstractStorage
{
    protected function parse()
    {
        $xml = new DOMDocument();
        $xml->load($this->getFile());
        foreach ($xml->getElementsByTagName("item") as $item) {
            $this->_items[$item->getAttribute("index")][$item->getAttribute("id")] = array("name" => $item->getAttribute("name"), "width" => $item->getAttribute("width"), "height" => $item->getAttribute("height"), "id" => $item->getAttribute("id"), "durability" => $item->getAttribute("durability"));
        }
        return $this->_items;
    }
}

?>