<?php

$bans = $conn->fetchAll('select * from 
    mw_banned_accounts 
    where unblock_date >= GETDATE() 
    and status = 1
');

foreach ($bans as $ban) {
    $conn->transactional(function ($conn) use ($ban) {
        $conn->update('mw_banned_accounts', [
            'status' => 0
        ], [
            'id' => $ban['id']
        ], [
            'integer', 'integer'
        ]);

        $conn->update('MEMB_INFO', [
            'bloc_code' => 0
        ], [
            'memb___id' => $ban['account']
        ], [
            'integer', 'string'
        ]);
    });
}