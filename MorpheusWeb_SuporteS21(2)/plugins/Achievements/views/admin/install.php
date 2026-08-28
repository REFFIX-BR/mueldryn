<?php

$schema = Connection::getSchemaManager();

if (!$schema->tablesExist("mw_achievements")) {
    $table = new Doctrine\DBAL\Schema\Table("mw_achievements");
    $table->addColumn("id", "integer", array("autoincrement" => true));
    $table->addColumn("name", "string", array("length" => 255));
    $table->addColumn("description", "text", array("notnull" => false));
    $table->addColumn("required_amount", "integer", array("default" => 1));
    $table->addColumn("active", "boolean", array("default" => 1));
    $table->addColumn("requirements", "text", array("notnull" => false));
    $table->addColumn("rewards", "text", array("notnull" => false));
    $table->setPrimaryKey(array("id"));
    $schema->createTable($table);
}