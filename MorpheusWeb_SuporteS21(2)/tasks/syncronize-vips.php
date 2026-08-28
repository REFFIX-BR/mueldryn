<?php

if (config('vip.table')) {
    $conn->executeUpdate('update ' . config('vip.table') . ' set
        ' . config('vip.column_type') . ' = 0,
        ' . config('vip.column_expire') . ' = GETDATE()
        where ' . config('vip.column_expire') . ' < GETDATE()
    ');
}