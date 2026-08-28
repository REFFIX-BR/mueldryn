<?php

use Morpheus\Database\Connection;

Connection::transactional(function () {
    $currentDate = date('Y-m-d H:i:s');
    
    $expiredAuctions = Connection::fetchAll(
        "SELECT * FROM player_auctions WHERE status = 'active' AND end_date < ?",
        array($currentDate)
    );
    
    foreach ($expiredAuctions as $auction) {
        if (!empty($auction['winner'])) {
            Connection::update("player_auctions", 
                array("status" => "completed"), 
                array("id" => $auction['id'])
            );
            
            echo "Leilão #{$auction['id']} finalizado. Vencedor: {$auction['winner']}\n";
        } else {
            $seller = new \Morpheus\Account\Account($auction['seller']);
            if ($seller->exists()) {
                $warehouse = $seller->getWarehouse();
                $warehouse->load();
                
                $itemObj = new \Morpheus\Item\Item($auction['item_hex']);
                $itemObj->parse();
                
                if (!$itemObj->isHexEmpty()) {
                    $warehouse->addItem($itemObj);
                    $warehouse->update();
                    
                    Connection::update("player_auctions", 
                        array("status" => "cancelled"), 
                        array("id" => $auction['id'])
                    );
                    
                    echo "Leilão #{$auction['id']} cancelado (sem lances). Item devolvido ao vendedor.\n";
                }
            }
        }
    }
    
    echo "Processamento concluído. " . count($expiredAuctions) . " leilões processados.\n";
});
