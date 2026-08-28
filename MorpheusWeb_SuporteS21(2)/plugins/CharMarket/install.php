<?php
$conn = Connection::getInstance();
$schema = $conn->getSchemaManager();

if (!$schema->tablesExist('mw_market_chars')) {
    $table = new Doctrine\DBAL\Schema\Table('mw_market_chars');
    $table->addColumn('id', 'integer', array('autoincrement' => true));
    $table->addColumn('seller', 'string', array('length' => 50));
    $table->addColumn('character_name', 'string', array('length' => 50));
    $table->addColumn('price', 'integer');
    $table->addColumn('coin_id', 'string', array('length' => 50));
    $table->addColumn('price_jb', 'integer', array('default' => 0));
    $table->addColumn('price_js', 'integer', array('default' => 0));
    $table->addColumn('price_jl', 'integer', array('default' => 0));
    $table->addColumn('price_jcr', 'integer', array('default' => 0));
    $table->addColumn('price_jc', 'integer', array('default' => 0));
    $table->addColumn('created_at', 'datetime');
    $table->setPrimaryKey(array('id'));
    $schema->createTable($table);
} else {
    // Migração: adiciona colunas de joias se faltarem
    foreach (array('price_jb', 'price_js', 'price_jl', 'price_jcr', 'price_jc') as $col) {
        try {
            $conn->executeUpdate("ALTER TABLE mw_market_chars ADD {$col} INT NOT NULL DEFAULT 0");
        } catch (Exception $e) {
            // já existe
        }
    }
}
