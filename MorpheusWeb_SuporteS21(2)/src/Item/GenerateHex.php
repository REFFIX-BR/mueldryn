<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class GenerateHex
{
    private $_item = NULL;
    public function __construct(Item $item)
    {
        $this->setItem($item);
    }
    public function setItem(Item $item)
    {
        $this->_item = $item;
        return $this;
    }
    public function getItem()
    {
        return $this->_item;
    }
    public function generate()
    {
        $item = $this->getItem();
        if ($item->getIndex() === null || $item->getSection() === null) {
            return str_repeat("F", $this->getItem()->getItemSize());
        }
        $hex = "";
        $hex .= $this->_generateByte1();
        $hex .= $this->_generateByte2();
        $hex .= $this->_generateByte3();
        $hex .= $this->_generateByte4to7();
        $hex .= $this->_generateByte8();
        if ($this->getItem()->getDbVersion() <= 2) {
            $hex .= $this->_generateByte9();
            $hex .= "00";
        } else {
            $hex .= $this->_generateByte9();
            $hex .= $this->_generateByte10();
            $hex .= $this->_generateByte11();
            $hex .= $this->_generateByte12to16();
        }
        if (3 < $this->getItem()->getDbVersion()) {
            if ($this->getItem()->getItemSize() >= 64) {
                $hex .= $this->_generateByte17to21();
            }
            $remaining = $this->getItem()->getItemSize() - strlen($hex);
            if ($remaining > 0) {
                $hex .= str_repeat("F", $remaining);
            }
        }
        return strtoupper($hex);
    }
    private function _generateByte1()
    {
        if ($this->getItem()->getDbVersion() < 2) {
            return $this->_fix(dechex(($this->getItem()->getIndex() & 31 | $this->getItem()->getSection() << 5 & 224) & 255));
        }
        $dec = $this->getItem()->getIndex() % 256; // Previne gerar tamanhos maiores do que o limite da string
        return $this->_fix(dechex($dec));
    }
    private function _generateByte2()
    {
        $item = $this->getItem();
        $level = $item->getLevel() * 8;
        $level += $item->getSkill() ? 128 : 0;
        $level += $item->getLuck() ? 4 : 0;
        switch ($item->getOption()) {
            case 4:
                $level += 1;
                break;
            case 8:
                $level += 2;
                break;
            case 12:
                $level += 3;
                break;
            case 16:
                $level += 0;
                break;
            case 20:
                $level += 1;
                break;
            case 24:
                $level += 2;
                break;
            case 28:
                $level += 3;
                break;
        }
        return $this->_fix(dechex($level));
    }
    private function _generateByte3()
    {
        return $this->_fix(dechex($this->getItem()->getDurability()));
    }
    private function _generateByte4to7()
    {
        $serial = $this->getItem()->getSerial();
        if (8 < strlen($serial)) {
            $serial = substr($serial, 0, 8);
        }
        return $this->_fix($serial, 8);
    }
    private function _generateByte8()
    {
        $item = $this->getItem();
        $excellent = 0;
        $excellent += $item->isUnique() ? 128 : 0;
        $excellent += 16 <= $item->getOption() ? 64 : 0;
        $excellent += $item->getExcellent(0) ? 1 : 0;
        $excellent += $item->getExcellent(1) ? 2 : 0;
        $excellent += $item->getExcellent(2) ? 4 : 0;
        $excellent += $item->getExcellent(3) ? 8 : 0;
        $excellent += $item->getExcellent(4) ? 16 : 0;
        $excellent += $item->getExcellent(5) ? 32 : 0;
        return $this->_fix(dechex($excellent));
    }
    private function _generateByte9()
    {
        if ($this->getItem()->getAncient()->get() === 1) {
            return "05";
        }
        if ($this->getItem()->getAncient()->get() === 2) {
            return "0A";
        }
        return "00";
    }
    private function _generateByte10()
    {
        return substr($this->_fix(dechex($this->getItem()->getSection())), 1) . ($this->getItem()->getRefine() ? 8 : 0);
    }
    private function _generateByte11()
    {
        $harmony = dechex($this->getItem()->getHarmony()->getType());
        $harmony .= dechex($this->getItem()->getHarmony()->getLevel());
        return $harmony;
    }
    private function _generateByte12to16()
    {
        $sockets = "";
        $sock = new Socket($this->getItem());
        $max = $sock->max();
        for ($i = 0; $i < 5; $i++) {
            if ($i < $max) {
                $socket = $this->getItem()->getSocket($i);
                if ($socket && $socket->get() !== null) {
                    $sockets .= $this->_fix(dechex($socket->get()));
                } else {
                    $sockets .= $this->_fix(dechex($sock->getNoValue()));
                }
            } else {
                $sockets .= $this->_fix(dechex($sock->getNoValue()));
            }
        }
        return $this->_fix($sockets, 10);
    }
    private function _generateByte17to21()
    {
        $serial = $this->getItem()->getSerial();
        if (8 < strlen($serial)) {
            $serial = substr($serial, 8, 8);
        }
        return $this->_fix($serial, 8);
    }
    private function _fix($string, $size = 2)
    {
        return str_pad($string, $size, 0, STR_PAD_LEFT);
    }
}

?>