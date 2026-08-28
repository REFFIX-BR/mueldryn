<?php

namespace Morpheus\OpenMU;

/**
 * Converte inventário OpenMU (tabelas data.Item) para o blob hex do Character.Inventory (Morpheus).
 */
class InventorySync
{
    /** Option type GUIDs from OpenMU ItemOptionTypes */
    const TYPE_EXCELLENT = '6487c498-58e0-48e5-b409-35d7598313fc';
    const TYPE_WING = '55cb57a7-4fc6-47bb-9fee-84e6c4ebce95';
    const TYPE_LUCK = '3e3e9be8-4e16-4f27-a7cf-986d48454d76';
    const TYPE_OPTION = 'f193f91e-86d7-4456-add8-a3667e731303';
    const TYPE_HARMONY = '0ca234f0-4a0f-4fa1-8e07-cfb89c1ec94f';
    const TYPE_ANCIENT_BONUS = '5e2c10ef-e580-48d5-a48b-0ffcd0678966';
    const TYPE_GUARDIAN = '4aa95715-1ed3-453d-8d1d-093b281416ca';
    const TYPE_SOCKET = 'aab309d3-cd97-4f77-ae1b-e9f904102502';
    const TYPE_SOCKET_BONUS = '43da2c68-d6e1-4b94-adb1-8864d92f8fb9';

    /**
     * Sincroniza o inventário de um personagem OpenMU para o MSSQL Character.Inventory.
     *
     * @param string $characterName
     * @return int Quantidade de itens convertidos
     */
    public static function syncCharacter($characterName)
    {
        $pdo = Bridge::pdo();
        $items = self::fetchItems($pdo, $characterName);
        $hex = self::buildInventoryHex($items);
        self::writeToMssql($characterName, $hex);
        return count($items);
    }

    /**
     * @param \PDO $pdo
     * @param string $name
     * @return array
     */
    public static function fetchItems($pdo, $name)
    {
        $st = $pdo->prepare(
            'SELECT i."Id", i."ItemSlot", i."Level", i."Durability", i."HasSkill", i."SocketCount",
                    d."Group" AS item_group, d."Number" AS item_number, d."Name" AS item_name
             FROM data."Character" c
             JOIN data."Item" i ON i."ItemStorageId" = c."InventoryId"
             JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
             WHERE c."Name" = :name
             ORDER BY i."ItemSlot"'
        );
        $st->execute(array('name' => $name));
        $rows = $st->fetchAll();
        if (!$rows) {
            return array();
        }

        $ids = array();
        foreach ($rows as $r) {
            $ids[] = $r['Id'];
        }

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

    /**
     * @param \PDO $pdo
     * @param array $itemIds
     * @return array
     */
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

    /**
     * @param \PDO $pdo
     * @param array $itemIds
     * @return array
     */
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
            // primeiro discriminator encontrado
            if (!isset($map[$row['ItemId']])) {
                $map[$row['ItemId']] = (int) $row['AncientSetDiscriminator'];
            }
        }
        return $map;
    }

    /**
     * @param array $items
     * @return string hex uppercase
     */
    public static function buildInventoryHex(array $items)
    {
        $itemSize = self::itemHexSize(); // 32 for db_v3
        $byteLen = self::inventoryByteLength();
        $maxSlots = (int) floor($byteLen / ($itemSize / 2));
        if ($maxSlots < 76) {
            $maxSlots = 76;
        }

        $slots = array_fill(0, $maxSlots, str_repeat('F', $itemSize));

        foreach ($items as $row) {
            $slot = (int) $row['ItemSlot'];
            if ($slot < 0 || $slot >= $maxSlots) {
                continue;
            }
            $hex = self::itemToHex($row);
            if ($hex !== null && strlen($hex) === $itemSize) {
                $slots[$slot] = $hex;
                self::rememberName((int) $row['item_group'], (int) $row['item_number'], $row['item_name']);
            }
        }

        return strtoupper(implode('', $slots));
    }

    /**
     * @param array $row
     * @return string|null
     */
    public static function itemToHex(array $row)
    {
        $section = (int) $row['item_group'];
        $index = (int) $row['item_number'];
        if ($section < 0 || $index < 0) {
            return null;
        }

        $item = new \Morpheus\Item\Item();
        $item->setDbVersion((int) config('item.db_version', 3));
        $item->setSection($section);
        $item->setIndex($index);
        $item->setLevel((int) $row['Level']);
        $item->setDurability(max(0, min(255, (int) floor((float) $row['Durability']))));
        $item->setSkill(!empty($row['HasSkill']) && ($row['HasSkill'] === true || $row['HasSkill'] === 't' || $row['HasSkill'] === '1' || $row['HasSkill'] === 1));
        $item->setSerial(strtoupper(substr(md5($row['Id']), 0, 8)));

        $excellents = array(false, false, false, false, false, false);
        $luck = false;
        $option = 0;
        $refine = false;
        $harmonyType = 0;
        $harmonyLevel = 0;
        $sockets = array();

        foreach ($row['options'] as $opt) {
            $typeId = strtolower(str_replace(array('{', '}'), '', $opt['type_id']));
            $optNumber = (int) $opt['opt_number'];
            $linkLevel = (int) $opt['link_level'];
            $linkIndex = (int) $opt['link_index'];

            if ($typeId === self::TYPE_LUCK) {
                $luck = true;
            } elseif ($typeId === self::TYPE_OPTION) {
                // OpenMU Level 1..7 → option +4..+28
                $option = max($option, $linkLevel * 4);
            } elseif ($typeId === self::TYPE_EXCELLENT || $typeId === self::TYPE_WING) {
                if ($optNumber >= 1 && $optNumber <= 6) {
                    $excellents[$optNumber - 1] = true;
                }
            } elseif ($typeId === self::TYPE_GUARDIAN) {
                $refine = true;
            } elseif ($typeId === self::TYPE_HARMONY) {
                $harmonyType = $optNumber;
                $harmonyLevel = $linkLevel;
            } elseif ($typeId === self::TYPE_SOCKET || $typeId === self::TYPE_SOCKET_BONUS) {
                // sockets: keep simple — empty sockets from SocketCount, filled if Number present
                if ($typeId === self::TYPE_SOCKET) {
                    $sockets[$linkIndex] = $optNumber;
                }
            }
        }

        $item->setLuck($luck);
        $item->setOption($option);
        $item->setExcellents($excellents);
        $item->setRefine($refine);
        if ($harmonyType > 0) {
            $item->addHarmony($harmonyType, $harmonyLevel);
        }

        $socketCount = (int) $row['SocketCount'];
        for ($i = 0; $i < 5; $i++) {
            if (isset($sockets[$i])) {
                $item->addSocket($i, $sockets[$i]);
            } elseif ($i < $socketCount) {
                $item->addSocket($i, $item->getEmptySocket()); // FE
            } else {
                $item->addSocket($i, $item->getNoSocket()); // FF
            }
        }

        if (!empty($row['ancient'])) {
            $disc = (int) $row['ancient'];
            // Morpheus: 1 => 05, 2 => 0A
            $item->setAncient($disc >= 2 ? 2 : 1);
        }

        // isUnique bit for index >= 256
        if ($index >= 256) {
            $item->setUnique(true);
        }

        return (new \Morpheus\Item\GenerateHex($item))->generate();
    }

    private static function itemHexSize()
    {
        $db = (int) config('item.db_version', 3);
        if ($db <= 2) {
            return 20;
        }
        if ($db === 3) {
            return 32;
        }
        if ($db >= 5) {
            return 50;
        }
        return 32;
    }

    private static function inventoryByteLength()
    {
        // Tamanho típico deste MuOnline (já usado no Bridge)
        return 3984;
    }

    /**
     * Guarda nomes OpenMU para o tooltip quando o catálogo Morpheus não tem o item.
     */
    private static function rememberName($section, $index, $name)
    {
        if ($name === null || $name === '') {
            return;
        }
        $dir = defined('CACHE_PATH') ? CACHE_PATH : (ROOT . DS . 'tmp' . DS . 'cache' . DS);
        if (!is_dir($dir)) {
            @mkdir($dir, 0777, true);
        }
        $file = $dir . 'openmu-item-names.php';
        static $map = null;
        if ($map === null) {
            $map = is_file($file) ? (include $file) : array();
            if (!is_array($map)) {
                $map = array();
            }
        }
        $key = $section . '-' . $index;
        if (!isset($map[$key]) || $map[$key] !== $name) {
            $map[$key] = $name;
            $export = "<?php\nreturn " . var_export($map, true) . ";\n";
            @file_put_contents($file, $export, LOCK_EX);
        }
    }

    public static function lookupName($section, $index)
    {
        static $map = null;
        if ($map === null) {
            $file = (defined('CACHE_PATH') ? CACHE_PATH : (ROOT . DS . 'tmp' . DS . 'cache' . DS)) . 'openmu-item-names.php';
            $map = is_file($file) ? (include $file) : array();
            if (!is_array($map)) {
                $map = array();
            }
        }
        $key = ((int) $section) . '-' . ((int) $index);
        return isset($map[$key]) ? $map[$key] : null;
    }

    /**
     * @param string $characterName
     * @param string $hex
     */
    private static function writeToMssql($characterName, $hex)
    {
        $conn = \Morpheus\Database\DriverManager::getConnection();
        $hex = strtoupper(preg_replace('/[^0-9A-Fa-f]/', '', $hex));
        if (strlen($hex) % 2 === 1) {
            $hex .= '0';
        }
        // Literal 0x... evita problemas do driver com varbinary parametrizado
        $conn->executeUpdate(
            'UPDATE Character SET Inventory = 0x' . $hex . ' WHERE Name = ?',
            array($characterName)
        );
    }
}
