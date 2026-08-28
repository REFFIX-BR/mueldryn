<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item\Storage;

class IGCN extends AbstractStorage
{
    protected function parse()
    {
        $xml = new \DOMDocument();
        $xml->load($this->getFile());
        $this->_items = array();
        foreach ($xml->getElementsByTagName("Section") as $section) {
            foreach ($section->getElementsByTagName("Item") as $item) {
                $data = array("type" => $item->getAttribute("Type"), "name" => $item->getAttribute("Name"), "width" => $item->getAttribute("Width"), "height" => $item->getAttribute("Height"), "id" => $item->getAttribute("Index"), "skill" => $item->getAttribute("SkillIndex"), "slot" => $item->getAttribute("Slot"), "durability" => $item->getAttribute("Durability"));
                if (0 <= $section->getAttribute("Index") && $section->getAttribute("Index") <= 5) {
                    $data += array("damage" => array("min" => $item->getAttribute("DamageMin"), "max" => $item->getAttribute("DamageMax")), "attack_speed" => $item->getAttribute("AttackSpeed"), "magic" => array("durability" => $item->getAttribute("MagicDurability"), "damage" => $item->getAttribute("MagicPower")), "requirement" => array("level" => $item->getAttribute("ReqLevel"), "strength" => $item->getAttribute("ReqStrength"), "dexterity" => $item->getAttribute("ReqDexterity"), "vitality" => $item->getAttribute("ReqVitality"), "energy" => $item->getAttribute("ReqEnergy"), "command" => $item->getAttribute("ReqCommand")), "class" => array("dk" => $item->getAttribute("DarkKnight"), "dw" => $item->getAttribute("DarkWizard"), "fe" => $item->getAttribute("FairyElf"), "mg" => $item->getAttribute("MagicGladiator"), "dl" => $item->getAttribute("DarkLord"), "sm" => $item->getAttribute("Summoner"), "rf" => $item->getAttribute("RageFighter"), "gl" => $item->getAttribute("GrowLancer")));
                } else {
                    if (5 < $section->getAttribute("Index") && $section->getAttribute("Index") <= 11) {
                        $data += array("defense" => $item->getAttribute("Defense"), "class" => array("dk" => $item->getAttribute("DarkKnight"), "dw" => $item->getAttribute("DarkWizard"), "fe" => $item->getAttribute("FairyElf"), "mg" => $item->getAttribute("MagicGladiator"), "dl" => $item->getAttribute("DarkLord"), "sm" => $item->getAttribute("Summoner"), "rf" => $item->getAttribute("RageFighter"), "gl" => $item->getAttribute("GrowLancer")));
                        switch ($section->getAttribute("Index")) {
                            case 6:
                                $data += array("defense_success" => $item->getAttribute("SuccessfulBlocking"));
                                break;
                            case 7:
                                $data += array("magic_defense" => $item->getAttribute("MagicDefense"));
                                break;
                            case 8:
                                $data += array("magic_defense" => $item->getAttribute("MagicDefense"));
                                break;
                            case 9:
                                $data += array("magic_defense" => $item->getAttribute("MagicDefense"));
                                break;
                            case 10:
                                $data += array("attack_speed" => $item->getAttribute("AttackSpeed"));
                                break;
                            case 11:
                                $data += array("walk_speed" => $item->getAttribute("WalkSpeed"));
                                break;
                        }
                    } else {
                        if ($section->getAttribute("Index") == 12) {
                            $data += array("defense" => $item->getAttribute("Defense"), "requirement" => array("level" => $item->getAttribute("ReqLevel"), "strength" => $item->getAttribute("ReqStrength"), "dexterity" => $item->getAttribute("ReqDexterity"), "vitality" => $item->getAttribute("ReqVitality"), "energy" => $item->getAttribute("ReqEnergy"), "command" => $item->getAttribute("ReqCommand")), "class" => array("dk" => $item->getAttribute("DarkKnight"), "dw" => $item->getAttribute("DarkWizard"), "fe" => $item->getAttribute("FairyElf"), "mg" => $item->getAttribute("MagicGladiator"), "dl" => $item->getAttribute("DarkLord"), "sm" => $item->getAttribute("Summoner"), "rf" => $item->getAttribute("RageFighter"), "gl" => $item->getAttribute("GrowLancer")));
                        } else {
                            if ($section->getAttribute("Index") == 13) {
                                $data += array("resistance" => array("ice" => $item->getAttribute("IceRes"), "poison" => $item->getAttribute("PoisonRes"), "light" => $item->getAttribute("LightRes"), "fire" => $item->getAttribute("FireRes"), "earth" => $item->getAttribute("EarthRes"), "wind" => $item->getAttribute("WindRes"), "water" => $item->getAttribute("WaterRes")), "class" => array("dk" => $item->getAttribute("DarkKnight"), "dw" => $item->getAttribute("DarkWizard"), "fe" => $item->getAttribute("FairyElf"), "mg" => $item->getAttribute("MagicGladiator"), "dl" => $item->getAttribute("DarkLord"), "sm" => $item->getAttribute("Summoner"), "rf" => $item->getAttribute("RageFighter"), "gl" => $item->getAttribute("GrowLancer")));
                            } else {
                                if ($section->getAttribute("Index") == 15) {
                                    $data += array("requirement" => array("level" => $item->getAttribute("ReqLevel"), "energy" => $item->getAttribute("ReqEnergy")), "class" => array("dk" => $item->getAttribute("DarkKnight"), "dw" => $item->getAttribute("DarkWizard"), "fe" => $item->getAttribute("FairyElf"), "mg" => $item->getAttribute("MagicGladiator"), "dl" => $item->getAttribute("DarkLord"), "sm" => $item->getAttribute("Summoner"), "rf" => $item->getAttribute("RageFighter"), "gl" => $item->getAttribute("GrowLancer")));
                                }
                            }
                        }
                    }
                }
                $this->_items[$section->getAttribute("Index")][$item->getAttribute("Index")] = $data;
            }
        }
        return $this->_items;
    }
}

?>