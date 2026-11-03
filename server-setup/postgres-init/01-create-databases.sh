#!/bin/bash
# Läuft nur beim allerersten Start, solange das Datenbank-Volume leer ist.
set -e

psql -v ON_ERROR_STOP=1 --username postgres <<EOSQL
CREATE USER keycloak WITH PASSWORD '${KC_DB_PASSWORD}';
CREATE DATABASE keycloak OWNER keycloak;
CREATE USER app WITH PASSWORD '${APP_DB_PASSWORD}';
CREATE DATABASE app OWNER app;
EOSQL
