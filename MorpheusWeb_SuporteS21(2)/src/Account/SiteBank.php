<?php

namespace Morpheus\Account;

use Morpheus\Database\Connection;
use Morpheus\Item\Item;
use Morpheus\OpenMU\Bridge;
use Morpheus\OpenMU\VaultSync;
use Morpheus\Util\Item as ItemUtil;

class SiteBank
{
    public static function ensureSchema()
    {
        static $done = false;
        if ($done) {
            return;
        }
        $done = true;
        try {
            if (!Connection::fetchColumn("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'mw_site_bank'")) {
                Connection::executeUpdate(
                    "CREATE TABLE mw_site_bank (
                        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        account NVARCHAR(10) NOT NULL,
                        hex VARCHAR(64) NOT NULL,
                        item_section INT NOT NULL DEFAULT 0,
                        item_index INT NOT NULL DEFAULT 0,
                        item_name NVARCHAR(120) NULL,
                        item_level INT NOT NULL DEFAULT 0,
                        item_option INT NOT NULL DEFAULT 0,
                        source NVARCHAR(32) NULL,
                        created_at DATETIME NOT NULL DEFAULT GETDATE()
                    )"
                );
                Connection::executeUpdate("CREATE INDEX IX_mw_site_bank_account ON mw_site_bank (account)");
            }
            if (!Connection::fetchColumn("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'mw_site_bank_zen'")) {
                Connection::executeUpdate(
                    "CREATE TABLE mw_site_bank_zen (
                        account NVARCHAR(10) NOT NULL PRIMARY KEY,
                        money BIGINT NOT NULL DEFAULT 0
                    )"
                );
            }
        } catch (\Exception $e) {
            error_log('SiteBank::ensureSchema: ' . $e->getMessage());
        }
    }

    public static function getZen($login)
    {
        self::ensureSchema();
        $v = Connection::fetchColumn('SELECT money FROM mw_site_bank_zen WHERE account = ?', array($login));
        return ($v === null || $v === false) ? 0 : (int) $v;
    }

    public static function setZen($login, $amount)
    {
        self::ensureSchema();
        $amount = max(0, (int) $amount);
        if (Connection::fetchColumn('SELECT 1 FROM mw_site_bank_zen WHERE account = ?', array($login))) {
            Connection::executeUpdate('UPDATE mw_site_bank_zen SET money = ? WHERE account = ?', array($amount, $login));
        } else {
            Connection::executeUpdate('INSERT INTO mw_site_bank_zen (account, money) VALUES (?, ?)', array($login, $amount));
        }
    }

    public static function transferZen($login, $amount, $dir)
    {
        $amount = (int) $amount;
        if ($amount <= 0) {
            throw new \Exception('Informe um valor de Zen válido.');
        }
        if (!Bridge::enabled()) {
            throw new \Exception('OpenMU offline.');
        }
        $game = Bridge::getVaultMoney($login);
        $site = self::getZen($login);
        if ($dir === 'to_site') {
            if ($game < $amount) {
                throw new \Exception('Zen insuficiente no baú do jogo.');
            }
            Bridge::setVaultMoney($login, $game - $amount);
            self::setZen($login, $site + $amount);
        } elseif ($dir === 'to_game') {
            if ($site < $amount) {
                throw new \Exception('Zen insuficiente no Banco do Site.');
            }
            self::setZen($login, $site - $amount);
            Bridge::setVaultMoney($login, $game + $amount);
        } else {
            throw new \Exception('Direção inválida.');
        }
        try {
            Connection::executeUpdate('UPDATE warehouse SET Money = ? WHERE AccountID = ?', array(Bridge::getVaultMoney($login), $login));
        } catch (\Exception $e) {
        }
        return true;
    }

    public static function depositHex($login, $hex, $source = 'market')
    {
        self::ensureSchema();
        $hex = strtoupper(preg_replace('/[^0-9A-Fa-f]/', '', (string) $hex));
        if ($hex === '') {
            return false;
        }
        $it = (new Item($hex))->parse();
        $name = ItemUtil::getDisplayName($it);
        Connection::executeUpdate(
            "INSERT INTO mw_site_bank (account, hex, item_section, item_index, item_name, item_level, item_option, source)
             VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            array($login, $hex, (int) $it->getSection(), (int) $it->getIndex(), $name, (int) $it->getLevel(), (int) $it->getOption(), $source)
        );
        return (int) Connection::fetchColumn(
            'SELECT TOP 1 id FROM mw_site_bank WHERE account = ? AND hex = ? ORDER BY id DESC',
            array($login, $hex)
        );
    }

    public static function listForLogin($login)
    {
        self::ensureSchema();
        $out = array();
        foreach (Connection::fetchAll('SELECT * FROM mw_site_bank WHERE account = ? ORDER BY id ASC', array($login)) as $row) {
            $out[] = self::rowToView($row);
        }
        return $out;
    }

    public static function count($login)
    {
        self::ensureSchema();
        return (int) Connection::fetchColumn('SELECT COUNT(*) FROM mw_site_bank WHERE account = ?', array($login));
    }

    public static function get($login, $id)
    {
        self::ensureSchema();
        return Connection::fetchAssoc('SELECT * FROM mw_site_bank WHERE id = ? AND account = ?', array((int) $id, $login));
    }

    private static function rowToView(array $row)
    {
        try {
            $it = (new Item($row['hex']))->parse();
        } catch (\Exception $e) {
            $it = new Item($row['hex']);
            $it->setSection((int) $row['item_section'])->setIndex((int) $row['item_index'])->update();
        }
        return ItemUtil::bankItemView($it, array(
            'id' => (int) $row['id'],
            'source' => isset($row['source']) ? $row['source'] : null,
        ));
    }

    public static function withdrawToGame($login, $id)
    {
        self::ensureSchema();
        if (!Bridge::enabled()) {
            throw new \Exception('OpenMU offline.');
        }
        $row = self::get($login, $id);
        if (!$row) {
            throw new \Exception('Item não encontrado no banco do site.');
        }
        $it = (new Item($row['hex']))->parse();
        if (!VaultSync::addFromMorpheusItem($login, $it)) {
            throw new \Exception('Sem espaço no baú do jogo ou item inválido.');
        }
        Connection::executeUpdate('DELETE FROM mw_site_bank WHERE id = ? AND account = ?', array((int) $id, $login));
        VaultSync::syncAccount($login);
        return true;
    }

    public static function depositFromGame($login, $vaultItemId)
    {
        self::ensureSchema();
        if (!Bridge::enabled()) {
            throw new \Exception('OpenMU offline.');
        }
        $vault = VaultSync::getVaultItem($login, $vaultItemId);
        if (!$vault) {
            throw new \Exception('Item não encontrado no baú do jogo.');
        }
        $hex = \Morpheus\OpenMU\InventorySync::itemToHex($vault);
        if (!$hex) {
            throw new \Exception('Não foi possível converter o item do baú.');
        }
        if (!VaultSync::removeById($login, $vaultItemId)) {
            throw new \Exception('Falha ao remover item do baú do jogo.');
        }
        self::depositHex($login, $hex, 'vault');
        VaultSync::syncAccount($login);
        return true;
    }

    /** Remove item do banco do site e retorna a linha (hex, etc.) para listar no mercado. */
    public static function takeForMarket($login, $id)
    {
        self::ensureSchema();
        $row = self::get($login, $id);
        if (!$row) {
            throw new \Exception('Item não encontrado no Banco do Site.');
        }
        Connection::executeUpdate(
            'DELETE FROM mw_site_bank WHERE id = ? AND account = ?',
            array((int) $id, $login)
        );
        return $row;
    }
}
