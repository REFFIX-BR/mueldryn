<?php

return [

    'conn' => [
        'default' => [
            'host' => 'localhost',
            'user' => 'sa',
            'password' => '',
            'dbname' => 'MuOnline',
            'driverClass' => 'Lsw\DoctrinePdoDblib\Doctrine\DBAL\Driver\PDODblib\Driver'
        ]
    ],

    'columns' => [
        'credit' => 'credit',
        'token' => 'memb_token',
        'avatar' => 'Avatar',
        'resets' => 'resets',
        'master_resets' => false,
        'pk' => 'PkCountWeb',
        'hero' => 'PkCountWeb'
    ],

    'use_vi_curr_info' => false,
    'use_md5' => false,
    'generate_guid' => true
];