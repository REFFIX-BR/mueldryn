<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character\MasterSkill;

class IGCN extends AbstractMasterSkill
{
    public function reset($resetPoints = true, $resetLevel = true, $multiplier = 1)
    {
        if ($this->exists()) {
            $master = \Morpheus\Database\Connection::fetchAssoc("select mLevel as level\r\n                , mlExperience as exp\r\n                from Character\r\n                where Name = ?\r\n            ", array($this->getCharacter()->getName()));
            $skills = str_split(bintohex($this->getCharacter()->getMagicList()), 6);
            foreach ($skills as $key => $skill) {
                $index = $this->_getSkillIndex($skill);
                if ($this->_isMasterSkill($index)) {
                    $skills[$key] = "FF0000";
                }
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
            \Morpheus\Database\Connection::executeUpdate("update Character set\r\n                mLevel = " . $level . ",\r\n                mlPoint = " . $points . ",\r\n                mlExperience = " . $exp . ",\r\n                MagicList = 0x" . implode("", $skills) . "\r\n                where Name = ?\r\n            ", array($this->getCharacter()->getName()));
        }
    }
    public function exists()
    {
        $tree = \Morpheus\Database\Connection::fetchAll("select *\r\n            from INFORMATION_SCHEMA.COLUMNS\r\n            where TABLE_NAME = ?\r\n            and COLUMN_NAME = ?\r\n        ", array("Character", "mLevel"));
        return empty($tree);
    }
    private function _getSkillIndex($hex)
    {
        $id = hexdec(substr($hex, 0, 2));
        $id2 = hexdec(substr($hex, 2, 2));
        $id3 = hexdec(substr($hex, 4, 2));
        if (0 < ($id2 & 7)) {
            $id = $id * ($id2 & 7) + $id3;
        }
        return $id;
    }
    private function _isMasterSkill($index)
    {
        $is = false;
        $xml = new \DOMDocument();
        $xml->load(ROOT . DS . "resources" . DS . "files" . DS . "SkillList.xml");
        foreach ($xml->getElementsByTagName("Skill") as $skill) {
            if ($skill->getAttribute("Index") === $index) {
                $is = 1 < $skill->getAttribute("UseType");
            }
        }
        return $is;
    }
}

?>