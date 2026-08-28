<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Socket
{
    private $_value = NULL;
    private $_item = NULL;
    private static $_empty = 254;
    private static $_no = 255;
    public function __construct(Item $item = NULL, $value = NULL)
    {
        $this->_item = $item;
        $this->_value = $value;
        if (config("item.socket_system", 0) === 1) {
            static::$_empty = 255;
            static::$_no = 0;
        }
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
    public static function getNoValue()
    {
        return static::$_no;
    }
    public static function getEmptyValue()
    {
        return static::$_empty;
    }
    public function hasValue()
    {
        return $this->_value !== null && $this->_value !== static::getNoValue() && $this->_value !== static::getEmptyValue();
    }
    public function isEmpty()
    {
        return $this->get() === static::getEmptyValue();
    }
    public function has()
    {
        return $this->get() !== null && $this->get() !== static::getNoValue();
    }
    public function exists($section = NULL, $index = NULL)
    {
        if ($section === null) {
            $section = $this->getItem()->getSection();
            $index = $this->getItem()->getIndex();
        }
        $sockets = false;
        if (\Morpheus\Util\Team::is("igcn")) {
            $default = $this->getItem()->getDefaults();
            return isset($default["type"]) && in_array($default["type"], array(2, 4));
        }
        if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SocketItemType.txt", "r"))) {
            throw new \Morpheus\Exception\FileNotFoundException($file);
        }
        while (!feof($file)) {
            $types = fscanf($file, "%d %d %d");
            if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                $sockets = true;
                break;
            }
        }
        fclose($file);
        return $sockets;
    }
    public function getName()
    {
        $sockets = $this->available();
        $name = "";
        foreach ($sockets as $type => $socks) {
            foreach ($socks as $socket) {
                if ($this->get() === $socket["value"]) {
                    $name = $socket["name"];
                    break 2;
                }
            }
        }
        return $name;
    }
    public function max($section = NULL, $index = NULL)
    {
        if ($section === null) {
            $section = $this->getItem()->getSection();
            $index = $this->getItem()->getIndex();
        }
        if (\Morpheus\Util\Team::is("igcn")) {
            return 5;
        }
        $max = 0;
        if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SocketItemType.txt", "r"))) {
            throw new \Morpheus\Exception\FileNotFoundException();
        }
        while (!feof($file)) {
            $types = fscanf($file, "%d %d %d");
            if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                $max = $types[2];
                break;
            }
        }
        fclose($file);
        return $max;
    }
    public function available($section = NULL, $max = 5)
    {
        if ($section === null) {
            $section = $this->getItem()->getSection();
        }
        $data = array();
        if ($section <= 5) {
            $allow = array(1, 3, 5);
        } else {
            $allow = array(2, 4, 6);
        }
        if (\Morpheus\Util\Team::is("igcn")) {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_SocketOption.xml";
            if (!file_exists($file)) {
                throw new \Morpheus\Exception\FileNotFoundException($file);
            }
            $xml = new \DOMDocument();
            $xml->load($file);
            foreach ($xml->getElementsByTagName("SocketItemOptionSettings") as $setting) {
                foreach ($setting->getElementsByTagName("Option") as $option) {
                    for ($i = 0; $i < $max; $i++) {
                        $idx = $option->getAttribute("Index");
                        $value = $idx + $i * 50;
                        $bonus = $option->getAttribute("BonusValue" . ($i + 1));
                        $element = $option->getAttribute("ElementType");
                        if (in_array($element, $allow)) {
                            $type = $this->_getType($element);
                            $complement = "";
                            if (in_array($idx, array(0, 5, 10, 12, 13, 14, 16, 17, 20, 22, 23, 30, 32))) {
                                $complement = "%";
                            }
                            if (!isset($data[$type])) {
                                $data[$type] = array();
                            }
                            $data[$type][] = array("type" => $type, "name" => $option->getAttribute("Name") . " +" . $bonus . $complement, "value" => $value);
                        }
                    }
                }
            }
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SocketItemOption.txt", "rb+"))) {
                throw new \Morpheus\Exception\FileNotFoundException($file);
            }
            $category = -1;
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
                        break;
                    }
                    $columns = preg_split("/[\\s,]*\\\"([^\\\"]+)\\\"[\\s,]*|[\\s,]*'([^']+)'[\\s,]*|[\\s,]+/", $line, 0, PREG_SPLIT_NO_EMPTY | PREG_SPLIT_DELIM_CAPTURE);
                    for ($i = 0; $i < $max; $i++) {
                        $value = $columns[0] + $i * 50;
                        $initial = 4;
                        if (9 < count($columns)) {
                            $initial = 5;
                        }
                        $option = $columns[$i + $initial];
                        if (in_array($columns[1], $allow)) {
                            $type = $this->_getType($columns[1]);
                            $complement = "";
                            if (in_array($columns[0], array(0, 5, 10, 12, 13, 14, 16, 17, 20, 22, 23, 30, 32))) {
                                $complement = "%";
                            }
                            if (!isset($data[$type])) {
                                $data[$type] = array();
                            }
                            $data[$type][] = array("type" => $type, "name" => $columns[3] . " +" . $option . $complement, "value" => $value);
                        }
                    }
                }
            }
        }
        return $data;
    }
    private function _getType($element)
    {
        $type = "-";
        switch ($element) {
            case 1:
                $type = "Fire";
                break;
            case 2:
                $type = "Water";
                break;
            case 3:
                $type = "Ice";
                break;
            case 4:
                $type = "Wind";
                break;
            case 5:
                $type = "Lightning";
                break;
            case 6:
                $type = "Earth";
                break;
        }
        return $type;
    }
    public function __toString()
    {
        return $this->getName();
    }
}

?>