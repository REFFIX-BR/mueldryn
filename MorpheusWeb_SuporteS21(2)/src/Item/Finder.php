<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item;

class Finder
{
    public static function inWarehouse($item)
    {
        $serial = $item;
        if ($item instanceof Item) {
            $serial = $item->getSerial();
        }
        $i = new Item();
        $account = \Morpheus\Database\Connection::fetchColumn("select AccountID \n            from warehouse \n            where (CHARINDEX(0x" . $serial . ", items) %" . $i->getItemSize() / 2 . "=4)\n        ");
        if (!empty($account)) {
            return new \Morpheus\Account\Account($account);
        }
        return null;
    }
    public static function inExtWarehouse($item)
    {
        if (!config("warehouse.ext.active", false)) {
            return null;
        }
        $serial = $item;
        if ($item instanceof Item) {
            $serial = $item->getSerial();
        }
        $i = new Item();
        $account = \Morpheus\Database\Connection::fetchColumn("select " . config("warehouse.ext.column_account") . " \n            from " . config("warehouse.ext.table") . " \n            where (CHARINDEX(0x" . $serial . ", " . config("warehouse.ext.column_items") . ") %" . $i->getItemSize() / 2 . "=4)\n        ");
        if (!empty($account)) {
            return new \Morpheus\Account\Account($account);
        }
        return null;
    }
    public static function inInventory($item)
    {
        $serial = $item;
        if ($item instanceof Item) {
            $serial = $item->getSerial();
        }
        $i = new Item();
        $character = \Morpheus\Database\Connection::fetchColumn("select Name \n            from Character \n            where (CHARINDEX(0x" . $serial . ", Inventory) %" . $i->getItemSize() / 2 . "=4)\n        ");
        if (!empty($character)) {
            return new \Morpheus\Character\Character($character);
        }
        return null;
    }
}

?>