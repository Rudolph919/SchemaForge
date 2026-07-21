#!/bin/sh
# Runs once, on first container init (docker-entrypoint-initdb.d), as the POSTGRES_USER superuser.
# Creates a restricted, non-superuser role for the running application - distinct from the
# superuser role migrations use. This is not optional hardening: Postgres superusers (and any
# BYPASSRLS role) always bypass Row-Level Security regardless of FORCE ROW LEVEL SECURITY, so if
# the app connected as the same superuser that runs migrations, every RLS policy would be a no-op
# with no error or warning to reveal it - confirmed by direct testing while building the first
# migration (see the identity vertical slice PR description).
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    DO \$\$
    BEGIN
        IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$POSTGRES_APP_USER') THEN
            CREATE ROLE "$POSTGRES_APP_USER" WITH LOGIN PASSWORD '$POSTGRES_APP_PASSWORD' NOSUPERUSER NOBYPASSRLS;
        END IF;
    END
    \$\$;

    GRANT CONNECT ON DATABASE "$POSTGRES_DB" TO "$POSTGRES_APP_USER";
    -- Also lets the app role CREATE SCHEMA - needed once Hangfire (Step 1 §8) is wired up, since
    -- it installs its own tables into a dedicated "hangfire" schema on first run, using this same
    -- app connection. Everything the app creates from here on (Hangfire's own tables included)
    -- stays owned by this role, so no further grant is needed inside that schema.
    GRANT CREATE ON DATABASE "$POSTGRES_DB" TO "$POSTGRES_APP_USER";
    GRANT USAGE ON SCHEMA public TO "$POSTGRES_APP_USER";

    -- Tables don't exist yet at first-init time (migrations haven't run) - default privileges
    -- apply automatically to tables the migration role creates from this point forward, so no
    -- second grant step is needed after `dotnet ef database update` runs.
    ALTER DEFAULT PRIVILEGES FOR ROLE "$POSTGRES_USER" IN SCHEMA public
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "$POSTGRES_APP_USER";
    ALTER DEFAULT PRIVILEGES FOR ROLE "$POSTGRES_USER" IN SCHEMA public
        GRANT USAGE, SELECT ON SEQUENCES TO "$POSTGRES_APP_USER";
EOSQL
