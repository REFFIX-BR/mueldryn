<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character\Inventory;

class Renderer
{
    private $_inventory = NULL;
    private $_itemUrl = NULL;
    private $_activeItem = NULL;
    private $_activeSlot = NULL;
    private $_enabledItemInfo = true;

    private static $_equipSlotNames = array(
        0 => 'Mão esquerda',
        1 => 'Mão direita',
        2 => 'Elmo',
        3 => 'Armadura',
        4 => 'Calça',
        5 => 'Luvas',
        6 => 'Botas',
        7 => 'Asas / Capa',
        8 => 'Pet',
        9 => 'Pingente',
        10 => 'Anel',
        11 => 'Anel',
        12 => 'Pentagrama',
        13 => 'Brinco',
        14 => 'Brinco',
    );

    public function __construct(\Morpheus\Character\Inventory $inventory)
    {
        $this->setInventory($inventory);
    }
    public function setInventory(\Morpheus\Character\Inventory $inventory)
    {
        $this->_inventory = $inventory;
        return $this;
    }
    public function getInventory()
    {
        return $this->_inventory;
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
        $html = "<div class=\"morpheus-inventory\">";
        $html .= "<div class=\"morpheus-inventory-equipments\">";
        $equipCount = $this->getInventory()->getDbVersion() >= 5 ? 15 : 12;
        for ($i = 0; $i < $equipCount; $i++) {
            $it = $this->getInventory()->getEquipment($i);
            if ($it === null || !util("Item")->hasDisplayImage($it)) {
                continue;
            }
            $slot = "e" . $i;
            if ($this->getActiveSlot()) {
                $active = substr($this->getActiveSlot(), 0, 1) == "e" && $i == substr($this->getActiveSlot(), 1);
            } else {
                $active = $this->getActiveItem() && $this->getActiveItem()->getSerial() === $it->getSerial();
            }
            $html .= "<div class=\"item-mapping item" . $i . ($active ? " active" : "") . "\"" . ($this->isEnabledItemInfo() ? " data-tooltip data-tooltip-url=\"" . resource_to("/item.php?hex=" . $it->getHex()) . "\"" : "") . "\">";
            if ($this->getItemUrl()) {
                $url = $this->_parseMacros($this->getItemUrl(), $slot, $it);
                $html .= "<a data-ajax href=\"" . url_to($url) . "\">";
            }
            $html .= $this->_renderItemImage($it);
            if ($this->getItemUrl()) {
                $html .= "</a>";
            }
            $html .= "</div>";
        }
        $html .= "</div>";
        $starty = 0;
        $slices = $this->getInventory()->getHeight();
        if (1 < $this->getInventory()->getSlice()) {
            $slices = $this->getInventory()->getHeight() / $this->getInventory()->getSlice();
            $starty = $slices * ($slice - 1);
        }
        $html .= "<div class=\"morpheus-inventory-items\">";
        for ($y = 0; $y < $this->getInventory()->getHeight(); $y++) {
            for ($x = 0; $x < $this->getInventory()->getWidth(); $x++) {
                $posy = $y + $starty;
                $i = $this->getInventory()->getItem($x, $posy);
                if ($i === null || !util("Item")->hasDisplayImage($i)) {
                    continue;
                }
                $item_width = $i->getWidth();
                $item_height = $i->getHeight();
                if ($this->getInventory()->getDbVersion() >= 5 && ($item_width > 8 || $item_height > 8)) {
                    $item_width = 1;
                    $item_height = 1;
                }

                $slot = $this->getInventory()->getSlotByCord($x, $y) + (1 < $slice ? $slices * $this->getInventory()->getWidth() : 0);
                if ($this->getActiveSlot()) {
                    $active = substr($this->getActiveSlot(), 0, 1) != "e" && $slot == $this->getActiveSlot();
                } else {
                    $active = $this->getActiveItem() && $this->getActiveItem()->getSerial() === $i->getSerial();
                }
                $html .= "<div class=\"item" . ($active ? " active" : "") . "\" style=\"height:" . $item_height * 32 . "px;width:" . $item_width * 32 . "px;top:" . $y * 32 . "px;left:" . $x * 32 . "px\"" . ($this->isEnabledItemInfo() ? " data-tooltip data-tooltip-url=\"" . resource_to("/item.php?hex=" . $i->getHex()) . "\"" : "") . "\">";
                if ($this->getItemUrl()) {
                    $url = $this->_parseMacros($this->getItemUrl(), $slot, $i);
                    $html .= "<a data-ajax href=\"" . url_to($url) . "\">";
                }
                $html .= "<div class=\"image\">";
                $html .= $this->_renderItemImage($i);
                $html .= "</div>";
                if ($this->getItemUrl()) {
                    $html .= "</a>";
                }
                $html .= "</div>";
            }
        }
        $html .= "</div>";
        $html .= "</div>";
        return $html;
    }
    public function renderAll()
    {
        $html = "";
        for ($i = 1; $i <= $this->getInventory()->getSlice(); $i++) {
            $html .= $this->render($i);
        }
        return $html;
    }

    /**
     * Itens sem gif (cosméticos / visuais) para listar ao lado do inventário.
     * @return array[] each: name, place, equipped, hex, item
     */
    public function getVisualItems()
    {
        $list = array();
        $equipCount = $this->getInventory()->getDbVersion() >= 5 ? 15 : 12;
        for ($i = 0; $i < $equipCount; $i++) {
            $it = $this->getInventory()->getEquipment($i);
            if ($it === null || util("Item")->hasItemImage($it)) {
                continue;
            }
            if (!util("Item")->hasDisplayImage($it)) {
                continue;
            }
            $list[] = array(
                "name" => $this->_itemDisplayName($it),
                "place" => isset(self::$_equipSlotNames[$i]) ? self::$_equipSlotNames[$i] : ("Slot " . $i),
                "equipped" => true,
                "hex" => $it->getHex(),
                "item" => $it,
                "image" => util("Item")->getDisplayImage($it, 48),
            );
        }

        $width = $this->getInventory()->getWidth();
        $height = $this->getInventory()->getHeight();
        for ($y = 0; $y < $height; $y++) {
            for ($x = 0; $x < $width; $x++) {
                $it = $this->getInventory()->getItem($x, $y);
                if ($it === null || util("Item")->hasItemImage($it)) {
                    continue;
                }
                if (!util("Item")->hasDisplayImage($it)) {
                    continue;
                }
                $list[] = array(
                    "name" => $this->_itemDisplayName($it),
                    "place" => "Inventário",
                    "equipped" => false,
                    "hex" => $it->getHex(),
                    "item" => $it,
                    "image" => util("Item")->getDisplayImage($it, 48),
                );
            }
        }
        return $list;
    }

    public function renderVisualList()
    {
        $items = $this->getVisualItems();
        if (empty($items)) {
            return "";
        }
        $html = "<div class=\"inventory-visual-list\">";
        $html .= "<div class=\"inventory-visual-list-title\">Itens visuais</div>";
        $html .= "<ul>";
        foreach ($items as $row) {
            $badge = $row["equipped"] ? "Equipado" : "Baú";
            $badgeClass = $row["equipped"] ? "eq" : "bag";
            $html .= "<li" . ($this->isEnabledItemInfo() ? " data-tooltip data-tooltip-url=\"" . resource_to("/item.php?hex=" . $row["hex"]) . "\"" : "") . ">";
            $html .= "<span class=\"ivl-icon\"><img src=\"" . htmlspecialchars($row["image"]) . "\" alt=\"\"></span>";
            $html .= "<span class=\"ivl-body\">";
            $html .= "<span class=\"ivl-badge " . $badgeClass . "\">" . $badge . "</span>";
            $html .= "<span class=\"ivl-place\">" . htmlspecialchars($row["place"]) . "</span>";
            $html .= "<span class=\"ivl-name\">" . htmlspecialchars($row["name"]) . "</span>";
            $html .= "</span>";
            $html .= "</li>";
        }
        $html .= "</ul></div>";
        return $html;
    }

    private function _itemDisplayName(\Morpheus\Item\Item $item)
    {
        return util("Item")->getDisplayName($item, true);
    }

    private function _renderItemImage(\Morpheus\Item\Item $item)
    {
        $image = util("Item")->getDisplayImage($item, max(32, $item->getWidth() * 32));
        $ix = $item->getWidth() * 32;
        $iy = $item->getHeight() * 32;
        $docRoot = env("DOCUMENT_ROOT");
        if ($docRoot && $image && strpos($image, ".php") === false) {
            $local = $docRoot . $image;
            if (is_file($local)) {
                $size = @getimagesize($local);
                if ($size) {
                    $ix = $size[0];
                    $iy = $size[1];
                }
            }
        }
        return "<img style=\"position:absolute;top:50%;left:50%;margin-left:-" . ($ix / 2) . "px;margin-top:-" . ($iy / 2) . "px;\" src=\"" . htmlspecialchars($image) . "\" alt=\"\" />";
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
