#!/usr/bin/env bash

# Always use the environment variables stored in this file for each command
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export UV_ENV_FILE=$SCRIPT_DIR/.env.test

# Target directory for the Airflow environment - this make UV MUCH faster as it doesn't need to use the slow Windows layer
export UV_PROJECT_ENVIRONMENT=~/airflow-venv

# Initialise the DB
uv run airflow db migrate

# Import dags
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export AIRFLOW__CORE__DAGS_FOLDER="$SCRIPT_DIR/Airflow/dags"
uv run airflow dag-processor -n 1

# Get SF test key
export SNOWFLAKE_AGENT_PRIVATE_KEY=$(az keyvault secret show --name AKS-AIRFLOW-SNOWFLAKE-AGENT-PRIVATE-KEY --vault-name bv-airflow-vault --query value -o tsv | tr -d '\r')

if [[ -z "${SNOWFLAKE_AGENT_PRIVATE_KEY:-}" ]]; then
	echo "Failed to retrieve Snowflake test key from Key Vault - are you logged in as your .admin user?" >&2
	exit 1
fi

# Get blob SAS token
export BLOB_EXPORT_SAS_TOKEN=$(az keyvault secret show --name AKS-AIRFLOW-BLOB-EXPORT-SAS-TOKEN --vault-name bv-airflow-vault --query value -o tsv | tr -d '\r')

if [[ -z "${BLOB_EXPORT_SAS_TOKEN:-}" ]]; then
	echo "Failed to retrieve Blob SAS token from Key Vault - are you logged in as your .admin user?" >&2
	exit 1
fi

# Get SQL test password
MSSQL_PASSWORD=$(az keyvault secret show --name AKS-AIRFLOW-MSSQL-PASSWORD --vault-name bv-airflow-vault --query value -o tsv | tr -d '\r')

if [[ -z "${MSSQL_PASSWORD:-}" ]]; then
	echo "Failed to retrieve MSSQL password from Key Vault - are you logged in as your .admin user?" >&2
	exit 1
fi

CONN_ID="test_mssql"

# If the connection already exists, delete it so we can recreate with fresh credentials
if uv run airflow connections get "$CONN_ID" >/dev/null 2>&1; then
	echo "Existing Airflow connection '$CONN_ID' found; deleting before recreation."
	# Don't fail script if delete fails
	uv run airflow connections delete "$CONN_ID" >/dev/null 2>&1 || echo "(Warn) Failed to delete existing connection '$CONN_ID'" >&2
fi

echo "Creating Airflow connection '$CONN_ID'"
uv run airflow connections add "$CONN_ID" \
	--conn-host='sqltest.svtazure.savanta.com\sql2019' \
	--conn-type='mssql' \
	--conn-login='airflow_user' \
	--conn-password="$MSSQL_PASSWORD" >/dev/null 2>&1 && echo "Connection '$CONN_ID' created/updated." || echo "(Error) Failed to create connection '$CONN_ID'" >&2

echo "Testing Airflow connection '$CONN_ID'"
uv run airflow connections test "$CONN_ID" >/dev/null 2>&1 && echo "Connection '$CONN_ID' is working." || echo "(Error) Connection '$CONN_ID' failed." >&2

