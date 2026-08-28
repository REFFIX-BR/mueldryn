<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character\MasterSkill;

class XTeam extends AbstractMasterSkill
{
    public function reset($resetPoints = true, $resetLevel = true, $multiplier = 1)
    {
        if ($this->exists()) {
            $this->_removeSkillsEffect();
            $master = \Morpheus\Database\Connection::fetchAssoc("select MasterSkill as skills\r\n                , MasterLevel as level\r\n                , MasterExperience as exp\r\n                from MasterSkillTree\r\n                where Name = ?\r\n            ", array($this->getCharacter()->getName()));
            $skills = str_split(bintohex($master["skills"]), 6);
            foreach ($skills as $key => $skill) {
                $skills[$key] = "FF0000";
            }
            $points = $master["level"] * $multiplier;
            if (!$resetPoints) {
                $points = 0;
            }
            $level = $master["level"];
            $exp = $master["exp"];
            if ($resetLevel) {
                $level = 0;
                $exp = 0;
            }
            \Morpheus\Database\Connection::executeUpdate("update MasterSkillTree set \r\n                MasterLevel = " . $level . ",\r\n                MasterPoint = " . $points . ",\r\n                MasterExperience = " . $exp . ",\r\n                MasterSkill = 0x" . implode("", $skills) . "\r\n                where Name = ?\r\n            ", array($this->getCharacter()->getName()));
        }
    }
    public function changeTo($name)
    {
        if ($this->exists()) {
            \Morpheus\Database\Connection::update("MasterSkillTree", array("Name" => $name), array("Name" => $this->getCharacter()->getName()));
        }
    }
    public function exists()
    {
        $tree = \Morpheus\Database\Connection::fetchAll("select *\r\n            from INFORMATION_SCHEMA.COLUMNS\r\n            where TABLE_NAME = ?\r\n        ", array("MasterSkillTree"));
        return !empty($tree);
    }
    private function _removeSkillsEffect()
    {
        $skills = str_split(bintohex($this->getCharacter()->getMagicList()), 6);
        foreach ($skills as $key => $skill) {
            $skills[$key] = "FF0000";
        }
        \Morpheus\Database\Connection::executeUpdate("update Character\r\n            set MagicList = 0x" . implode("", $skills) . "\r\n            where Name = ?\r\n        ", array($this->getCharacter()->getName()));
    }
}

?>