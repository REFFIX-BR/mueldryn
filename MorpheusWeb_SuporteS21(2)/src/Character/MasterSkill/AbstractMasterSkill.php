<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character\MasterSkill;

abstract class AbstractMasterSkill implements \Morpheus\Character\MasterSkill
{
    private $_character = NULL;
    public function __construct(\Morpheus\Character\Character $character = NULL)
    {
        $this->_character = $character;
    }
    public function getCharacter()
    {
        return $this->_character;
    }
    public function setCharacter(\Morpheus\Character\Character $character)
    {
        $this->_character = $character;
        return $this;
    }
    public function changeTo($name)
    {
    }
    public function exists()
    {
        return false;
    }
}

?>