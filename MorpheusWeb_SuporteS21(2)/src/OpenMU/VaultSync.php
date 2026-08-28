<?php

namespace Morpheus\OpenMU;

/**
 * Espelha o vault (baú) OpenMU → warehouse MSSQL do Morpheus.
 */
class VaultSync
{
    /**
     * @param string $login
     * @return int itens convertidos
     */
    public static function syncAccount($login)
    {
        if (!Bridge::enabled()) {
            return 0;
        }
        $acc = Bridge::findAccount($login);
        if (!$acc) {
            return 0;
        }

        $items = self::fetchVaultItems($acc['Id']);
        $hex = self::buildWarehouseHex($items);
        self::writeWarehouse($login, $hex);
        return count($items);
    }

    /**
     * @param string $accountId UUID
     * @return array
     */
    public static function fetchVaultItems($accountId)
    {
        $pdo = Bridge::pdo();
        $st = $pdo->prepare(
            'SELECT i."Id", i."ItemSlot", i."Level", i."Durability", i."HasSkill", i."SocketCount",
                    d."Group" AS item_group, d."Number" AS item_number, d."Name" AS item_name
             FROM data."Account" a
             JOIN data."Item" i ON i."ItemStorageId" = a."VaultId"
             JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
             WHERE a."Id" = :aid
             ORDER BY i."ItemSlot"'
        );
        $st->execute(array('aid' => $accountId));
        $rows = $st->fetchAll();
        if (!$rows) {
            return array();
        }

        $ids = array();
        foreach ($rows as $r) {
            $ids[] = $r['Id'];
        }

        // Reusa lógica privada via reflexão leve: duplica fetch com queries iguais ao InventorySync
        $optionsByItem = self::fetchOptions($pdo, $ids);
        $ancientByItem = self::fetchAncient($pdo, $ids);

        foreach ($rows as &$row) {
            $id = $row['Id'];
            $row['options'] = isset($optionsByItem[$id]) ? $optionsByItem[$id] : array();
            $row['ancient'] = isset($ancientByItem[$id]) ? $ancientByItem[$id] : null;
        }
        unset($row);

        return $rows;
    }

    private static function fetchOptions($pdo, array $itemIds)
    {
        if (empty($itemIds)) {
            return array();
        }
        $placeholders = implode(',', array_fill(0, count($itemIds), '?'));
        $sql = 'SELECT ol."ItemId", ol."Level" AS link_level, ol."Index" AS link_index,
                       io."Number" AS opt_number, lower(iot."Id"::text) AS type_id, iot."Name" AS type_name
                FROM data."ItemOptionLink" ol
                JOIN config."IncreasableItemOption" io ON io."Id" = ol."ItemOptionId"
                JOIN config."ItemOptionType" iot ON iot."Id" = io."OptionTypeId"
                WHERE ol."ItemId" IN (' . $placeholders . ')';
        $st = $pdo->prepare($sql);
        $st->execute(array_values($itemIds));
        $map = array();
        foreach ($st->fetchAll() as $opt) {
            $map[$opt['ItemId']][] = $opt;
        }
        return $map;
    }

    private static function fetchAncient($pdo, array $itemIds)
    {
        if (empty($itemIds)) {
            return array();
        }
        $placeholders = implode(',', array_fill(0, count($itemIds), '?'));
        $sql = 'SELECT j."ItemId", ois."AncientSetDiscriminator"
                FROM data."ItemItemOfItemSet" j
                JOIN config."ItemOfItemSet" ois ON ois."Id" = j."ItemOfItemSetId"
                WHERE j."ItemId" IN (' . $placeholders . ')
                  AND ois."AncientSetDiscriminator" <> 0';
        $st = $pdo->prepare($sql);
        $st->execute(array_values($itemIds));
        $map = array();
        foreach ($st->fetchAll() as $row) {
            if (!isset($map[$row['ItemId']])) {
                $map[$row['ItemId']] = (int) $row['AncientSetDiscriminator'];
            }
        }
        return $map;
    }

    /**
     * Warehouse padrão 8x15 = 120 slots (db v3 = 32 hex / slot).
     */
    public static function buildWarehouseHex(array $items)
    {
        $itemSize = 32;
        $db = (int) config('item.db_version', 3);
        if ($db <= 2) {
            $itemSize = 20;
        } elseif ($db >= 5) {
            $itemSize = 50;
        }

        $width = 8;
        $height = 15;
        if (config('warehouse.extended', false)) {
            // Morpheus usa slice; mantém 15*2 se extendido ativo no account load — aqui sync base 15
        }
        $maxSlots = $width * $height; // 120
        // Vault OpenMU pode ir além; se IsVaultExtended, aumenta
        $maxSeen = 0;
        foreach ($items as $row) {
            $maxSeen = max($maxSeen, (int) $row['ItemSlot']);
        }
        if ($maxSeen >= $maxSlots) {
            $maxSlots = (int) (floor($maxSeen / $width) + 1) * $width;
            if ($maxSlots > 240) {
                $maxSlots = 240; // 8x30
            }
        }

        $slots = array_fill(0, $maxSlots, str_repeat('F', $itemSize));
        foreach ($items as $row) {
            $slot = (int) $row['ItemSlot'];
            if ($slot < 0 || $slot >= $maxSlots) {
                continue;
            }
            try {
                $hex = InventorySync::itemToHex($row);
                if ($hex !== null && strlen($hex) === $itemSize) {
                    $slots[$slot] = $hex;
                }
            } catch (\Exception $e) {
                error_log('VaultSync itemToHex slot ' . $slot . ': ' . $e->getMessage());
            }
        }
        return strtoupper(implode('', $slots));
    }

    private static function writeWarehouse($login, $hex)
    {
        $conn = \Morpheus\Database\DriverManager::getConnection();
        $hex = strtoupper(preg_replace('/[^0-9A-Fa-f]/', '', $hex));
        if (strlen($hex) % 2 === 1) {
            $hex .= '0';
        }

        $exists = $conn->fetchColumn('SELECT COUNT(1) FROM warehouse WHERE AccountID = ?', array($login));
        if (!(int) $exists) {
            try {
                $conn->executeUpdate(
                    'INSERT INTO warehouse (AccountID, Money, EndUseDate, DbVersion, Items) VALUES (?, 0, GETDATE(), 3, 0x' . $hex . ')',
                    array($login)
                );
            } catch (\Exception $e) {
                $conn->executeUpdate(
                    'INSERT INTO warehouse (AccountID, Money, Items) VALUES (?, 0, 0x' . $hex . ')',
                    array($login)
                );
            }
        } else {
            $conn->executeUpdate(
                'UPDATE warehouse SET Items = 0x' . $hex . ' WHERE AccountID = ?',
                array($login)
            );
        }
    }

    /**
     * Lista itens do vault por login (para UI do market).
     * @return array
     */
    public static function listForLogin($login)
    {
        $acc = Bridge::findAccount($login);
        if (!$acc) {
            return array();
        }
        $rows = self::fetchVaultItems($acc['Id']);
        $out = array();
        foreach ($rows as $row) {
            $hex = null;
            try {
                $hex = InventorySync::itemToHex($row);
            } catch (\Exception $e) {
                $hex = null;
            }
            $section = (int) $row['item_group'];
            $index = (int) $row['item_number'];
            if ($hex) {
                try {
                    $it = (new \Morpheus\Item\Item($hex))->parse();
                } catch (\Exception $e) {
                    $it = new \Morpheus\Item\Item($hex);
                }
            } else {
                $it = new \Morpheus\Item\Item();
            }
            $it->setSection($section)->setIndex($index);
            if (!empty($row['item_name'])) {
                $it->setName($row['item_name']);
            }
            $out[] = \Morpheus\Util\Item::bankItemView($it, array(
                'id' => $row['Id'],
                'slot' => (int) $row['ItemSlot'],
                'durability' => (int) floor((float) $row['Durability']),
                'skill' => !empty($row['HasSkill']),
            ), 48, isset($row['item_name']) ? $row['item_name'] : null);
        }
        return $out;
    }

    /**
     * Remove item do vault pelo Id OpenMU.
     */
    public static function removeById($login, $itemId)
    {
        $acc = Bridge::findAccount($login);
        if (!$acc) {
            return false;
        }
        $pdo = Bridge::pdo();
        $st = $pdo->prepare(
            'SELECT i."Id" FROM data."Item" i
             JOIN data."Account" a ON a."VaultId" = i."ItemStorageId"
             WHERE a."Id" = :aid AND i."Id" = :iid
             LIMIT 1'
        );
        $st->execute(array('aid' => $acc['Id'], 'iid' => $itemId));
        $id = $st->fetchColumn();
        if (!$id) {
            return false;
        }
        return self::deleteItemCascade($pdo, $id);
    }

    /**
     * Busca 1 item do vault (com options) pelo Id.
     */
    public static function getVaultItem($login, $itemId)
    {
        $acc = Bridge::findAccount($login);
        if (!$acc) {
            return null;
        }
        $rows = self::fetchVaultItems($acc['Id']);
        foreach ($rows as $row) {
            if ($row['Id'] === $itemId || strcasecmp($row['Id'], $itemId) === 0) {
                return $row;
            }
        }
        return null;
    }

    /**
     * Remove item do vault OpenMU pelo slot (ao anunciar no market).
     */
    public static function removeBySlot($login, $slot)
    {
        $acc = Bridge::findAccount($login);
        if (!$acc) {
            return false;
        }
        $pdo = Bridge::pdo();
        $st = $pdo->prepare(
            'SELECT i."Id" FROM data."Item" i
             JOIN data."Account" a ON a."VaultId" = i."ItemStorageId"
             WHERE a."Id" = :aid AND i."ItemSlot" = :slot
             LIMIT 1'
        );
        $st->execute(array('aid' => $acc['Id'], 'slot' => (int) $slot));
        $id = $st->fetchColumn();
        if (!$id) {
            return false;
        }
        return self::deleteItemCascade($pdo, $id);
    }

    private static function deleteItemCascade($pdo, $itemId)
    {
        $pdo->prepare('DELETE FROM data."ItemOptionLink" WHERE "ItemId" = ?')->execute(array($itemId));
        try {
            $pdo->prepare('DELETE FROM data."ItemItemOfItemSet" WHERE "ItemId" = ?')->execute(array($itemId));
        } catch (\Exception $e) {
        }
        $pdo->prepare('DELETE FROM data."Item" WHERE "Id" = ?')->execute(array($itemId));
        return true;
    }

    /**
     * Cria item básico no vault OpenMU a partir de um Item Morpheus (compra).
     */
    public static function addFromMorpheusItem($login, \Morpheus\Item\Item $item)
    {
        $acc = Bridge::findAccount($login);
        if (!$acc) {
            return false;
        }
        $pdo = Bridge::pdo();
        $vaultSt = $pdo->prepare('SELECT "VaultId" FROM data."Account" WHERE "Id" = :id');
        $vaultSt->execute(array('id' => $acc['Id']));
        $vaultId = $vaultSt->fetchColumn();
        if (!$vaultId) {
            return false;
        }

        $section = (int) $item->getSection();
        $index = (int) $item->getIndex();
        $defId = sprintf('00000080-%04x-%04x-0000-000000000000', $section, $index);

        // Confirma definição
        $chk = $pdo->prepare('SELECT "Id" FROM config."ItemDefinition" WHERE "Id" = :id LIMIT 1');
        $chk->execute(array('id' => $defId));
        $realDef = $chk->fetchColumn();
        if (!$realDef) {
            $chk2 = $pdo->prepare(
                'SELECT "Id" FROM config."ItemDefinition" WHERE "Group" = :g AND "Number" = :n LIMIT 1'
            );
            $chk2->execute(array('g' => $section, 'n' => $index));
            $realDef = $chk2->fetchColumn();
        }
        if (!$realDef) {
            return false;
        }

        $used = $pdo->prepare('SELECT "ItemSlot" FROM data."Item" WHERE "ItemStorageId" = :vid');
        $used->execute(array('vid' => $vaultId));
        $busy = array();
        while ($r = $used->fetch()) {
            $busy[(int) $r['ItemSlot']] = true;
        }
        $slot = null;
        for ($s = 0; $s < 120; $s++) {
            if (!isset($busy[$s])) {
                $slot = $s;
                break;
            }
        }
        if ($slot === null) {
            return false;
        }

        $newId = self::uuid();
        $ins = $pdo->prepare(
            'INSERT INTO data."Item"
            ("Id", "ItemStorageId", "ItemSlot", "DefinitionId", "Durability", "Level", "HasSkill", "SocketCount", "StorePrice", "IsRare")
            VALUES (:id, :vid, :slot, :def, :dur, :lvl, :skill, 0, NULL, false)'
        );
        $ins->bindValue(':id', $newId);
        $ins->bindValue(':vid', $vaultId);
        $ins->bindValue(':slot', $slot, \PDO::PARAM_INT);
        $ins->bindValue(':def', $realDef);
        $ins->bindValue(':dur', max(1, (float) $item->getDurability()));
        $ins->bindValue(':lvl', (int) $item->getLevel(), \PDO::PARAM_INT);
        $ins->bindValue(':skill', $item->hasSkill() ? 't' : 'f');
        $ins->execute();
        return true;
    }

    private static function uuid()
    {
        $data = random_bytes(16);
        $data[6] = chr((ord($data[6]) & 0x0f) | 0x40);
        $data[8] = chr((ord($data[8]) & 0x3f) | 0x80);
        return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($data), 4));
    }
}
