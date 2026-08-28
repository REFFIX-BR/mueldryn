<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Matrix
{
    private $_items = array();
    private $_map = array();
    private $_width = 0;
    private $_height = 0;
    private $_dbVersion = 3;
    private $_slice = 1;
    private $_loaded = false;
    public function __construct($width, $height, $slice = 1)
    {
        for ($x = 0; $x < $width; $x++) {
            for ($y = 0; $y < $height; $y++) {
                $this->_items[$x][$y] = null;
                $this->_map[$x][$y] = false;
            }
        }
        $this->_width = $width;
        $this->_height = $height;
        $this->_slice = $slice;
        $this->_dbVersion = config("item.db_version", 3);
    }
    public function getWidth()
    {
        return $this->_width;
    }
    public function getHeight()
    {
        return $this->_height;
    }
    public function getSlice()
    {
        return $this->_slice;
    }
    public function isLoaded()
    {
        return $this->_loaded;
    }
    public function getDbVersion()
    {
        return $this->_dbVersion;
    }
    public function setDbVersion($version)
    {
        $this->_dbVersion = $version;
        return $this;
    }
    public function getItemSize()
    {
        $size = config("item.size");
        if (!empty($size)) {
            return (int) $size;
        }
        if ($this->getDbVersion() <= 2) {
            return 20;
        }
        if (3 <= $this->getDbVersion() && $this->getDbVersion() < 4) {
            return 32;
        }
        if (5 <= $this->getDbVersion()) {
            return 50;
        }
        return 64;
    }
    public function load($hex)
    {
        $lenght = $this->getWidth() * $this->getHeight() * $this->getItemSize();
        $hex = substr($hex, 0, $lenght);
        $items = str_split($hex, $this->getItemSize());
        for ($i = 0; $i < $this->getWidth() * $this->getHeight(); $i++) {
            if (!isset($items[$i])) {
                continue;
            }
            $cords = $this->getCordBySlot($i);
            $item = new Item($items[$i], $this->getDbVersion());
            if (!$item->isHexEmpty()) {
                try {
                    $item->parse();
                    if ($item->getWidth() < 1) {
                        $item->setSize(1, max(1, (int) $item->getHeight()));
                    }
                    if ($item->getHeight() < 1) {
                        $item->setSize(max(1, (int) $item->getWidth()), 1);
                    }
                    // Se não cabe na grade, ainda tenta 1x1 para não “sumir”
                    if ($cords["y"] + $item->getHeight() > $this->getHeight() || $cords["x"] + $item->getWidth() > $this->getWidth()) {
                        $item->setSize(1, 1);
                    }
                    if ($cords["y"] + $item->getHeight() <= $this->getHeight() && $cords["x"] + $item->getWidth() <= $this->getWidth()) {
                        $this->_put($item, $cords["x"], $cords["y"]);
                    }
                } catch (\Exception $e) {
                    // ignora item inválido e segue o baú
                }
            }
        }
        $this->_loaded = true;
        return $this;
    }
    public function generate()
    {
        $temp = array();
        for ($y = 0; $y < $this->getHeight(); $y++) {
            for ($x = 0; $x < $this->getWidth(); $x++) {
                $item = $this->getItem($x, $y);
                if ($item == null) {
                    $temp[] = str_repeat("F", $this->getItemSize());
                } else {
                    $temp[] = $item->generate();
                }
            }
        }
        return join("", $temp);
    }
    public function getItems()
    {
        return $this->_items;
    }
    public function addItem(Item $item)
    {
        $item->update();
        if ($this->getWidth() < $item->getWidth()) {
            throw new \Exception("Item width is bigger than containers width");
        }
        if ($this->getHeight() < $item->getHeight()) {
            throw new \Exception("Item width is bigger than containers width");
        }
        if ($item->getHeight() <= 0 || $item->getWidth() <= 0) {
            throw new \Exception("Invalid item size (width <= 0 or height <= 0)");
        }
        for ($y = 0; $y <= $this->getHeight() - $item->getHeight(); $y++) {
            for ($x = 0; $x <= $this->getWidth() - $item->getWidth(); $x++) {
                if ($this->_fit($item, $x, $y)) {
                    $this->_put($item, $x, $y);
                    return true;
                }
            }
        }
        return false;
    }
    private function _put(Item $item, $posx, $posy)
    {
        if ($this->getWidth() <= $posx || $posx < 0) {
            throw new \Exception("Invalid slot position. (x > width || x < 0)");
        }
        if ($this->getHeight() <= $posy || $posy < 0) {
            throw new \Exception("Invalid slot position. (y > height || y < 0)");
        }
        for ($y = $posy; $y < $posy + $item->getHeight(); $y++) {
            for ($x = $posx; $x < $posx + $item->getWidth(); $x++) {
                $this->_map[$x][$y] = true;
            }
        }
        $this->_items[$posx][$posy] = $item;
        return true;
    }
    private function _fit($item, $posx, $posy)
    {
        if ($this->getWidth() <= $posx || $posx < 0) {
            throw new \Exception("Invalid slot position. (x > width || x < 0)");
        }
        if ($this->getHeight() <= $posy || $posy < 0) {
            throw new \Exception("Invalid slot position. (y > height || y < 0)");
        }
        for ($y = $posy; $y < $posy + $item->getHeight(); $y++) {
            for ($x = $posx; $x < $posx + $item->getWidth(); $x++) {
                $height = $this->getHeight() / $this->getSlice();
                $slice = ceil(($y + 1) / $height);
                if ($height * $slice < $posy + $item->getHeight()) {
                    return false;
                }
                if ($this->hasItem($x, $y)) {
                    return false;
                }
            }
        }
        return true;
    }
    public function getItem($x, $y)
    {
        if ($x < 0 || $y < 0 || $this->getWidth() <= $x || $this->getHeight() <= $y) {
            return null;
        }
        return $this->_items[$x][$y];
    }
    public function getItemBySlot($slot)
    {
        $item = null;
        $cords = $this->getCordBySlot($slot);
        if (isset($this->_items[$cords["x"]][$cords["y"]])) {
            return $this->getItem($cords["x"], $cords["y"]);
        }
    }
    public function getItemBySerial($serial)
    {
        $item = null;
        for ($y = 0; $y < $this->getHeight(); $y++) {
            for ($x = 0; $x < $this->getWidth(); $x++) {
                $i = $this->getItem($x, $y);
                if ($i !== null && $i->getSerial() === $serial) {
                    $item = $i;
                    break;
                }
            }
        }
        return $item;
    }
    public function getItemsByType($section, $index)
    {
        $items = array();
        for ($y = 0; $y < $this->getHeight(); $y++) {
            for ($x = 0; $x < $this->getWidth(); $x++) {
                $i = $this->getItem($x, $y);
                if ($i !== null && $i->getSection() === $section && $i->getIndex() === $index) {
                    $items[] = $i;
                }
            }
        }
        return $items;
    }
    public function removeItem(Item $item)
    {
        for ($y = 0; $y < $this->getHeight(); $y++) {
            for ($x = 0; $x < $this->getWidth(); $x++) {
                $i = $this->getItem($x, $y);
                if ($i !== null && $i->getHex() === $item->getHex()) {
                    $this->_items[$x][$y] = null;
                    for ($posy = $y; $posy < $y + $item->getHeight(); $posy++) {
                        for ($posx = $x; $posx < $x + $item->getWidth(); $posx++) {
                            $this->_map[$posx][$posy] = false;
                        }
                    }
                    return true;
                }
            }
        }
        return false;
    }
    public function hasItem($x, $y)
    {
        if ($x < 0 || $y < 0 || $this->getWidth() <= $x || $this->getHeight() <= $y) {
            return false;
        }
        return $this->_map[$x][$y];
    }
    public function getCordBySlot($slot)
    {
        return array("x" => $slot % $this->getWidth(), "y" => floor($slot / $this->getWidth()));
    }
    public function getSlotByCord($x, $y)
    {
        return $y * $this->getWidth() + $x;
    }
    public function debug()
    {
        $html = "<table style=\"display:inline-block;border-collapse:collapse;margin-right:10px\">";
        for ($y = 0; $y < $this->getHeight(); $y++) {
            if (0 < $y && $y % ($this->getHeight() / $this->getSlice()) == 0) {
                $html .= "</table><table style=\"display:inline-block;border-collapse:collapse;margin-right:10px;\">";
            }
            $html .= "<tr>";
            for ($x = 0; $x < $this->getWidth(); $x++) {
                $html .= "<td width=\"32\" height=\"32\" style=\"border:1px solid #000;background:" . ($this->hasItem($x, $y) ? "red" : "green") . "\"></td>";
            }
            $html .= "</tr>";
        }
        $html .= "</table>";
        return $html;
    }
}

?>