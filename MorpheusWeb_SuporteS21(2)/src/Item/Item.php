<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Item
{
    private $_hex = NULL;
    private $_section = NULL;
    private $_index = NULL;
    private $_unique = false;
    private $_name = NULL;
    private $_level = 0;
    private $_option = 0;
    private $_luck = false;
    private $_skill = false;
    private $_durability = NULL;
    private $_excellents = array(false, false, false, false, false, false);
    private $_serial = NULL;
    private $_ancient = NULL;
    private $_refine = false;
    private $_sockets = array();
    private $_harmony = NULL;
    private $_size = array();
    private $_dbVersion = 3;
    private $_definitions = NULL;
    private static $_storage = NULL;
    public function __construct($hex = NULL, $dbVersion = NULL)
    {
        if ($hex !== null) {
            $this->setHex($hex);
        }
        $this->setDbVersion($dbVersion === null ? config("item.db_version", 3) : $dbVersion);
        $this->_serial = new Serial($this);
        $this->_harmony = new Harmony($this);
        $this->_ancient = new Ancient($this);
    }
    public function setHex($hex)
    {
        $this->_hex = $hex;
        return $this;
    }
    public function getHex()
    {
        return $this->_hex;
    }
    public function setSection($section)
    {
        if ($this->getDbVersion() < 2 && 7 < $section) {
            $this->_unique = true;
        }
        $this->_section = $section;
        return $this;
    }
    public function getSection()
    {
        return $this->_section;
    }
    public function setIndex($index)
    {
        if (255 < $index) {
            $this->_unique = true;
        }
        $this->_index = $index;
        return $this;
    }
    public function getIndex()
    {
        return $this->_index;
    }
    public function setName($name)
    {
        $this->_name = $name;
        return $this;
    }
    public function getName()
    {
        return $this->_name;
    }
    public function setLevel($level)
    {
        $this->_level = $level;
        return $this;
    }
    public function addLevel($level = 1)
    {
        $this->_level += $level;
        return $this;
    }
    public function getLevel()
    {
        return $this->_level;
    }
    public function setOption($option)
    {
        $this->_option = $option;
        return $this;
    }
    public function addOption($option = 4)
    {
        $this->_option += $option;
        return $this;
    }
    public function getOption()
    {
        return $this->_option;
    }
    public function setLuck($luck)
    {
        $this->_luck = $luck;
        return $this;
    }
    public function addLuck()
    {
        return $this->setLuck(true);
    }
    public function getLuck()
    {
        return $this->_luck;
    }
    public function hasLuck()
    {
        return $this->getLuck();
    }
    public function setSkill($skill)
    {
        $this->_skill = $skill;
        return $this;
    }
    public function addSkill()
    {
        return $this->setSkill(true);
    }
    public function getSkill()
    {
        return $this->_skill;
    }
    public function hasSkill()
    {
        return $this->getSkill();
    }
    public function setDurability($durability)
    {
        $this->_durability = $durability;
        return $this;
    }
    public function getDurability()
    {
        return $this->_durability;
    }
    public function addExcellent($position, $excellent)
    {
        $this->_excellents[$position] = $excellent;
        return $this;
    }
    public function getExcellent($position)
    {
        return $this->_excellents[$position];
    }
    public function setExcellents(array $excellents)
    {
        $this->_excellents = $excellents;
        return $this;
    }
    public function getExcellents()
    {
        return $this->_excellents;
    }
    public function setUnique($unique)
    {
        $this->_unique = $unique;
        return $this;
    }
    public function isUnique()
    {
        return $this->_unique;
    }
    public function isExcellent()
    {
        return 0 < count(array_filter($this->_excellents, function ($el) {
            return $el;
        }));
    }
    public function setSerial($serial)
    {
        $this->getSerial()->set($serial);
        return $this;
    }
    public function getSerial()
    {
        return $this->_serial;
    }
    public function setSize($x, $y = NULL)
    {
        if (is_array($x)) {
            $this->_size = $x;
        } else {
            $this->_size = array("x" => $x, "y" => $y);
        }
        return $this;
    }
    public function getSize($position = NULL)
    {
        if ($position === null) {
            return $this->_size;
        }
        return $this->_size[$position];
    }
    public function getWidth()
    {
        return $this->_size["x"];
    }
    public function getHeight()
    {
        return $this->_size["y"];
    }
    public function getNoSocket()
    {
        return 255;
    }
    public function getEmptySocket()
    {
        return 254;
    }
    public function getAncient()
    {
        return $this->_ancient;
    }
    public function setAncient($ancient)
    {
        $this->_ancient->set($ancient);
        return $this;
    }
    public function isAncient()
    {
        return $this->_ancient->has();
    }
    public function setRefine($refine)
    {
        $this->_refine = $refine;
        return $this;
    }
    public function addRefine()
    {
        return $this->setRefine(true);
    }
    public function getRefine()
    {
        return $this->_refine;
    }
    public function isRefine()
    {
        return $this->getRefine();
    }
    public function getHarmony()
    {
        return $this->_harmony;
    }
    public function addHarmony($type, $level)
    {
        $this->getHarmony()->setType($type)->setLevel($level);
        return $this;
    }
    public function hasSocket()
    {
        return 0 < count(array_filter($this->_sockets, function ($socket) {
            return $socket->has() && !$socket->isEmpty();
        }));
    }
    public function addSocket($position, $socket)
    {
        if (!isset($this->_sockets[$position])) {
            $this->_sockets[$position] = new Socket($this);
        }
        $this->_sockets[$position]->set($socket);
        return $this;
    }
    public function getSocket($position)
    {
        if (isset($this->_sockets[$position])) {
            return $this->_sockets[$position];
        }
        return null;
    }
    public function setSockets(array $sockets)
    {
        foreach ($sockets as $position => $sock) {
            $this->addSocket($position, $sock);
        }
        return $this;
    }
    public function getSockets()
    {
        return $this->_sockets;
    }
    public function setDbVersion($dbVersion)
    {
        $this->_dbVersion = $dbVersion;
        return $this;
    }
    public function getDbVersion()
    {
        return $this->_dbVersion;
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
    public function repair()
    {
        $durability = $this->getDefinitions()->getDurability();
        $this->setDurability($durability);
    }
    public static function setStorage(Storage\AbstractStorage $storage)
    {
        static::$_storage = $storage;
    }
    public static function getStorage()
    {
        if (static::$_storage === null) {
            static::$_storage = Storage\Storage::factory("Serialize", ROOT . DS . "resources" . DS . "files" . DS . "items.mw");
        }
        return static::$_storage;
    }
    public function isHexEmpty()
    {
        $hex = strtoupper((string)$this->getHex());
        if ($hex === '') {
            return true;
        }
        return $hex === str_repeat("F", strlen($hex)) || $hex === str_repeat("0", strlen($hex));
    }
    public function getDefinitions()
    {
        if ($this->_definitions === null) {
            $this->_definitions = new Definition($this);
        }
        return $this->_definitions;
    }
    public function getDefaults()
    {
        try {
            return static::getStorage()->getItem($this->getSection(), $this->getIndex());
        } catch (\Exception $e) {
            $name = "Unknown Item";
            if (class_exists("\\Morpheus\\OpenMU\\InventorySync")) {
                $openMuName = \Morpheus\OpenMU\InventorySync::lookupName($this->getSection(), $this->getIndex());
                if ($openMuName) {
                    $name = $openMuName;
                }
            }
            return array("name" => $name, "width" => 1, "height" => 1);
        }
    }
    public function update()
    {
        if (!static::getStorage()) {
            throw new \RuntimeException("Storage class not found");
        }
        $item = $this->getDefaults();
        if (class_exists("\\Morpheus\\OpenMU\\Bridge") && \Morpheus\OpenMU\Bridge::enabled()) {
            $muName = \Morpheus\OpenMU\Bridge::itemDefinitionName($this->getSection(), $this->getIndex());
            if ($muName !== "") {
                $item["name"] = $muName;
            }
        }
        if ($item["name"] === "" || strcasecmp($item["name"], "Unknown Item") === 0) {
            if (class_exists("\\Morpheus\\OpenMU\\InventorySync")) {
                $openMuName = \Morpheus\OpenMU\InventorySync::lookupName($this->getSection(), $this->getIndex());
                if ($openMuName) {
                    $item["name"] = $openMuName;
                }
            }
        }
        $this->setName($item["name"])->setSize($item["width"], $item["height"]);
    }
    public function parse()
    {
        if ($this->isHexEmpty()) {
            return NULL;
        }
        $parse = new ParseHex($this->getHex(), $this->getDbVersion());
        $this->setIndex($parse->getIndex())->setSection($parse->getSection())->setUnique($parse->getUnique())->setLevel($parse->getLevel())->setOption($parse->getOption())->setSkill($parse->getSkill())->setLuck($parse->getLuck())->setDurability($parse->getDurability())->setExcellents($parse->getExcellents())->setSerial($parse->getSerial())->setSockets($parse->getSockets())->addHarmony($parse->getHarmonyType(), $parse->getHarmonyLevel())->setAncient($parse->getAncient())->setRefine($parse->getRefine())->update();
        return $this;
    }
    public function generate()
    {
        $generate = new GenerateHex($this);
        $hex = $generate->generate();
        $this->setHex($hex);
        return $hex;
    }
    public function toArray()
    {
        $item = array();
        if (!$this->isHexEmpty()) {
            $item = array("section" => $this->getSection(), "index" => $this->getIndex(), "name" => $this->getName(), "unique" => $this->isUnique(), "level" => $this->getLevel(), "option" => $this->getOption(), "skill" => $this->getSkill(), "luck" => $this->getLuck(), "durability" => $this->getDurability(), "excellents" => $this->getExcellents(), "serial" => $this->getSerial(), "ancient" => $this->getAncient()->get(), "refine" => $this->getRefine(), "sockets" => $this->getSockets(), "harmony" => array("type" => $this->getHarmony()->getType(), "level" => $this->getHarmony()->getLevel()));
        }
        return $item;
    }
}

?>