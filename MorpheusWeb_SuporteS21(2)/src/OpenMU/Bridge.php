<?php

namespace Morpheus\OpenMU;

/**
 * Bridge: autentica no Postgres OpenMU (MuMain) e espelha conta/chars no SQL do Morpheus.
 */
class Bridge
{
    /** @var \PDO|null */
    private static $pdo = null;

    public static function enabled()
    {
        return (bool) config('openmu.enabled', true);
    }

    public static function pdo()
    {
        if (self::$pdo instanceof \PDO) {
            return self::$pdo;
        }
        $dsn = config('openmu.dsn', 'pgsql:host=127.0.0.1;port=5433;dbname=openmu');
        $user = config('openmu.user', 'postgres');
        $pass = config('openmu.password', 'admin');
        self::$pdo = new \PDO($dsn, $user, $pass, array(
            \PDO::ATTR_ERRMODE => \PDO::ERRMODE_EXCEPTION,
            \PDO::ATTR_DEFAULT_FETCH_MODE => \PDO::FETCH_ASSOC,
            \PDO::ATTR_TIMEOUT => 5,
        ));
        // connect_timeout no DSN (libpq)
        if (strpos($dsn, 'connect_timeout') === false) {
            // já conectou; ok
        }
        return self::$pdo;
    }

    /**
     * @return array|null Account row with LoginName, PasswordHash, EMail, Id, State
     */
    public static function findAccount($login)
    {
        $st = self::pdo()->prepare('SELECT "Id", "LoginName", "PasswordHash", "EMail", "State", "SecurityCode", "IsVaultExtended"
            FROM data."Account" WHERE lower("LoginName") = lower(:login) LIMIT 1');
        $st->execute(array('login' => $login));
        $row = $st->fetch();
        return $row ? $row : null;
    }

    public static function verifyPassword($login, $password)
    {
        $acc = self::findAccount($login);
        if (!$acc || empty($acc['PasswordHash'])) {
            return false;
        }
        if ((int) $acc['State'] === 1) {
            // banned/blocked typical
            return false;
        }
        return password_verify($password, $acc['PasswordHash']);
    }

    public static function createAccount($login, $password, $email = '')
    {
        $hash = password_hash($password, PASSWORD_BCRYPT);
        $id = self::uuid();
        $st = self::pdo()->prepare('INSERT INTO data."Account"
            ("Id", "LoginName", "PasswordHash", "SecurityCode", "EMail", "RegistrationDate",
             "State", "TimeZone", "VaultPassword", "IsVaultExtended", "IsTemplate", "LanguageIsoCode", "IsBot")
            VALUES
            (:id, :login, :hash, :sec, :email, NOW(), 0, 0, \'\', false, false, \'pt\', false)');
        $st->execute(array(
            'id' => $id,
            'login' => $login,
            'hash' => $hash,
            'sec' => (string) mt_rand(100000, 999999),
            'email' => $email ? $email : ($login . '@mueldryn.local'),
        ));
        return $id;
    }

    public static function accountExists($login)
    {
        return self::findAccount($login) !== null;
    }

    /**
     * Espelha conta + personagens OpenMU no MuOnline (MSSQL) para o Morpheus.
     */
    public static function syncToMorpheus($login)
    {
        $acc = self::findAccount($login);
        if (!$acc) {
            return false;
        }
        $login = $acc['LoginName'];
        $email = $acc['EMail'] ? $acc['EMail'] : ($login . '@mueldryn.local');
        $conn = \Morpheus\Database\DriverManager::getConnection();

        $exists = $conn->fetchColumn(
            'SELECT COUNT(1) FROM ' . \Morpheus\Database\Connection::getTableWithDb('MEMB_INFO') . ' WHERE memb___id = ?',
            array($login)
        );
        if (!(int) $exists) {
            $conn->executeUpdate(
                'INSERT INTO ' . \Morpheus\Database\Connection::getTableWithDb('MEMB_INFO') . '
                (memb___id, memb__pwd, memb_name, sno__numb, post_code, addr_info, addr_deta, tel__numb, phon_numb,
                 mail_addr, fpas_ques, fpas_answ, job__code, appl_days, modi_days, out__days, true_days, mail_chek, bloc_code, ctl1_code,
                 AccountLevel, AccountExpireDate, credit, memb_token)
                VALUES
                (?, ?, ?, ?, 0, 0, 0, 0, 0, ?, ?, ?, 0, GETDATE(), GETDATE(), GETDATE(), GETDATE(), ?, 0, 1, 0, \'1900-01-01\', 0, NULL)',
                array(
                    $login,
                    'openmu', // senha real fica no Postgres
                    substr($login, 0, 10),
                    '0000000',
                    substr($email, 0, 50),
                    'openmu',
                    'openmu',
                    '1',
                )
            );
        } else {
            $conn->executeUpdate(
                'UPDATE ' . \Morpheus\Database\Connection::getTableWithDb('MEMB_INFO') . ' SET mail_addr = ?, mail_chek = \'1\', bloc_code = 0 WHERE memb___id = ?',
                array(substr($email, 0, 50), $login)
            );
        }

        // CashShopData
        $cash = $conn->fetchColumn('SELECT COUNT(1) FROM CashShopData WHERE AccountID = ?', array($login));
        if (!(int) $cash) {
            $conn->executeUpdate(
                'INSERT INTO CashShopData (AccountID, WCoinC, WCoinP, GoblinPoint, Ruud) VALUES (?, 0, 0, 0, 0)',
                array($login)
            );
        }

        // MEMB_STAT (necessário para o perfil / rankings online)
        $statTable = \Morpheus\Database\Connection::getTableWithDb('MEMB_STAT');
        $statExists = $conn->fetchColumn('SELECT COUNT(1) FROM ' . $statTable . ' WHERE memb___id = ?', array($login));
        if (!(int) $statExists) {
            try {
                $conn->executeUpdate(
                    'INSERT INTO ' . $statTable . ' (memb___id, ConnectStat, ServerName, IP, ConnectTM, DisConnectTM, OnlineHours)
                     VALUES (?, 0, \'\', \'127.0.0.1\', GETDATE(), GETDATE(), 0)',
                    array($login)
                );
            } catch (\Exception $e) {
                try {
                    $conn->executeUpdate(
                        'INSERT INTO ' . $statTable . ' (memb___id, ConnectStat) VALUES (?, 0)',
                        array($login)
                    );
                } catch (\Exception $e2) {
                }
            }
        }

        // warehouse
        $wh = $conn->fetchColumn('SELECT COUNT(1) FROM warehouse WHERE AccountID = ?', array($login));
        if (!(int) $wh) {
            try {
                $conn->executeUpdate('INSERT INTO warehouse (AccountID, Money, EndUseDate, DbVersion) VALUES (?, 0, GETDATE(), 3)', array($login));
            } catch (\Exception $e) {
                try {
                    $conn->executeUpdate('INSERT INTO warehouse (AccountID, Money) VALUES (?, 0)', array($login));
                } catch (\Exception $e2) {
                }
            }
        }

        // Espelha vault OpenMU → warehouse MSSQL
        try {
            VaultSync::syncAccount($login);
        } catch (\Exception $e) {
            error_log('OpenMU VaultSync failed for ' . $login . ': ' . $e->getMessage());
        }

        $listed = array();
        try {
            $rows = $conn->fetchAll('SELECT character_name FROM mw_market_chars WHERE seller = ?', array($login));
            foreach ($rows as $r) {
                $listed[strtolower($r['character_name'])] = true;
            }
        } catch (\Exception $e) {
            $listed = array();
        }

        $chars = self::fetchCharacters($acc['Id']);
        $slots = array('', '', '', '', '');
        $i = 0;
        foreach ($chars as $ch) {
            $onMarket = isset($listed[strtolower($ch['Name'])]);
            self::upsertCharacter($conn, $login, $ch);
            if ($onMarket) {
                try {
                    $conn->executeUpdate('UPDATE Character SET CtlCode = 1 WHERE Name = ?', array($ch['Name']));
                } catch (\Exception $e) {
                }
                continue;
            }
            if ($i < 5) {
                $slots[$i] = $ch['Name'];
                $i++;
            }
        }

        $ac = $conn->fetchColumn('SELECT COUNT(1) FROM AccountCharacter WHERE Id = ?', array($login));
        if (!(int) $ac) {
            $conn->executeUpdate(
                'INSERT INTO AccountCharacter (Id, GameID1, GameID2, GameID3, GameID4, GameID5, GameIDC) VALUES (?, ?, ?, ?, ?, ?, ?)',
                array($login, $slots[0], $slots[1], $slots[2], $slots[3], $slots[4], $slots[0])
            );
        } else {
            $conn->executeUpdate(
                'UPDATE AccountCharacter SET GameID1=?, GameID2=?, GameID3=?, GameID4=?, GameID5=?, GameIDC=? WHERE Id=?',
                array($slots[0], $slots[1], $slots[2], $slots[3], $slots[4], $slots[0], $login)
            );
        }

        return true;
    }

    /**
     * Conta joias do Banco de Joias OpenMU (atributos da conta).
     * Mesma fonte do client — não usa itens do vault.
     * @return array<string,int>
     */
    public static function countJewelBank($login)
    {
        $acc = self::findAccount($login);
        $out = array();
        $attrMap = array();
        foreach (\Morpheus\Account\JewelBank::catalog() as $code => $meta) {
            $out[$code] = 0;
            if (!empty($meta['attribute_id'])) {
                $attrMap[strtolower($meta['attribute_id'])] = $code;
            }
        }
        if (!$acc || empty($acc['Id']) || empty($attrMap)) {
            return $out;
        }

        $ids = array_keys($attrMap);
        $placeholders = array();
        $params = array('id' => $acc['Id']);
        foreach ($ids as $i => $uuid) {
            $key = 'd' . $i;
            $placeholders[] = ':' . $key;
            $params[$key] = $uuid;
        }
        $st = self::pdo()->prepare(
            'SELECT lower(s."DefinitionId"::text) AS def, COALESCE(s."Value", 0) AS qty
             FROM data."StatAttribute" s
             WHERE s."AccountId" = :id
               AND s."DefinitionId" IN (' . implode(',', $placeholders) . ')'
        );
        $st->execute($params);
        while ($row = $st->fetch()) {
            $def = strtolower((string) $row['def']);
            if (isset($attrMap[$def])) {
                $out[$attrMap[$def]] = (int) round((float) $row['qty']);
            }
        }
        return $out;
    }

    /**
     * @deprecated Use countJewelBank — o banco de joias não fica no vault.
     * @return array<string,int>
     */
    public static function countVaultJewels($login)
    {
        return self::countJewelBank($login);
    }

    /**
     * Transfere joias do Banco de Joias (atributos da conta) entre logins.
     * @param array<string,int> $prices
     */
    public static function transferJewelBank($fromLogin, $toLogin, array $prices)
    {
        $from = self::findAccount($fromLogin);
        $to = self::findAccount($toLogin);
        if (!$from || !$to) {
            throw new \Exception('Conta OpenMU não encontrada para transferência de joias.');
        }

        $catalog = \Morpheus\Account\JewelBank::catalog();
        $have = self::countJewelBank($fromLogin);
        foreach ($catalog as $code => $meta) {
            $need = isset($prices[$code]) ? (int) $prices[$code] : 0;
            if ($need > 0 && $have[$code] < $need) {
                throw new \Exception('Joias insuficientes no banco: ' . $meta['name']);
            }
        }

        $pdo = self::pdo();
        $pdo->beginTransaction();
        try {
            foreach ($catalog as $code => $meta) {
                $need = isset($prices[$code]) ? (int) $prices[$code] : 0;
                if ($need <= 0 || empty($meta['attribute_id'])) {
                    continue;
                }
                self::adjustJewelBankAttribute($pdo, $from['Id'], $meta['attribute_id'], -$need);
                self::adjustJewelBankAttribute($pdo, $to['Id'], $meta['attribute_id'], $need);
            }
            $pdo->commit();
        } catch (\Exception $e) {
            $pdo->rollBack();
            throw $e;
        }
    }

    /**
     * @deprecated Use transferJewelBank
     */
    public static function transferVaultJewels($fromLogin, $toLogin, array $prices)
    {
        return self::transferJewelBank($fromLogin, $toLogin, $prices);
    }

    /**
     * @param \PDO $pdo
     */
    private static function adjustJewelBankAttribute($pdo, $accountId, $attributeId, $delta)
    {
        $delta = (int) $delta;
        if ($delta === 0) {
            return;
        }
        $attributeId = strtolower((string) $attributeId);

        $st = $pdo->prepare(
            'SELECT "Id", "Value" FROM data."StatAttribute"
             WHERE "AccountId" = :aid AND "DefinitionId" = :def
             LIMIT 1 FOR UPDATE'
        );
        $st->execute(array('aid' => $accountId, 'def' => $attributeId));
        $row = $st->fetch();

        if ($row) {
            $next = (float) $row['Value'] + $delta;
            if ($next < 0) {
                throw new \Exception('Saldo de joias insuficiente no banco.');
            }
            $upd = $pdo->prepare('UPDATE data."StatAttribute" SET "Value" = :v WHERE "Id" = :id');
            $upd->execute(array('v' => $next, 'id' => $row['Id']));
            return;
        }

        if ($delta < 0) {
            throw new \Exception('Saldo de joias insuficiente no banco.');
        }

        $ins = $pdo->prepare(
            'INSERT INTO data."StatAttribute" ("Id", "DefinitionId", "AccountId", "CharacterId", "Value")
             VALUES (:id, :def, :aid, NULL, :v)'
        );
        $ins->execute(array(
            'id' => self::uuid(),
            'def' => $attributeId,
            'aid' => $accountId,
            'v' => (float) $delta,
        ));
    }

    /** @var array<string, string> */
    private static $itemNameCache = array();

    /**
     * Nome oficial do item em config."ItemDefinition" (OpenMU).
     */
    public static function itemDefinitionName($group, $number)
    {
        $group = (int) $group;
        $number = (int) $number;
        $key = $group . '-' . $number;
        if (array_key_exists($key, self::$itemNameCache)) {
            return self::$itemNameCache[$key];
        }
        try {
            $st = self::pdo()->prepare(
                'SELECT "Name" FROM config."ItemDefinition" WHERE "Group" = :g AND "Number" = :n LIMIT 1'
            );
            $st->execute(array('g' => $group, 'n' => $number));
            $name = $st->fetchColumn();
            self::$itemNameCache[$key] = ($name !== false && $name !== null && $name !== '') ? (string) $name : '';
        } catch (\Exception $e) {
            self::$itemNameCache[$key] = '';
        }
        return self::$itemNameCache[$key];
    }

    /** Zen no vault OpenMU (ItemStorage.Money). */
    public static function getVaultMoney($login)
    {
        $acc = self::findAccount($login);
        if (!$acc) {
            return 0;
        }
        $st = self::pdo()->prepare(
            'SELECT COALESCE(s."Money", 0)
             FROM data."Account" a
             JOIN data."ItemStorage" s ON s."Id" = a."VaultId"
             WHERE a."Id" = :id
             LIMIT 1'
        );
        $st->execute(array('id' => $acc['Id']));
        return (int) $st->fetchColumn();
    }

    public static function setVaultMoney($login, $amount)
    {
        $acc = self::findAccount($login);
        if (!$acc) {
            return false;
        }
        $amount = max(0, (int) $amount);
        $st = self::pdo()->prepare(
            'UPDATE data."ItemStorage" s
             SET "Money" = :m
             FROM data."Account" a
             WHERE a."VaultId" = s."Id" AND a."Id" = :id'
        );
        return $st->execute(array('m' => $amount, 'id' => $acc['Id']));
    }

    /**
     * Transfere personagem OpenMU do vendedor para o comprador.
     */
    public static function transferCharacter($charName, $fromLogin, $toLogin)
    {
        $from = self::findAccount($fromLogin);
        $to = self::findAccount($toLogin);
        if (!$from || !$to) {
            throw new \Exception('Conta OpenMU não encontrada para transferência do personagem.');
        }
        $pdo = self::pdo();

        $st = $pdo->prepare(
            'SELECT "Id", "AccountId", "CharacterSlot" FROM data."Character"
             WHERE "Name" = :name AND "AccountId" = :aid LIMIT 1'
        );
        $st->execute(array('name' => $charName, 'aid' => $from['Id']));
        $ch = $st->fetch();
        if (!$ch) {
            // tenta só pelo nome (já pode ter sido movido parcialmente)
            $st2 = $pdo->prepare('SELECT "Id", "AccountId", "CharacterSlot" FROM data."Character" WHERE "Name" = :name LIMIT 1');
            $st2->execute(array('name' => $charName));
            $ch = $st2->fetch();
            if (!$ch) {
                throw new \Exception('Personagem não encontrado no OpenMU: ' . $charName);
            }
        }

        $slotSt = $pdo->prepare(
            'SELECT COALESCE(MAX("CharacterSlot"), -1) FROM data."Character" WHERE "AccountId" = :aid'
        );
        $slotSt->execute(array('aid' => $to['Id']));
        $nextSlot = ((int) $slotSt->fetchColumn()) + 1;
        if ($nextSlot > 4) {
            // OpenMU tipicamente 5 slots 0-4; verifica vazios
            $used = $pdo->prepare('SELECT "CharacterSlot" FROM data."Character" WHERE "AccountId" = :aid');
            $used->execute(array('aid' => $to['Id']));
            $busy = array();
            while ($r = $used->fetch()) {
                $busy[(int) $r['CharacterSlot']] = true;
            }
            $nextSlot = null;
            for ($s = 0; $s <= 4; $s++) {
                if (!isset($busy[$s])) {
                    $nextSlot = $s;
                    break;
                }
            }
            if ($nextSlot === null) {
                throw new \Exception('Comprador sem slot livre no OpenMU.');
            }
        }

        $upd = $pdo->prepare(
            'UPDATE data."Character" SET "AccountId" = :aid, "CharacterSlot" = :slot WHERE "Id" = :id'
        );
        $upd->execute(array(
            'aid' => $to['Id'],
            'slot' => $nextSlot,
            'id' => $ch['Id'],
        ));
        return true;
    }

    private static function fetchCharacters($accountId)
    {
        $st = self::pdo()->prepare(
            'SELECT c."Id", c."Name", c."Experience", c."MasterExperience", c."PlayerKillCount", c."CharacterSlot",
                    c."PositionX", c."PositionY", cc."Number" AS class_number,
                    COALESCE((
                        SELECT ca."Value" FROM data."StatAttribute" ca
                        JOIN config."AttributeDefinition" ad ON ad."Id" = ca."DefinitionId"
                        WHERE ca."CharacterId" = c."Id" AND ad."Designation" = \'Level\'
                        LIMIT 1
                    ), 1) AS level,
                    COALESCE((
                        SELECT ca."Value" FROM data."StatAttribute" ca
                        JOIN config."AttributeDefinition" ad ON ad."Id" = ca."DefinitionId"
                        WHERE ca."CharacterId" = c."Id" AND ad."Designation" = \'Strength\'
                        LIMIT 1
                    ), 18) AS strength,
                    COALESCE((
                        SELECT ca."Value" FROM data."StatAttribute" ca
                        JOIN config."AttributeDefinition" ad ON ad."Id" = ca."DefinitionId"
                        WHERE ca."CharacterId" = c."Id" AND ad."Designation" = \'Agility\'
                        LIMIT 1
                    ), 18) AS agility,
                    COALESCE((
                        SELECT ca."Value" FROM data."StatAttribute" ca
                        JOIN config."AttributeDefinition" ad ON ad."Id" = ca."DefinitionId"
                        WHERE ca."CharacterId" = c."Id" AND ad."Designation" = \'Vitality\'
                        LIMIT 1
                    ), 15) AS vitality,
                    COALESCE((
                        SELECT ca."Value" FROM data."StatAttribute" ca
                        JOIN config."AttributeDefinition" ad ON ad."Id" = ca."DefinitionId"
                        WHERE ca."CharacterId" = c."Id" AND ad."Designation" = \'Energy\'
                        LIMIT 1
                    ), 15) AS energy,
                    COALESCE((
                        SELECT ca."Value" FROM data."StatAttribute" ca
                        JOIN config."AttributeDefinition" ad ON ad."Id" = ca."DefinitionId"
                        WHERE ca."CharacterId" = c."Id" AND ad."Designation" = \'Leadership\'
                        LIMIT 1
                    ), 0) AS leadership
             FROM data."Character" c
             JOIN config."CharacterClass" cc ON cc."Id" = c."CharacterClassId"
             WHERE c."AccountId" = :aid
             ORDER BY c."CharacterSlot"'
        );
        $st->execute(array('aid' => $accountId));
        return $st->fetchAll();
    }

    private static function upsertCharacter($conn, $login, $ch)
    {
        $name = substr($ch['Name'], 0, 10);
        $level = max(1, min(400, (int) $ch['level']));
        $class = self::mapOpenMuClassToClassic((int) $ch['class_number']);
        $exp = (int) min(2147483647, (int) $ch['Experience']);
        $pk = (int) $ch['PlayerKillCount'];
        $str = max(1, (int) $ch['strength']);
        $agi = max(1, (int) $ch['agility']);
        $vit = max(1, (int) $ch['vitality']);
        $ene = max(1, (int) $ch['energy']);
        $cmd = max(0, (int) $ch['leadership']);
        $x = (int) $ch['PositionX'];
        $y = (int) $ch['PositionY'];

        $exists = $conn->fetchColumn('SELECT COUNT(1) FROM Character WHERE Name = ?', array($name));
        if ((int) $exists) {
            $conn->executeUpdate(
                'UPDATE Character SET AccountID=?, cLevel=?, Class=?, Experience=?, Strength=?, Dexterity=?, Vitality=?, Energy=?,
                 Leadership=?, PkCount=?, PkCountWeb=?, MapPosX=?, MapPosY=?, ResetCount=ISNULL(ResetCount,0), MasterResetCount=ISNULL(MasterResetCount,0)
                 WHERE Name=?',
                array($login, $level, $class, $exp, $str, $agi, $vit, $ene, $cmd, $pk, $pk, $x, $y, $name)
            );
        } else {
            // Inventário/MagicList vazios (tamanho típico deste MuOnline)
            $conn->executeUpdate(
                'INSERT INTO Character
                (AccountID, Name, cLevel, Class, Experience, Strength, Dexterity, Vitality, Energy, Leadership,
                 LevelUpPoint, Money, MapNumber, MapPosX, MapPosY, PkCount, PkLevel, PkCountWeb, CtlCode,
                 Life, MaxLife, Mana, MaxMana, BP, MaxBP, Inventory, MagicList,
                 ResetCount, MasterResetCount, resets)
                VALUES
                (?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
                 0, 0, 0, ?, ?, ?, 3, ?, 0,
                 100, 100, 100, 100, 100, 100,
                 CONVERT(varbinary(max), REPLICATE(CHAR(0xFF), 3984)),
                 CONVERT(varbinary(max), REPLICATE(CHAR(0xFF), 180)),
                 0, 0, 0)',
                array(
                    $login, $name, $level, $class, $exp, $str, $agi, $vit, $ene, $cmd,
                    $x, $y, $pk, $pk,
                )
            );
        }

        // Espelha equipamento/inventário (e cosméticos em slots de wear) do OpenMU
        try {
            InventorySync::syncCharacter($name);
        } catch (\Exception $e) {
            error_log('OpenMU InventorySync failed for ' . $name . ': ' . $e->getMessage());
        }
    }

    /**
     * OpenMU usa CharacterClass.Number (ex.: BladeMaster=7).
     * Morpheus/web espera o Class clássico do client (ex.: Blade Master=18).
     */
    public static function mapOpenMuClassToClassic($openMuNumber)
    {
        static $map = array(
            0 => 0,   // Dark Wizard
            1 => 0,
            2 => 1,   // Soul Master
            3 => 2,   // Grand Master
            4 => 16,  // Dark Knight
            5 => 16,
            6 => 17,  // Blade Knight
            7 => 18,  // Blade Master
            8 => 32,  // Fairy Elf
            9 => 32,
            10 => 33, // Muse Elf
            11 => 34, // High Elf
            12 => 48, // Magic Gladiator
            13 => 50, // Duel Master
            14 => 50,
            15 => 50,
            16 => 64, // Dark Lord
            17 => 66, // Lord Emperor
            18 => 67,
            19 => 67,
            20 => 80, // Summoner
            21 => 80,
            22 => 81, // Bloody Summoner
            23 => 82, // Dimension Master
            24 => 96, // Rage Fighter
            25 => 98, // Fist Master
            26 => 98,
            27 => 98,
            28 => 112, // Grow Lancer (approx)
            29 => 112,
            30 => 114,
            31 => 114,
        );
        $n = (int) $openMuNumber;
        if (isset($map[$n])) {
            return $map[$n];
        }
        // Se já estiver no formato clássico (16, 32, 48...), mantém
        return $n;
    }

    private static function uuid()
    {
        $data = random_bytes(16);
        $data[6] = chr((ord($data[6]) & 0x0f) | 0x40);
        $data[8] = chr((ord($data[8]) & 0x3f) | 0x80);
        return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($data), 4));
    }
}
