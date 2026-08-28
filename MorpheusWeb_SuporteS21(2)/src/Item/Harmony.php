<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Harmony
{
    private $_type = 0;
    private $_level = 0;
    private $_item = NULL;
    public function __construct(Item $item = NULL, $type = 0, $level = 0)
    {
        $this->_item = $item;
        $this->_type = $type;
        $this->_level = $level;
    }
    public function getType()
    {
        return $this->_type;
    }
    public function setType($type)
    {
        $this->_type = (int) $type;
        return $this;
    }
    public function getLevel()
    {
        return $this->_level;
    }
    public function setLevel($level)
    {
        $this->_level = (int) $level;
        return $this;
    }
    public function getItem()
    {
        return $this->_item;
    }
    public function setItem(Item $item)
    {
        $this->_item = $item;
    }
    public function exists($section = NULL)
    {
        if ($section === null) {
            $section = $this->getItem()->getSection();
        }
        return $section <= 11;
    }
    public function has()
    {
        return !in_array($this->getType(), array(0, 15));
    }
    public function getName()
    {
        $name = "";
        if (!$this->has()) {
            return $name;
        }
        $harmonys = $this->available();
        foreach ($harmonys as $harmony) {
            foreach ($harmony["levels"] as $level) {
                if ($this->getType() === $harmony["index"] && $this->getLevel() === $level["level"]) {
                    $name = sprintf($harmony["name"], $level["name"]);
                    break 2;
                }
            }
        }
        return $name;
    }
    public function available($section = NULL)
    {
        if ($section === null) {
            $section = $this->getItem()->getSection();
        }
        if ($section < 5) {
            $hsection = 1;
        } else {
            if ($section == 5) {
                $hsection = 2;
            } else {
                $hsection = 3;
            }
        }
        if (\Morpheus\Util\Team::is("igcn")) {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_HarmonyItem_Option.xml";
            if (!file_exists($file)) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            $xml = new \DOMDocument();
            $xml->load($file);
            $data = array();
            foreach ($xml->getElementsByTagName("Type") as $type) {
                if ($type->getAttribute("ID") == $hsection) {
                    foreach ($type->getElementsByTagName("Option") as $option) {
                        $levels = array();
                        $index = 0;
                        foreach ($option->getElementsByTagName("Effect") as $effect) {
                            $value = $effect->getAttribute("Value" . $index);
                            if ($value != 0) {
                                $levels[] = array("level" => (int) $index, "name" => (int) $value);
                            }
                            $index++;
                        }
                        $data[] = array("index" => (int) $option->getAttribute("Index"), "name" => $option->getAttribute("Name"), "levels" => $levels);
                    }
                }
            }
            return $data;
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "JewelOfHarmonyOption.txt", "rb+"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            $category = -1;
            $data = array();
            while (!feof($file)) {
                $line = fgets($file);
                $line = trim($line, " \t\r\n");
                if (substr($line, 0, 2) == "//" || substr($line, 0, 2) == "#" || $line == "") {
                    continue;
                }
                if (($pos = strpos($line, "//")) !== false) {
                    $line = substr($line, 0, $pos);
                }
                $line = trim($line, " \t\r\n");
                if ($category == -1) {
                    if (is_numeric($line)) {
                        $category = $line;
                    }
                } else {
                    if (strtolower($line) == "end") {
                        $category = -1;
                        continue;
                    }
                    if ($category == $hsection) {
                        $columns = preg_split("/[\\s,]*\\\"([^\\\"]+)\\\"[\\s,]*|[\\s,]*'([^']+)'[\\s,]*|[\\s,]+/", $line, 0, PREG_SPLIT_NO_EMPTY | PREG_SPLIT_DELIM_CAPTURE);
                        $levels = array();
                        $index = 0;
                        $i = 3;
                        while ($i <= 29) {
                            if ($columns[$i] != 0) {
                                $levels[] = array("level" => (int) $index, "name" => (int) $columns[$i]);
                            }
                            $index++;
                            $i += 2;
                        }
                        $data[] = array("index" => (int) $columns[0], "name" => $columns[1] . " +%d", "levels" => $levels);
                    } else {
                        continue;
                    }
                }
            }
            return $data;
        }
    }
    public function __toString()
    {
        return $this->getName();
    }
}

?>