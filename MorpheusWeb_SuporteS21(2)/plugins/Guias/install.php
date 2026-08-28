<?php

// Cria a tabela de Categorias
Connection::executeUpdate("
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='mw_guides_categories' and xtype='U')
    CREATE TABLE mw_guides_categories (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name VARCHAR(255) NOT NULL,
        active TINYINT DEFAULT 1
    )
");

// Cria a tabela de Guias
Connection::executeUpdate("
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='mw_guides' and xtype='U')
    CREATE TABLE mw_guides (
        id INT IDENTITY(1,1) PRIMARY KEY,
        category_id INT NOT NULL,
        title VARCHAR(255) NOT NULL,
        content TEXT NOT NULL,
        active TINYINT DEFAULT 1
    )
");
