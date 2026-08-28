<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Ancient
{
    private $_value = 0;
    private $_item = NULL;
    public function __construct(Item $item = NULL, $value = 0)
    {
        $this->_item = $item;
        $this->_value = $value;
    }
    public function get()
    {
        return $this->_value;
    }
    public function set($value)
    {
        $this->_value = (int) $value;
    }
    public function getItem()
    {
        return $this->_item;
    }
    public function setItem(Item $item)
    {
        $this->_item = $item;
    }
    public function has()
    {
        return 0 < $this->_value;
    }
    public function is($value)
    {
        return $this->_value === (int) $value;
    }
    public function exists($section = NULL, $index = NULL)
    {
        $available = $this->available($section, $index);
        return !empty($available);
    }
    public function getName()
    {
        $available = $this->available();
        return isset($available[$this->get()]) ? $available[$this->get()] : "";
    }
    public function toText()
    {
        $section = $this->getItem()->getSection();
        $index = $this->getItem()->getIndex();
        $text = "";
        if ($this->has()) {
            if (in_array($section, array(6, 7, 8, 9, 10, 11, 13)) || $section == 13 && in_array($index, array(8, 9, 21, 22, 23))) {
                $text = "Increase Stamina +" . $this->get() * 5;
            } else {
                if (in_array($section, array(0, 1, 2, 3, 4, 5)) || $section == 13 && in_array($index, array(12, 13, 25, 26, 27))) {
                    $text = "Increase Strength +" . $this->get() * 5;
                }
            }
        }
        return $text;
    }
    public function available($section = NULL, $index = NULL)
    {
        $ancients = array();
        if ($section === null) {
            $section = $this->getItem()->getSection();
            $index = $this->getItem()->getIndex();
        }
        $all = $this->all();
        if (isset($all[$section][$index])) {
            foreach ($all[$section][$index] as $code => $ancient) {
                $ancients[$code] = $ancient["name"];
            }
        }
        return $ancients;
    }
    public function all()
    {
        if (\Morpheus\Util\Team::is("igcn")) {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetType.xml";
            $file2 = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetOption.xml";
            if (!file_exists($file)) {
                throw new \Morpheus\Exception\FileNotFoundException($file);
            }
            if (!file_exists($file2)) {
                throw new \Morpheus\Exception\FileNotFoundException($file2);
            }
            $xml = new \DOMDocument();
            $xml->load($file);
            $data = array();
            foreach ($xml->getElementsByTagName("Section") as $section) {
                foreach ($section->getElementsByTagName("Item") as $i) {
                    $xml2 = new \DOMDocument();
                    $xml2->load($file2);
                    foreach ($xml2->getElementsByTagName("SetItem") as $si) {
                        if (in_array($si->getAttribute("Index"), array($i->getAttribute("TierI"), $i->getAttribute("TierII")))) {
                            $ix = $si->getAttribute("Index") == $i->getAttribute("TierI") ? 1 : 2;
                            if (!isset($data[$section->getAttribute("Index")][$i->getAttribute("Index")])) {
                                $data[$section->getAttribute("Index")][$i->getAttribute("Index")] = array();
                            }
                            $data[$section->getAttribute("Index")][$i->getAttribute("Index")][$ix] = array("index" => (int) $ix, "name" => $si->getAttribute("Name"));
                        }
                    }
                }
            }
            return $data;
        } else {
            $data = array();
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemType.txt", "r"))) {
                throw new \Morpheus\Exception\FileNotFoundException($file);
            }
            while (!feof($file)) {
                $types = fscanf($file, "%d %d %d %d %d");
                if (isset($types[0]) && !strpos($types[0], "//")) {
                    if (!($file2 = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemOption.txt", "r"))) {
                        throw new \Morpheus\Exception\FileNotFoundException($file2);
                    }
                    while (!feof($file2)) {
                        $infos = fscanf($file2, "%d \"%[^\"]\" %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d");
                        if (isset($infos[0]) && !strpos($infos[0], "//") && in_array($infos[0], array($types[3], $types[4]))) {
                            $ix = $infos[0] == $types[3] ? 1 : 2;
                            if (!isset($data[$types[0]][$types[1]])) {
                                $data[$types[0]][$types[1]] = array();
                            }
                            $data[$types[0]][$types[1]][$ix] = array("index" => (int) $ix, "name" => $infos[1]);
                        }
                    }
                }
            }
            fclose($file);
            if (isset($file2)) {
                fclose($file2);
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