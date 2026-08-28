<?php

use Morpheus\Database\Connection;

$accounts = Connection::fetchAll('select mi.memb___id as origin
    , mii.memb___id as destination
    from ' . table_with_db('MEMB_INFO') . ' as mi
    join ' . table_with_db('MEMB_INFO') . ' as mii on mii.indication_code = mi.indicated_by
    where mi.indicated_by is not null
    and mii.memb___id is not null
');
$goals = Connection::fetchAll('select *
    from mw_indication_goals
');

foreach ($accounts as $account) {
   foreach ($goals as $goal) {
       $exists = Connection::fetchColumn('select 1 
          from mw_indication_rewards
          where account = ?
          and indication = ?
          and indication_goal_id =?
       ', [$account['destination'], $account['origin'], $goal['id']]);

       if (!$exists) {
            $amount = Connection::fetchColumn($goal['amount'], [$account['origin'], $account['destination']]);
            if ($amount) {
                Connection::insert('mw_indication_rewards', [
                    'amount' => $amount,
                    'account' => $account['destination'],
                    'indication' => $account['origin'],
                    'indication_goal_id' => $goal['id'],
                    'rewarded' => 0
                ], [
                    'integer', 'string', 'string',
                    'integer', 'integer'
                ]);
            }
       }
   }
}