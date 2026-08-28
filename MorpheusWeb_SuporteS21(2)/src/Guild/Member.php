<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Guild;

class Member extends \Morpheus\Character\Character
{
    private $_memberLevel = NULL;
    public function __construct($name)
    {
        $this->_read($name);
    }
    public function setMemberLevel($level)
    {
        $this->_memberLevel = $level;
        return $this;
    }
    public function getMemberLevel()
    {
        return $this->_memberLevel;
    }
    public function isMaster()
    {
        return $this->getGuild() !== null && $this->getName() === $this->getGuild()->getMaster()->getName();
    }
    private function _read($name)
    {
        parent::read($name);
        $result = \Morpheus\Database\DriverManager::getConnection()->fetchAssoc("SELECT\n            gm.G_Level as memberLevel\n            FROM GuildMember gm\n            WHERE gm.Name = ?\n        ", array($name));
        if (!empty($result)) {
            attach($this, $result);
        }
    }
}

?>