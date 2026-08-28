<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Character;

interface MasterSkill
{
    public function getCharacter();
    public function setCharacter(Character $character);
    public function reset($resetPoints, $resetLevel, $multiplier);
    public function changeTo($name);
    public function exists();
}

?>