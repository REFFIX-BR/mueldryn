<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character;

class Inventory extends \Morpheus\Item\Matrix
{
    private $_character = NULL;
    private $_equipments = array();
    private $_renderer = NULL;
    public function __construct(Character $character, $width, $height, $slice = 1)
    {
        $this->setCharacter($character);
        parent::__construct($width, $height, $slice);
    }
    public function setCharacter(Character $character)
    {
        $this->_character = $character;
        return $this;
    }
    public function getCharacter()
    {
        return $this->_character;
    }
    public function load($hex = NULL)
    {
        if ($hex === null) {
            $hex = bintohex($this->getCharacter()->getBinInventory());
        }
        $equipCount = 12;
        $elenght = $this->getItemSize() * 12;
        $items = str_split($hex, $this->getItemSize());
        for ($i = 0; $i < $equipCount; $i++) {
            if (isset($items[$i])) {
                $item = new \Morpheus\Item\Item($items[$i], $this->getDbVersion());
                if ($item->isHexEmpty()) {
                    $this->_equipments[$i] = null;
                } else {
                    $this->_equipments[$i] = $item->parse();
                }
            } else {
                $this->_equipments[$i] = null;
            }
        }

        // Leitura correta do Pentagrama e Brincos S20 (Slots 236, 237 e 238 da DB)
        if ($this->getDbVersion() >= 5) {
            $this->_equipments[12] = isset($items[236]) && trim($items[236], 'F0') !== '' ? (new \Morpheus\Item\Item($items[236]))->parse() : null;
            $this->_equipments[13] = isset($items[237]) && trim($items[237], 'F0') !== '' ? (new \Morpheus\Item\Item($items[237]))->parse() : null;
            $this->_equipments[14] = isset($items[238]) && trim($items[238], 'F0') !== '' ? (new \Morpheus\Item\Item($items[238]))->parse() : null;
        }

        $hex = substr($hex, $elenght);
        $length = $this->getWidth() * $this->getHeight() * $this->getItemSize();
        $hex = substr($hex, 0, $length);
        parent::load($hex);
    }
    public function getEquipment($i)
    {
        return isset($this->_equipments[$i]) ? $this->_equipments[$i] : null;
    }
    public function getEquipments()
    {
        return $this->_equipments;
    }
    public function hasItemsEquipped()
    {
        $has = false;
        foreach ($this->getEquipments() as $equip) {
            if ($equip !== null) {
                $has = true;
                break;
            }
        }
        return $has;
    }
    public function getItemBySlot($slot)
    {
        if (substr($slot, 0, 1) === "e") {
            foreach ($this->_equipments as $index => $item) {
                if ($index == substr($slot, 1)) {
                    return $item;
                }
            }
        }
        return parent::getItemBySlot($slot);
    }
    public function getItemBySerial($serial)
    {
        foreach ($this->_equipments as $item) {
            if ($item !== null && $item->getSerial() === $serial) {
                return $item;
            }
        }
        return parent::getItemBySerial($serial);
    }
    public function clear($items = true, $zen = true)
    {
        $size = $this->getCharacter()->getSchema()["Inventory"]["CHARACTER_MAXIMUM_LENGTH"];
        $empty = str_repeat("F", $size);
        $set = array();
        if ($items) {
            $set[] = "Inventory = 0x" . $empty;
        }
        if ($zen) {
            $set[] = "Money = 0";
        }
        if (!empty($set)) {
            \Morpheus\Database\Connection::executeUpdate("update Character\n                set\n                " . join(",", $set) . "\n                where Name = ?\n            ", array($this->getCharacter()->getName()));
        }
    }
    public function update()
    {
        $hex = "";
        for ($i = 0; $i < 12; $i++) {
            $item = isset($this->_equipments[$i]) ? $this->_equipments[$i] : null;
            if ($item === null) {
                $hex .= str_repeat("F", $this->getItemSize());
            } else {
                $hex .= $item->generate();
            }
        }
        $hex .= $this->generate();
        $original = bintohex($this->getCharacter()->getBinInventory());
        $hex = $hex . substr($original, strlen($hex));

        // Injeta os slots S20 (236, 237 e 238) de volta nas devidas posicoes da string na hora de salvar
        if ($this->getDbVersion() >= 5) {
            $pentagramHex = isset($this->_equipments[12]) && $this->_equipments[12] !== null ? $this->_equipments[12]->generate() : str_repeat("F", $this->getItemSize());
            $earringLHex  = isset($this->_equipments[13]) && $this->_equipments[13] !== null ? $this->_equipments[13]->generate() : str_repeat("F", $this->getItemSize());
            $earringRHex  = isset($this->_equipments[14]) && $this->_equipments[14] !== null ? $this->_equipments[14]->generate() : str_repeat("F", $this->getItemSize());
            $extraHex = $pentagramHex . $earringLHex . $earringRHex;
            $injectPos = 236 * $this->getItemSize();
            if (strlen($hex) >= $injectPos + strlen($extraHex)) {
                $hex = substr_replace($hex, $extraHex, $injectPos, strlen($extraHex));
            }
        }

        \Morpheus\Database\Connection::executeUpdate("update Character set\n            Inventory = 0x" . $hex . "\n            where Name = :name\n        ", array("name" => $this->getCharacter()->getName()));
    }
    public function getWeapon($left = true)
    {
        if ($left) {
            return $this->_equipments[0];
        }
        return $this->_equipments[1];
    }
    public function getLeftWeapon()
    {
        return $this->getWeapon();
    }
    public function getRightWeapon()
    {
        return $this->getWeapon(false);
    }
    public function getHelm()
    {
        return $this->_equipments[2];
    }
    public function getArmor()
    {
        return $this->_equipments[3];
    }
    public function getPants()
    {
        return $this->_equipments[4];
    }
    public function getGloves()
    {
        return $this->_equipments[5];
    }
    public function getBoots()
    {
        return $this->_equipments[6];
    }
    public function getWing()
    {
        return $this->_equipments[7];
    }
    public function getGuardian()
    {
        return $this->_equipments[8];
    }
    public function getPendant()
    {
        return $this->_equipments[9];
    }
    public function getRing($left = true)
    {
        if ($left) {
            return $this->_equipments[10];
        }
        return $this->_equipments[11];
    }
    public function getLeftRing()
    {
        return $this->getRing();
    }
    public function getRightRing()
    {
        return $this->getRing(false);
    }
    public function getRenderer()
    {
        if ($this->_renderer === null) {
            $this->_renderer = new Inventory\Renderer($this);
        }
        return $this->_renderer;
    }
}

?>