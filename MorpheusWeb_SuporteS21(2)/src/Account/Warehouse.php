<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Account;

class Warehouse extends \Morpheus\Item\Matrix
{
    private $_account = NULL;
    private $_money = NULL;
    private $_schema = NULL;
    private $_renderer = NULL;
    public function __construct(Account $account, $width = NULL, $height = NULL, $slice = 1)
    {
        parent::__construct($width, $height, $slice);
        $this->setAccount($account);
    }
    public function load($hex = NULL)
    {
        if ($hex === null) {
            $hex = bintohex($this->getAccount()->getBinWarehouse());
        }
        $this->_money = \Morpheus\Database\Connection::fetchColumn("select Money \n            from Warehouse \n            where AccountID = ?\n        ", array($this->getAccount()->getUsername()));
        parent::load($hex);
    }
    public function getAccount()
    {
        return $this->_account;
    }
    public function setAccount(Account $account)
    {
        $this->_account = $account;
        return $this;
    }
    public function getMoney()
    {
        return $this->_money;
    }
    public function addMoney($money)
    {
        $this->_money += $money;
        return $this;
    }
    public function setMoney($money)
    {
        $this->_money = $money;
        return $this;
    }
    public function getSchema()
    {
        if ($this->_schema === null) {
            $columns = \Morpheus\Database\Connection::fetchAll("select *\n                from INFORMATION_SCHEMA.COLUMNS\n                where TABLE_NAME = ?", array("Warehouse"));
            foreach ($columns as $column) {
                $this->_schema[$column["COLUMN_NAME"]] = $column;
            }
        }
        return $this->_schema;
    }
    public function update($items = true)
    {
        $hex = $this->generate();
        \Morpheus\Database\Connection::executeUpdate("update warehouse\n            set\n            Money = :money\n            " . ($items ? ", Items = 0x" . $hex : "") . "\n            where AccountId = :account\n        ", array("money" => $this->getMoney(), "account" => $this->getAccount()->getUsername()));
    }
    public function create()
    {
        $warehouse = \Morpheus\Database\Connection::fetchAssoc("select 1 \n            from Warehouse \n            where AccountID = ?\n        ", array($this->getAccount()->getUsername()));
        if (empty($warehouse)) {
            $size = $this->getWidth() * $this->getHeight() * $this->getItemSize();
            $empty = str_repeat("F", $size);
            \Morpheus\Database\Connection::executeUpdate("insert into warehouse \n                (Items, AccountID, EndUseDate" . (isset($this->getSchema()["DbVersion"]) ? ",DbVersion" : "") . ") \n                values \n                (0x" . $empty . ", ?, getdate()" . (isset($this->getSchema()["DbVersion"]) ? "," . (int) $this->getDbVersion() : "") . ")\n            ", array($this->getAccount()->getUsername()));
            $this->getAccount()->read();
        }
    }
    public function clear($items = true, $zen = true)
    {
        $size = $this->getWidth() * $this->getHeight() * $this->getItemSize();
        $empty = str_repeat("F", $size);
        $set = array();
        if ($items) {
            $set[] = "Items = 0x" . $empty;
        }
        if ($zen) {
            $set[] = "Money = 0";
        }
        if (!empty($set)) {
            \Morpheus\Database\Connection::executeUpdate("update warehouse\n                set\n                " . join(",", $set) . "\n                where AccountId = :account\n            ", array("account" => $this->getAccount()->getUsername()));
        }
    }
    public function getRenderer()
    {
        if ($this->_renderer === null) {
            $this->_renderer = new Warehouse\Renderer($this);
        }
        return $this->_renderer;
    }
}

?>