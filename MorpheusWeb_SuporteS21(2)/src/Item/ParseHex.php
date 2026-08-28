<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class ParseHex
{
    private $_hex = NULL;
    private $_dbVersion = NULL;
    public function __construct($hex, $dbVersion = NULL)
    {
        $this->setHex($hex);
        $this->setDbVersion($dbVersion === null ? config("item.db_version", 3) : $dbVersion);
    }
    public function setHex($hex)
    {
        $this->_hex = $hex;
        return $this;
    }
    public function getHex()
    {
        return $this->_hex;
    }
    public function setDbVersion($dbVersion)
    {
        $this->_dbVersion = $dbVersion;
        return $this;
    }
    public function getDbVersion()
    {
        return $this->_dbVersion;
    }
    public function getIndex()
    {
        $temp = hexdec(substr($this->getHex(), 0, 2));
        if ($this->getDbVersion() < 2) {
            return $temp & 31;
        }
        $unique = hexdec(substr($this->getHex(), 14, 2));
        $plus = 128 <= $unique ? 256 : 0;
        return $plus + $temp;
    }
    public function getSection()
    {
        $temp = hexdec(substr($this->getHex(), 0, 2));
        $unique = hexdec(substr($this->getHex(), 14, 2));
        if ($this->getDbVersion() < 2) {
            return (($temp & 224) >> 5) + (($unique & 128) == 128 ? 8 : 0);
        }
        return hexdec(substr($this->getHex(), 18, 1));
    }
    public function getLevel()
    {
        return (hexdec(substr($this->getHex(), 2, 2)) & 120) >> 3;
    }
    public function getExcellents()
    {
        $temp = hexdec(substr($this->getHex(), 14, 2));
        if (255 < $this->getIndex()) {
            $temp = $temp - 128;
        }
        $exc = array();
        $exc[0] = ($temp & 1) == 1;
        $exc[1] = ($temp & 2) == 2;
        $exc[2] = ($temp & 4) == 4;
        $exc[3] = ($temp & 8) == 8;
        $exc[4] = ($temp & 16) == 16;
        $exc[5] = ($temp & 32) == 32;
        return $exc;
    }
    public function getDurability()
    {
        return hexdec(substr($this->getHex(), 4, 2));
    }
    public function getLuck()
    {
        return (hexdec(substr($this->getHex(), 2, 2)) & 4) == 4;
    }
    public function getOption()
    {
        return ((hexdec(substr($this->getHex(), 2, 2)) & 3) + ((hexdec(substr($this->getHex(), 14, 2)) & 64) == 64 ? 4 : 0)) * 4;
    }
    public function getSerial()
    {
        $serial = substr($this->getHex(), 6, 8);
        if ($this->getDbVersion() >= 4 && strlen($this->getHex()) >= 64) {
            $serial .= substr($this->getHex(), 32, 8);
        }
        return $serial;
    }
    public function getSkill()
    {
        return (hexdec(substr($this->getHex(), 2, 2)) & 128) === 128;
    }
    public function getUnique()
    {
        return (hexdec(substr($this->getHex(), 14, 2)) & 128) == 128;
    }
    public function getAncient()
    {
        $temp = hexdec(substr($this->getHex(), 17, 1));
        if (in_array($temp, array(5, 9))) {
            return 1;
        }
        if (in_array($temp, array(6, 10))) {
            return 2;
        }
        return 0;
    }
    public function getRefine()
    {
        return hexdec(substr($this->getHex(), 19, 1)) == 8;
    }
    public function getHarmonyType()
    {
        $tmp = substr($this->getHex(), 20, 1);
        return hexdec($tmp);
    }
    public function getHarmonyLevel()
    {
        $tmp = substr($this->getHex(), 21, 1);
        return hexdec($tmp);
    }
    public function getSockets()
    {
        $sock = array();
        $sock[0] = hexdec(substr($this->getHex(), 22, 2));
        $sock[1] = hexdec(substr($this->getHex(), 24, 2));
        $sock[2] = hexdec(substr($this->getHex(), 26, 2));
        $sock[3] = hexdec(substr($this->getHex(), 28, 2));
        $sock[4] = hexdec(substr($this->getHex(), 30, 2));
        return $sock;
    }
}

?>