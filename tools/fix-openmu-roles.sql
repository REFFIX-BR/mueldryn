-- Recria senhas dos roles OpenMU (ConnectionSettings.xml padrao)
-- Rode apos restore: docker exec -i CID psql -U postgres -d openmu < fix-openmu-roles.sql

DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'config') THEN
    CREATE ROLE config WITH LOGIN PASSWORD 'config';
  ELSE
    ALTER ROLE config WITH LOGIN PASSWORD 'config';
  END IF;

  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'account') THEN
    CREATE ROLE account WITH LOGIN PASSWORD 'account';
  ELSE
    ALTER ROLE account WITH LOGIN PASSWORD 'account';
  END IF;

  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'friend') THEN
    CREATE ROLE friend WITH LOGIN PASSWORD 'friend';
  ELSE
    ALTER ROLE friend WITH LOGIN PASSWORD 'friend';
  END IF;

  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'guild') THEN
    CREATE ROLE guild WITH LOGIN PASSWORD 'guild';
  ELSE
    ALTER ROLE guild WITH LOGIN PASSWORD 'guild';
  END IF;
END $$;

GRANT USAGE ON SCHEMA config TO config;
GRANT SELECT ON ALL TABLES IN SCHEMA config TO config;
GRANT USAGE ON SCHEMA data TO config;
GRANT SELECT ON ALL TABLES IN SCHEMA data TO config;

GRANT USAGE ON SCHEMA data TO account;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA data TO account;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA data TO account;

GRANT USAGE ON SCHEMA friend TO friend;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA friend TO friend;

GRANT USAGE ON SCHEMA guild TO guild;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA guild TO guild;
