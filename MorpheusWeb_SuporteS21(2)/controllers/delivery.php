<?php

$currentDate = date('Y-m-d H:i:s');

// Busca os itens que já passaram do horário de encerramento e ainda não foram entregues
$items = $conn->fetchAll("SELECT * FROM auction_items WHERE active = 1 AND delivery = 0 AND end_date IS NOT NULL AND end_date <= ?", array($currentDate));

foreach ($items as $item) {
    if (!empty($item['winner'])) {
        try {
            $conn->transactional(function ($conn) use ($item) {
                $account = new \Morpheus\Account\Account($item['winner']);
                
                if ($account->exists()) {
                    $warehouse = $account->getWarehouse();
                    $warehouse->load();
                    
                    $itemObj = new \Morpheus\Item\Item(str_pad($item['item'], 32, '0'));
                    $itemObj->parse();
                    
                    if (!$itemObj->isHexEmpty()) {
                        // Tenta adicionar o item no baú do jogador
                        $warehouse->addItem($itemObj);
                        $warehouse->update();
                    }
                }
                
                // Se o item foi entregue com sucesso, atualiza o status na tabela
                $conn->update("auction_items", array(
                    "delivery" => 1,
                    "can_delivery" => 0,
                    "active" => 0
                ), array("id" => $item['id']));
            });
        } catch (Exception $e) {
            // Se der erro (exemplo: Baú Cheio), ele ativa a opção "can_delivery"
            // para o Administrador entregar manualmente depois pelo painel
            $conn->update("auction_items", array(
                "can_delivery" => 1,
                "active" => 0
            ), array("id" => $item['id']));
        }
    } else {
        // Se o item não teve nenhum vencedor, marcamos como "entregue" para ignorá-lo
        $conn->update("auction_items", array("delivery" => 1, "active" => 0), array("id" => $item['id']));
    }
}

// Encerra os leilões principais que já passaram da validade geral (opcional para limpar a lista)
$conn->executeUpdate("UPDATE auctions SET active = 0 WHERE active = 1 AND end_date <= ?", array($currentDate));