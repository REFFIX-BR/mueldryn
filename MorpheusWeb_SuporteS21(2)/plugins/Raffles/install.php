<?php

$schema = Connection::getSchemaManager();

if (!$schema->tablesExist("mw_raffles")) {
    $table = new Doctrine\DBAL\Schema\Table("mw_raffles");
    $table->addColumn("id", "integer", array("autoincrement" => true));
    $table->addColumn("name", "string", array("length" => 255));
    $table->addColumn("description", "text", array("notnull" => false));
    $table->addColumn("total_numbers", "integer", array("default" => 100));
    $table->addColumn("coin_id", "string", array("length" => 50, "default" => ""));
    $table->addColumn("price", "integer", array("default" => 0));
    $table->addColumn("reward_vip_type", "integer", array("default" => 0));
    $table->addColumn("reward_vip_days", "integer", array("default" => 0));
    $table->addColumn("reward_credits", "integer", array("default" => 0));
    $table->addColumn("reward_wcoinc", "integer", array("default" => 0));
    $table->addColumn("reward_wcoinp", "integer", array("default" => 0));
    $table->addColumn("reward_goblinpoints", "integer", array("default" => 0));
    $table->addColumn("reward_sql", "text", array("notnull" => false));
    $table->addColumn("winner", "string", array("length" => 50, "notnull" => false));
    $table->addColumn("winner_number", "integer", array("notnull" => false));
    $table->addColumn("active", "boolean", array("default" => 1));
    $table->setPrimaryKey(array("id"));
    $schema->createTable($table);
}