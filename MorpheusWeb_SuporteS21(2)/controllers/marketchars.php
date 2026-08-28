<?php
/*
 * CharMarket — listar / vender / comprar / cancelar personagens
 * Pagamento: moeda (coin) e/ou joias (Bless, Soul, Life, Creation, Chaos)
 */

use Morpheus\Account\JewelBank;

/**
 * Garante colunas de joias na tabela do mercado.
 */
function marketchars_ensure_schema()
{
    static $done = false;
    if ($done) {
        return;
    }
    $done = true;
    try {
        $cols = Connection::fetchAll(
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'mw_market_chars'"
        );
        $have = array();
        foreach ($cols as $c) {
            $have[strtolower($c['COLUMN_NAME'])] = true;
        }
        $add = array(
            'price_jb' => 'INT NOT NULL DEFAULT 0',
            'price_js' => 'INT NOT NULL DEFAULT 0',
            'price_jl' => 'INT NOT NULL DEFAULT 0',
            'price_jcr' => 'INT NOT NULL DEFAULT 0',
            'price_jc' => 'INT NOT NULL DEFAULT 0',
        );
        foreach ($add as $name => $def) {
            if (!isset($have[strtolower($name)])) {
                Connection::executeUpdate("ALTER TABLE mw_market_chars ADD {$name} {$def}");
            }
        }
        // coin_id / price podem ficar 0/'jewels' quando só joias
    } catch (Exception $e) {
        error_log('marketchars_ensure_schema: ' . $e->getMessage());
    }
}

function marketchars_jewel_prices_from_row($row)
{
    return array(
        'jb' => isset($row['price_jb']) ? (int) $row['price_jb'] : 0,
        'js' => isset($row['price_js']) ? (int) $row['price_js'] : 0,
        'jl' => isset($row['price_jl']) ? (int) $row['price_jl'] : 0,
        'jcr' => isset($row['price_jcr']) ? (int) $row['price_jcr'] : 0,
        'jc' => isset($row['price_jc']) ? (int) $row['price_jc'] : 0,
    );
}

function marketchars_find_free_slot($username)
{
    $accountChar = Connection::fetchAssoc('SELECT * FROM AccountCharacter WHERE Id = ?', array($username));
    $freeSlot = null;
    if ($accountChar) {
        for ($i = 1; $i <= 5; $i++) {
            if (empty($accountChar['GameID' . $i])) {
                $freeSlot = 'GameID' . $i;
                break;
            }
        }
    } else {
        $freeSlot = 'GameID1';
    }
    return array($accountChar, $freeSlot);
}

marketchars_ensure_schema();

// Página inicial: Lista de Personagens à Venda
Route::map('/panel/marketchars', function () {
    if (!logged_in()) {
        return App::redirect('/login');
    }

    $charsForSale = Connection::fetchAll('SELECT * FROM mw_market_chars ORDER BY id DESC');
    $charsData = array();
    foreach ($charsForSale as $char) {
        $charObj = new \Morpheus\Character\Character($char['character_name']);
        if ($charObj->exists()) {
            $charObj->getInventory()->load();
            $char['cLevel'] = $charObj->getLevel();
            $char['ResetCount'] = method_exists($charObj, 'getResets') ? $charObj->getResets() : 0;
            $char['MasterResetCount'] = method_exists($charObj, 'getMasterResets') ? $charObj->getMasterResets() : 0;
            $char['Class'] = $charObj->getClass();
            $stats = Connection::fetchAssoc(
                'SELECT Strength, Dexterity, Vitality, Energy, Leadership FROM Character WHERE Name = ?',
                array($char['character_name'])
            );
            $char['Strength'] = $stats ? $stats['Strength'] : 0;
            $char['Dexterity'] = $stats ? $stats['Dexterity'] : 0;
            $char['Vitality'] = $stats ? $stats['Vitality'] : 0;
            $char['Energy'] = $stats ? $stats['Energy'] : 0;
            $char['Leadership'] = $stats ? $stats['Leadership'] : 0;
            $char['jewel_prices'] = marketchars_jewel_prices_from_row($char);
            $char['character'] = $charObj;
            $charsData[] = $char;
        }
    }

    $jewels = JewelBank::counts(user());
    View::display('marketchars', array(
        'chars' => $charsData,
        'coins' => config('coins', array()),
        'jewels' => $jewels,
        'jewelCatalog' => JewelBank::catalog(),
    ));
})->via('GET')->name('panel.marketchars');


// Página: Detalhes do personagem à venda (estilo MuDream)
Route::map('/panel/marketchars/info/:id', function ($id) {
    if (!logged_in()) {
        return App::redirect('/login');
    }

    $listing = Connection::fetchAssoc('SELECT * FROM mw_market_chars WHERE id = ?', array($id));
    if (!$listing) {
        return error('Anúncio não encontrado ou já vendido.');
    }

    $charObj = new \Morpheus\Character\Character($listing['character_name']);
    if (!$charObj->exists()) {
        return error('Personagem não encontrado.');
    }

    $charObj->getInventory()->load();
    $listing['jewel_prices'] = marketchars_jewel_prices_from_row($listing);

    View::display('marketchars-info', array(
        'listing' => $listing,
        'character' => $charObj,
        'coins' => config('coins', array()),
        'jewelCatalog' => JewelBank::catalog(),
        'isOwner' => $listing['seller'] === user()->getUsername(),
    ));
})->via('GET')->name('panel.marketchars.info');


// Página: Vender um Personagem
Route::map('/panel/marketchars/sell', function () {
    if (!logged_in()) {
        return App::redirect('/login');
    }

    $username = user()->getUsername();
    $charsList = Connection::fetchAll('SELECT Name FROM Character WHERE AccountID = ? AND CtlCode = 0', array($username));
    $myChars = array();
    foreach ($charsList as $c) {
        $charObj = new \Morpheus\Character\Character($c['Name']);
        if ($charObj->exists()) {
            $stats = Connection::fetchAssoc(
                'SELECT Strength, Dexterity, Vitality, Energy, Leadership FROM Character WHERE Name = ?',
                array($c['Name'])
            );
            $myChars[] = array(
                'Name' => $charObj->getName(),
                'Class' => $charObj->getClass(),
                'cLevel' => $charObj->getLevel(),
                'ResetCount' => method_exists($charObj, 'getResets') ? $charObj->getResets() : 0,
                'MasterResetCount' => method_exists($charObj, 'getMasterResets') ? $charObj->getMasterResets() : 0,
                'Strength' => $stats ? $stats['Strength'] : 0,
                'Dexterity' => $stats ? $stats['Dexterity'] : 0,
                'Vitality' => $stats ? $stats['Vitality'] : 0,
                'Energy' => $stats ? $stats['Energy'] : 0,
                'Leadership' => $stats ? $stats['Leadership'] : 0,
            );
        }
    }

    if (Request::isPost()) {
        if (user()->isConnected()) {
            return error('Saia do jogo antes de colocar o personagem à venda.');
        }

        $charName = Input::post('character_name');
        $price = (int) Input::post('price');
        $coinId = Input::post('coin_id');
        $jewelPrices = JewelBank::parsePrices(array(
            'jb' => Input::post('price_jb'),
            'js' => Input::post('price_js'),
            'jl' => Input::post('price_jl'),
            'jcr' => Input::post('price_jcr'),
            'jc' => Input::post('price_jc'),
        ));

        $coinsConfig = config('coins', array());
        $useCoin = $price > 0 && $coinId !== '' && $coinId !== null;
        $useJewels = JewelBank::hasAnyPrice($jewelPrices);

        if (!$useCoin && !$useJewels) {
            return error('Defina um preço em moeda e/ou em joias.');
        }
        if ($useCoin && !isset($coinsConfig[$coinId])) {
            return error('Moeda inválida selecionada.');
        }
        if (!$useCoin) {
            $coinId = 'jewels';
            $price = 0;
        }

        $charExists = Connection::fetchColumn(
            'SELECT COUNT(*) FROM Character WHERE Name = ? AND AccountID = ? AND CtlCode = 0',
            array($charName, $username)
        );
        if (!$charExists) {
            return error('Personagem inválido ou já bloqueado.');
        }

        try {
            Connection::transactional(function () use ($charName, $price, $coinId, $username, $jewelPrices) {
                Connection::update('Character', array('CtlCode' => 1), array('Name' => $charName, 'AccountID' => $username));

                $accountChar = Connection::fetchAssoc('SELECT * FROM AccountCharacter WHERE Id = ?', array($username));
                if ($accountChar) {
                    $updateSlot = array();
                    for ($i = 1; $i <= 5; $i++) {
                        if ($accountChar['GameID' . $i] === $charName) {
                            $updateSlot['GameID' . $i] = null;
                        }
                    }
                    if (!empty($updateSlot)) {
                        Connection::update('AccountCharacter', $updateSlot, array('Id' => $username));
                    }
                }

                Connection::insert('mw_market_chars', array(
                    'seller' => $username,
                    'character_name' => $charName,
                    'price' => $price,
                    'coin_id' => $coinId,
                    'price_jb' => $jewelPrices['jb'],
                    'price_js' => $jewelPrices['js'],
                    'price_jl' => $jewelPrices['jl'],
                    'price_jcr' => $jewelPrices['jcr'],
                    'price_jc' => $jewelPrices['jc'],
                    'created_at' => date('Y-m-d H:i:s'),
                ));
            });
            return success('Personagem colocado à venda com sucesso!', array('redirect' => '/panel/marketchars'));
        } catch (Exception $ex) {
            return error('Erro ao colocar à venda: ' . $ex->getMessage());
        }
    }

    View::display('marketchars-sell', array(
        'myChars' => $myChars,
        'coins' => config('coins', array()),
        'jewelCatalog' => JewelBank::catalog(),
        'jewels' => JewelBank::counts(user()),
    ));
})->via('GET', 'POST')->name('panel.marketchars.sell');


// Ação: Comprar Personagem
Route::map('/panel/marketchars/buy/:id', function ($id) {
    if (!logged_in()) {
        return App::redirect('/login');
    }

    if (user()->isConnected()) {
        return error('Saia do jogo antes de comprar um personagem.');
    }

    $buyer = user()->getUsername();
    $listing = Connection::fetchAssoc('SELECT * FROM mw_market_chars WHERE id = ?', array($id));

    if (!$listing) {
        return error('Este anúncio não existe mais.');
    }
    if ($listing['seller'] === $buyer) {
        return error('Você não pode comprar seu próprio personagem.');
    }

    $jewelPrices = marketchars_jewel_prices_from_row($listing);
    $useJewels = JewelBank::hasAnyPrice($jewelPrices);
    $price = (int) $listing['price'];
    $coinId = $listing['coin_id'];
    $useCoin = $price > 0 && $coinId && $coinId !== 'jewels';

    $coinsConfig = config('coins', array());
    $coin = null;
    if ($useCoin) {
        if (!isset($coinsConfig[$coinId])) {
            return error('Erro interno: Moeda não configurada corretamente.');
        }
        $coin = $coinsConfig[$coinId];
        $buyerBalance = (int) Connection::fetchColumn(
            "SELECT {$coin['column']} FROM {$coin['table']} WHERE {$coin['foreign_key']} = ?",
            array($buyer)
        );
        if ($buyerBalance < $price) {
            return error('Você não possui ' . $coin['name'] . ' suficiente para comprar este personagem.');
        }
    }

    if ($useJewels) {
        $ok = JewelBank::canAfford($buyer, $jewelPrices);
        if ($ok !== true) {
            return error($ok);
        }
    }

    list($accountChar, $freeSlot) = marketchars_find_free_slot($buyer);
    if (!$freeSlot) {
        return error('Você não tem espaço na sua conta para receber um novo personagem. Exclua um personagem primeiro.');
    }

    // Slot livre no OpenMU
    if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
        try {
            $toAcc = \Morpheus\OpenMU\Bridge::findAccount($buyer);
            if ($toAcc) {
                $pdo = \Morpheus\OpenMU\Bridge::pdo();
                $used = $pdo->prepare('SELECT "CharacterSlot" FROM data."Character" WHERE "AccountId" = :aid');
                $used->execute(array('aid' => $toAcc['Id']));
                $busy = array();
                while ($r = $used->fetch()) {
                    $busy[(int) $r['CharacterSlot']] = true;
                }
                $hasFree = false;
                for ($s = 0; $s <= 4; $s++) {
                    if (!isset($busy[$s])) {
                        $hasFree = true;
                        break;
                    }
                }
                if (!$hasFree) {
                    return error('Você não tem slot livre no jogo (OpenMU) para receber o personagem.');
                }
            }
        } catch (Exception $e) {
            return error('Falha ao verificar slots OpenMU: ' . $e->getMessage());
        }
    }

    try {
        Connection::transactional(function () use ($listing, $buyer, $coin, $price, $useCoin, $useJewels, $jewelPrices, $accountChar, $freeSlot) {
            if ($useCoin && $coin) {
                Connection::executeUpdate(
                    "UPDATE {$coin['table']} SET {$coin['column']} = {$coin['column']} - ? WHERE {$coin['foreign_key']} = ?",
                    array($price, $buyer)
                );
                Connection::executeUpdate(
                    "UPDATE {$coin['table']} SET {$coin['column']} = {$coin['column']} + ? WHERE {$coin['foreign_key']} = ?",
                    array($price, $listing['seller'])
                );
            }

            if ($useJewels) {
                JewelBank::transfer($buyer, $listing['seller'], $jewelPrices);
            }

            // OpenMU primeiro (fonte da verdade no jogo)
            if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
                \Morpheus\OpenMU\Bridge::transferCharacter(
                    $listing['character_name'],
                    $listing['seller'],
                    $buyer
                );
            }

            $updated = Connection::executeUpdate(
                'UPDATE Character SET AccountID = ?, CtlCode = 0 WHERE Name = ? AND AccountID = ?',
                array($buyer, $listing['character_name'], $listing['seller'])
            );
            if (!(int) $updated) {
                // fallback se AccountID já mudou / inconsistência
                Connection::executeUpdate(
                    'UPDATE Character SET AccountID = ?, CtlCode = 0 WHERE Name = ?',
                    array($buyer, $listing['character_name'])
                );
            }

            if (!$accountChar) {
                Connection::insert('AccountCharacter', array('Id' => $buyer, $freeSlot => $listing['character_name']));
            } else {
                Connection::update('AccountCharacter', array($freeSlot => $listing['character_name']), array('Id' => $buyer));
            }

            Connection::delete('mw_market_chars', array('id' => $listing['id']));
        });

        // Re-sincroniza contas para espelhar slots
        if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
            try {
                \Morpheus\OpenMU\Bridge::syncToMorpheus($buyer);
                \Morpheus\OpenMU\Bridge::syncToMorpheus($listing['seller']);
            } catch (Exception $e) {
            }
        }

        return success('Personagem comprado com sucesso! Ele já está na sua conta.', array('redirect' => '/panel/marketchars'));
    } catch (Exception $ex) {
        return error('Erro processando a compra: ' . $ex->getMessage());
    }
})->via('POST')->name('panel.marketchars.buy');


// Ação: Cancelar Venda (Apenas o dono)
Route::map('/panel/marketchars/cancel/:id', function ($id) {
    if (!logged_in()) {
        return App::redirect('/login');
    }

    $seller = user()->getUsername();
    $listing = Connection::fetchAssoc('SELECT * FROM mw_market_chars WHERE id = ? AND seller = ?', array($id, $seller));

    if (!$listing) {
        return error('Anúncio não encontrado ou você não é o dono.');
    }

    list($accountChar, $freeSlot) = marketchars_find_free_slot($seller);
    if (!$freeSlot) {
        return error('Você não tem espaço na sua conta para recuperar este personagem. Exclua um personagem primeiro.');
    }

    try {
        Connection::transactional(function () use ($listing, $seller, $accountChar, $freeSlot) {
            Connection::update('Character', array('CtlCode' => 0), array('Name' => $listing['character_name'], 'AccountID' => $seller));

            if (!$accountChar) {
                Connection::insert('AccountCharacter', array('Id' => $seller, $freeSlot => $listing['character_name']));
            } else {
                Connection::update('AccountCharacter', array($freeSlot => $listing['character_name']), array('Id' => $seller));
            }

            Connection::delete('mw_market_chars', array('id' => $listing['id']));
        });

        if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
            try {
                \Morpheus\OpenMU\Bridge::syncToMorpheus($seller);
            } catch (Exception $e) {
            }
        }

        return success('Venda cancelada! Seu personagem foi desbloqueado.', array('redirect' => '/panel/marketchars'));
    } catch (Exception $ex) {
        return error('Erro ao cancelar: ' . $ex->getMessage());
    }
})->via('POST')->name('panel.marketchars.cancel');
