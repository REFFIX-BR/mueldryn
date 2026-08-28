<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Definition
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
    public function getDurability()
    {
        $defaults = $this->getItem()->getDefaults();
        if (!isset($defaults["durability"])) {
            return 0;
        }
        $durability = $defaults["durability"];
        if ($this->getItem()->getSection() == 5) {
            $durability = $defaults["magic"]["durability"];
        }
        $level = $this->getItem()->getLevel();
        if ($level <= 4) {
            $durability = $durability + $level * 1;
        } else {
            if ($level < 10 && 4 < $level) {
                $durability = $durability + ($level - 2) * 2;
            } else {
                if ($level == 10) {
                    $durability = $durability + $level * 2 - 3;
                } else {
                    if ($level == 11) {
                        $durability = $durability + $level * 2 - 1;
                    } else {
                        if ($level == 12) {
                            $durability = $durability + ($level + 1) * 2;
                        } else {
                            if ($level == 13) {
                                $durability = $durability + ($level + 3) * 2;
                            } else {
                                if ($level == 14) {
                                    $durability = $durability + $level * 2 + 11;
                                } else {
                                    if ($level == 15) {
                                        $durability = $durability + $level * 2 + 17;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        if (0 < $this->getItem()->isExcellent() && $this->getItem()->getAncient()->has()) {
            $durability += 20;
        } else {
            if (!$this->getItem()->isExcellent() && $this->getItem()->getAncient()->has()) {
                $durability += 20;
            } else {
                if ($this->getItem()->isExcellent() && !$this->getItem()->getAncient()->has()) {
                    $durability += 15;
                }
            }
        }
        return 255 < $durability ? 255 : $durability;
    }
    public function hasDurability()
    {
        $defaults = $this->getItem()->getDefaults();
        return isset($defaults["durability"]) && ($this->getItem()->getSection() == 5 ? 0 < $defaults["magic"]["durability"] : 0 < $defaults["durability"]);
    }
    public function getExcellentOptions()
    {
        try {
            return \Morpheus\Util\Item::getExcellentOptions($this->getItem()->getSection(), $this->getItem()->getIndex());
        } catch (\Exception $e) {
            return array();
        }
    }
    public function getExcellentOption($index)
    {
        $names = $this->getExcellentOptions();
        if (isset($names[$index])) {
            return $names[$index];
        }
        return "";
    }
    public function hasExcellentOptions()
    {
        return $this->getExcellentOptions();
    }
    public function hasRefine()
    {
        return \Morpheus\Util\Item::hasRefine($this->getItem()->getSection(), $this->getItem()->getIndex());
    }
    public function hasSkill()
    {
        return \Morpheus\Util\Item::hasSkill($this->getItem()->getSection(), $this->getItem()->getIndex());
    }
    public function hasAncient()
    {
        return (new Ancient($this->getItem()))->exists();
    }
    public function hasHarmony()
    {
        $harmony = (new Harmony($this->getItem()))->exists();
        if (\Morpheus\Util\Team::is(array("xteam", "muemu"))) {
            return $harmony && !$this->getItem()->hasSocket();
        }
        return $harmony;
    }
    public function hasSockets()
    {
        $socket = (new Socket($this->getItem()))->exists();
        if (\Morpheus\Util\Team::is(array("xteam", "muemu"))) {
            return $socket && !$this->getItem()->getHarmony()->has();
        }
        return $socket;
    }
    public function getSockets()
    {
        return (new Socket($this->getItem()))->available();
    }
    public function getMaxSockets()
    {
        return (new Socket($this->getItem()))->max();
    }
    public function hasLuck()
    {
        if ($this->getItem()->getSection() <= 11) {
            return true;
        }
        $defaults = $this->getItem()->getDefaults();
        if (isset($defaults["slot"]) && $defaults["slot"] == 7) {
            return true;
        }
        return false;
    }
    public function getRefineName($slot = 1)
    {
        return \Morpheus\Util\Item::getRefineName($this->getItem(), $slot);
    }
    public function getOptionText()
    {
        return \Morpheus\Util\Item::getOptionText($this->getItem());
    }
    public function getLuckText()
    {
        return "Luck (success rate of Jewel of Soul +25%)<br />Luck (critical damage rate +5%)";
    }
    public function getRefineText()
    {
        $option1 = $this->getRefineName();
        $option2 = $this->getRefineName(2);
        return $option1 . (!empty($option2) ? "<br />" . $option2 : "");
    }
    public function getAncientText()
    {
        return \Morpheus\Util\Item::getAncientText($this->getItem()->getSection(), $this->getItem()->getIndex(), $this->getItem()->getAncient()->get());
    }
    public function canEquip()
    {
        return \Morpheus\Util\Item::canEquip($this->getItem()->getSection(), $this->getItem()->getIndex());
    }
    public function canEquipTexts()
    {
        $for = $this->canEquip();
        if (empty($for)) {
            return array();
        }
        $defaults = $this->getItem()->getDefaults();
        $all = true;
        if (isset($defaults["class"])) {
            foreach ($defaults["class"] as $class => $val) {
                if ($val !== null && $val == 0) {
                    $all = false;
                    break;
                }
            }
        }
        $text = array();
        if (!$all) {
            foreach ($for as $class) {
                $text[] = "Can be equipped by " . util("Character")->classNameByShotName($class);
            }
        }
        return $text;
    }
}

?>