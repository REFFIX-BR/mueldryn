<?php

$schema = Connection::getSchemaManager();

if (!$schema->tablesExist("mw_event_categories")) {
    $table = new \Doctrine\DBAL\Schema\Table("mw_event_categories");
    $table->addColumn("id", "integer", array("autoincrement" => true));
    $table->addColumn("name", "string", array("length" => 255));
    $table->addColumn("color", "string", array("length" => 50, "default" => "#3bafda", "notnull" => false));
    $table->setPrimaryKey(array("id"));
    $schema->createTable($table);
}

if (!$schema->tablesExist("mw_events")) {
    $table = new \Doctrine\DBAL\Schema\Table("mw_events");
    $table->addColumn("id", "integer", array("autoincrement" => true));
    $table->addColumn("category_id", "integer");
    $table->addColumn("name", "string", array("length" => 255));
    $table->addColumn("days", "string", array("length" => 255, "notnull" => false));
    $table->addColumn("times", "text", array("notnull" => false));
    $table->addColumn("active", "boolean", array("default" => 1));
    $table->setPrimaryKey(array("id"));
    $table->addForeignKeyConstraint($schema->listTableDetails("mw_event_categories"), array("category_id"), array("id"), array("onDelete" => "CASCADE"));
    $schema->createTable($table);
}