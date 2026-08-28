<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Serial
{
    private $_value = NULL;
    private $_item = NULL;
    public function __construct($value = NULL)
    {
        $this->_value = $value;
    }
    public function get()
    {
        return $this->_value;
    }
    public function set($value)
    {
        $this->_value = $value;
    }
    public function getItem()
    {
        return $this->_item;
    }
    public function setItem($item)
    {
        $this->_item = $item;
    }
    public function generate()
    {
        $stmt = \Morpheus\Database\Connection::prepare("exec WZ_GetItemSerial");
        $stmt->execute();
        $res = $stmt->fetch();
        $serial = array_shift($res);
        $serial = substr($serial, 0, 8);
        if ($this->getItem() !== null && $this->getItem()->getDbVersion() >= 4 && $this->getItem()->getItemSize() >= 64) {
            $stmt = \Morpheus\Database\Connection::prepare("exec WZ_GetItemSerial2 1");
            $stmt->execute();
            $res = $stmt->fetch();
            $serial2 = array_shift($res);
            $serial2 = substr($serial2, 0, 8);
            $serial = $serial . $serial2;
        }
        $this->set($serial);
        return $serial;
    }
    public function __toString()
    {
        return $this->get();
    }
}

?>