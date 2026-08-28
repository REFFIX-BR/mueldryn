<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Guild;

class Guild
{
    private $_name = NULL;
    private $_mark = NULL;
    private $_score = NULL;
    private $_master = NULL;
    private $_members = NULL;
    private $_data = array();
    public function __construct($name = NULL)
    {
        if ($name !== null) {
            $this->read($name);
        }
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
    public function setMark($mark)
    {
        $this->_mark = $mark;
        return $this;
    }
    public function getMark()
    {
        return $this->_mark;
    }
    public function setMaster($master)
    {
        if ($master instanceof \Morpheus\Character\Character) {
            $this->_master = $master;
        } else {
            $this->_master = new \Morpheus\Character\Character($master);
        }
        return $this;
    }
    public function getMaster()
    {
        return $this->_master;
    }
    public function setScore($score)
    {
        $this->_score = $score;
        return $this;
    }
    public function getScore()
    {
        return $this->_score;
    }
    public function getMembers()
    {
        if ($this->_members === null) {
            $members = \Morpheus\Database\DriverManager::getConnection()->fetchAll("SELECT\n                Name as name\n                FROM GuildMember gm\n                WHERE G_Name = ?\n                ORDER BY G_Level DESC\n            ", array($this->getName()));
            $this->_members = array();
            foreach ($members as $member) {
                $this->_members[] = new Member($member["name"]);
            }
        }
        return $this->_members;
    }
    public function exists()
    {
        return $this->getName() !== null;
    }
    public function read($name)
    {
        $result = \Morpheus\Database\DriverManager::getConnection()->fetchAssoc("SELECT\n            g.G_Name as name,\n            g.G_Mark as mark,\n            g.G_Score as score,\n            g.G_Master as master\n            FROM Guild g\n            WHERE g.G_Name = ?\n        ", array($name));
        if (!empty($result)) {
            $this->_data = $result;
            attach($this, $result);
        }
    }
    public function count($where = NULL)
    {
        switch ($where) {
            case "new":
                $where = "";
                break;
            default:
                $where = $where !== null ? " WHERE " . $where : "";
        }
        $total = \Morpheus\Database\DriverManager::getConnection()->fetchColumn("SELECT\n            COUNT(1) AS total FROM Guild\n        " . $where);
        return $total;
    }
}

?>