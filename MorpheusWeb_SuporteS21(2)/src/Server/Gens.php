<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Server;

class Gens
{
    private $_family = NULL;
    const DUPRIAN = 1;
    const VANERT = 2;
    public function __construct($family = NULL)
    {
        $this->_family = $family;
    }
    public function getFamily()
    {
        return $this->_family;
    }
    public function setFamily($family)
    {
        $this->_family = $family;
    }
    public function getName()
    {
        return \Morpheus\Util\Gens::familyName($this->_family);
    }
    public function __toString()
    {
        return $this->getName();
    }
}

?>