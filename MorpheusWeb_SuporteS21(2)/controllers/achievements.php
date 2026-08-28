<?php

Route::get("/panel/achievements", authenticate(), function () {
    $schema = Connection::getSchemaManager();
    if (!$schema->tablesExist("mw_achievements_claimed")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_achievements_claimed");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("account", "string", array("length" => 50));
        $table->addColumn("achievement_id", "integer");
        $table->addColumn("created_at", "datetime");
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    }

    $account = user()->getUsername();
    $achievements = Connection::fetchAll("SELECT * FROM mw_achievements WHERE active = 1 ORDER BY id ASC");
    
    $claimed = Connection::fetchAll("SELECT achievement_id FROM mw_achievements_claimed WHERE account = ?", array($account));
    $claimedIds = array();
    foreach ($claimed as $c) {
        $claimedIds[] = $c['achievement_id'];
    }

    foreach ($achievements as &$ach) {
        $ach['claimed'] = in_array($ach['id'], $claimedIds);
        
        $reqs = json_decode($ach['requirements'], true) ?: array();
        $isCompleted = true;
        $progress = 0;
        
        if (!empty($reqs)) {
            foreach ($reqs as $req) {
                try {
                    // Executa a Query substituindo a variavel :account pelo login do jogador
                    $val = (int) Connection::fetchColumn($req['sql'], array("account" => $account));
                    if ($val < $ach['required_amount']) {
                        $isCompleted = false;
                    }
                    if ($val > $progress) {
                        $progress = $val;
                    }
                } catch(Exception $e) {
                    $isCompleted = false;
                }
            }
        } else {
            $isCompleted = false;
        }
        
        $ach['completed'] = $isCompleted;
        $ach['progress'] = min($progress, $ach['required_amount']);
    }
    
    View::display("achievements", array("achievements" => $achievements));
})->name("panel.achievements");

Route::get("/panel/achievements/claim/:id", authenticate(), function ($id) {
    $account = user()->getUsername();
    $ach = Connection::fetchAssoc("SELECT * FROM mw_achievements WHERE id = ? AND active = 1", array($id));
    if (!$ach) return error("Conquista não encontrada.");

    $claimed = Connection::fetchColumn("SELECT COUNT(*) FROM mw_achievements_claimed WHERE account = ? AND achievement_id = ?", array($account, $id));
    if ($claimed > 0) return error("Você já resgatou esta conquista.");

    $reqs = json_decode($ach['requirements'], true) ?: array();
    $isCompleted = true;
    if (!empty($reqs)) {
        foreach ($reqs as $req) {
            try {
                $val = (int) Connection::fetchColumn($req['sql'], array("account" => $account));
                if ($val < $ach['required_amount']) {
                    $isCompleted = false;
                    break;
                }
            } catch(Exception $e) {
                return error("Erro ao verificar requisitos: " . $e->getMessage());
            }
        }
    } else {
        $isCompleted = false;
    }

    if (!$isCompleted) return error("Você ainda não concluiu esta conquista.");

    // Entregar as Recompensas!
    $rews = json_decode($ach['rewards'], true) ?: array();
    if (!empty($rews)) {
        foreach ($rews as $rew) {
            try {
                Connection::executeUpdate($rew['sql'], array("account" => $account));
            } catch(Exception $e) {
                return error("Erro ao entregar recompensa: " . $e->getMessage());
            }
        }
    }

    Connection::insert("mw_achievements_claimed", array(
        "account" => $account,
        "achievement_id" => $id,
        "created_at" => (new DateTime())->format(DATETIME_ISO_FORMAT)
    ));

    return success("Recompensa resgatada com sucesso!", array("redirect" => "/panel/achievements"));
})->name("panel.achievements.claim");