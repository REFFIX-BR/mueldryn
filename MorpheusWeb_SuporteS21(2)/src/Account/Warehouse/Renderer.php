<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Account\Warehouse;

class Renderer
{
    private $_warehouse = NULL;
    private $_itemUrl = NULL;
    private $_activeItem = NULL;
    private $_activeSlot = NULL;
    private $_enabledItemInfo = true;
    public function __construct(\Morpheus\Account\Warehouse $warehouse)
    {
        $this->setWarehouse($warehouse);
    }
    public function setWarehouse(\Morpheus\Account\Warehouse $warehouse)
    {
        $this->_warehouse = $warehouse;
        return $this;
    }
    public function getWarehouse()
    {
        return $this->_warehouse;
    }
    public function setItemUrl($url)
    {
        $this->_itemUrl = $url;
        return $this;
    }
    public function getItemUrl()
    {
        return $this->_itemUrl;
    }
    public function setActiveItem($active, $slot = NULL)
    {
        $this->_activeItem = $active;
        $this->_activeSlot = $slot;
        return $this;
    }
    public function getActiveItem()
    {
        return $this->_activeItem;
    }
    public function getActiveSlot()
    {
        return $this->_activeSlot;
    }
    public function setEnabledItemInfo($enabled)
    {
        $this->_enabledItemInfo = $enabled;
        return $this;
    }
    public function isEnabledItemInfo()
    {
        return $this->_enabledItemInfo;
    }
    public function render($slice = 1)
    {
        $html = "<div class=\"morpheus-warehouse\">";
        $starty = 0;
        $slices = $this->getWarehouse()->getHeight();
        if (1 < $this->getWarehouse()->getSlice()) {
            $slices = $this->getWarehouse()->getHeight() / $this->getWarehouse()->getSlice();
            $starty = $slices * ($slice - 1);
        }
        for ($y = 0; $y < $slices; $y++) {
            for ($x = 0; $x < $this->getWarehouse()->getWidth(); $x++) {
                $posy = $y + $starty;
                $i = $this->getWarehouse()->getItem($x, $posy);
                if ($i !== null) {
                    $slot = $this->getWarehouse()->getSlotByCord($x, $y) + (1 < $slice ? $slices * $this->getWarehouse()->getWidth() : 0);
                    if ($this->getActiveSlot()) {
                        $active = $slot == $this->getActiveSlot();
                    } else {
                        $active = $this->getActiveItem() && $this->getActiveItem()->getSerial() === $i->getSerial();
                    }
                    $html .= "<div class=\"item" . ($active ? " active" : "") . "\" style=\"height:" . $i->getHeight() * 32 . "px;width:" . $i->getWidth() * 32 . "px;top:" . $y * 32 . "px;left:" . $x * 32 . "px\"" . ($this->isEnabledItemInfo() ? " data-tooltip data-tooltip-url=\"" . resource_to("/item.php?hex=" . $i->getHex()) . "\"" : "") . ">";
                    if ($this->getItemUrl()) {
                        $url = $this->_parseMacros($this->getItemUrl(), $slot, $i);
                        $html .= "<a data-ajax href=\"" . url_to($url) . "\">";
                    }
                    $html .= "<div class=\"image\">";
                    $image = util("Item")->getImage($i);
                    $ix = max(16, $i->getWidth() * 32);
                    $iy = max(16, $i->getHeight() * 32);
                    if (strpos($image, ".php") === false) {
                        $fs = env("DOCUMENT_ROOT") . str_replace(array("/", "\\"), DS, parse_url($image, PHP_URL_PATH));
                        // resource_to pode devolver path relativo ao site
                        $candidates = array(
                            $fs,
                            ROOT . DS . "resources" . DS . "images" . DS . "items" . DS . $i->getSection() . "-" . $i->getIndex() . ".gif",
                        );
                        foreach ($candidates as $path) {
                            if ($path && @is_file($path)) {
                                $size = @getimagesize($path);
                                if (is_array($size)) {
                                    $ix = $size[0];
                                    $iy = $size[1];
                                }
                                break;
                            }
                        }
                    }
                    $html .= "<img style=\"position:absolute;top:50%;left:50%;margin-left:-" . ($ix / 2) . "px;margin-top:-" . ($iy / 2) . "px;\" src=\"" . $image . "\" />";
                    $html .= "</div>";
                    if ($this->getItemUrl()) {
                        $html .= "</a>";
                    }
                    $html .= "</div>";
                }
            }
        }
        $html .= "</div>";
        return $html;
    }
    public function renderAll()
    {
        $html = "";
        for ($i = 1; $i <= $this->getWarehouse()->getSlice(); $i++) {
            $html .= $this->render($i);
        }
        return $html;
    }
    private function _parseMacros($text, $slot, \Morpheus\Item\Item $item)
    {
        preg_match_all("/\\{(?P<name>\\w+)\\}/i", $text, $match, PREG_SET_ORDER);
        $replace = array();
        foreach ($match as $key => $value) {
            if ($value["name"] === "slot") {
                $replace[$value[0]] = $slot;
            } else {
                $replace[$value[0]] = $item->{"get" . \Morpheus\Util\Inflector::classify($value["name"])}();
            }
        }
        return str_replace(array_keys($replace), array_values($replace), $text);
    }
    public function __toString()
    {
        return $this->renderAll();
    }
}

?>