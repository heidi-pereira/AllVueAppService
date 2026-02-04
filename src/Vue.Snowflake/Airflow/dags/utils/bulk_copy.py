"""Generic PolyBase CETAS export helper.

Provides a function to export the result of an arbitrary SELECT statement to
Azure Blob Storage as CSV files using PolyBase CETAS. A transient helper
database is created for isolation, external objects are created inside it,
the CETAS runs, and the database is dropped – leaving only the exported
files in Blob Storage.

Intended usage inside Airflow DAG tasks:

    from utils.bulk_copy import export_query_via_cetas
    export_query_via_cetas(
        mssql_conn_id=MSSQL_CONN_ID,
        query_name="responsecounts",
        select_sql="SELECT responseid, answerCount FROM [SurveyDB].[vue].[ResponseCounts]",
        folder_prefix=f"responsecounts/{run_id}")

Environment variables required:
    POLYBASE_AZURE_BLOB_ACCOUNT_NAME
    POLYBASE_AZURE_BLOB_CONTAINER
    BLOB_EXPORT_SAS_TOKEN (SAS without or with leading '?')
    POLYBASE_DB_MASTER_KEY_PASSWORD (password for creating master key)

Notes:
* The helper database name pattern: TMP_CETAS_<QUERYNAME>_<UTCYYYYMMDDHHMMSS>
* External objects created:
    - MASTER KEY (if missing)
    - DATABASE SCOPED CREDENTIAL AzureStorageCredential (if missing)
    - EXTERNAL DATA SOURCE CETAS_BLOB_DS
    - EXTERNAL FILE FORMAT CETAS_CSV_FF (comma, quoted strings, no header)
* CETAS external table name: EXT_<QUERYNAME>
* The folder_prefix is used as LOCATION under the container; ensure uniqueness per run.
* The database is dropped in a finally block even if CETAS fails (best effort).
"""
from __future__ import annotations

import os
import re
import logging
import secrets
import string
import time
from typing import Set
import xml.etree.ElementTree as ET
from urllib.parse import quote
try:
    import requests  # type: ignore
except Exception:  # pragma: no cover - requests should exist in Airflow env; fallback minimal stdlib
    requests = None  # sentinel; we'll log a warning if cleanup attempted without requests
from datetime import datetime, timezone
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
from utils.snowflake_utils import get_snowflake_connection
try:
    from utils.config import ENVIRONMENT  # Expected to be something like 'dev', 'qa', 'prod'
except Exception:  # Fallback if not available
    ENVIRONMENT = os.environ.get('ENVIRONMENT', '').strip() or 'local'


class CetasExportError(RuntimeError):
    """Raised when CETAS export fails."""


def _sanitize_name(name: str) -> str:
    return re.sub(r"[^A-Za-z0-9_]", "_", name)[:100]


def export_query_via_cetas(*, mssql_conn_id: str, query_name: str, select_sql: str, folder_prefix: str) -> dict:
    """Export a SELECT query to Azure Blob Storage via PolyBase CETAS.

    Parameters
    ----------
    mssql_conn_id : str
        Airflow connection ID for SQL Server (must point to server; initial DB irrelevant).
    query_name : str
        Logical name of the query (used in transient DB & external table names).
    select_sql : str
        A SELECT statement (no trailing semicolon required) producing tabular results.
    folder_prefix : str
        Path prefix (virtual folder) inside the target container for the generated CSV files.

    Returns
    -------
    dict with keys: 'folder_prefix', 'external_table', 'database'
    """
    account = os.environ.get('POLYBASE_AZURE_BLOB_ACCOUNT_NAME')
    container = os.environ.get('POLYBASE_AZURE_BLOB_CONTAINER')
    sas = os.environ.get('BLOB_EXPORT_SAS_TOKEN')
    master_key_pwd = os.environ.get('POLYBASE_DB_MASTER_KEY_PASSWORD')

    def _is_complex(p: str) -> bool:
        if not p or len(p) < 12:
            return False
        classes = [any(c.islower() for c in p), any(c.isupper() for c in p), any(c.isdigit() for c in p), any(c in string.punctuation for c in p)]
        return sum(classes) >= 3  # typical SQL Server policy (adjust if needed)

    def _generate_strong_password(length: int = 24) -> str:
        alphabet = string.ascii_lowercase + string.ascii_uppercase + string.digits + "!@#$%^&*-_=+?"  # avoid quotes
        while True:
            pwd = ''.join(secrets.choice(alphabet) for _ in range(length))
            if _is_complex(pwd):
                return pwd

    # If supplied password fails complexity heuristic, generate an ephemeral one (cannot bypass server policy)
    if master_key_pwd and not _is_complex(master_key_pwd):
        logging.warning("Provided POLYBASE_DB_MASTER_KEY_PASSWORD appears not to meet complexity policy; generating a strong ephemeral password instead.")
        master_key_pwd = _generate_strong_password()
    elif not master_key_pwd:
        # Allow auto-generation rather than failing fast; still note missing env
        logging.warning("POLYBASE_DB_MASTER_KEY_PASSWORD not set; generating a strong ephemeral password. Set env var to control this explicitly.")
        master_key_pwd = _generate_strong_password()

    if not all([account, container, sas, master_key_pwd]):
        missing = [k for k, v in {
            'POLYBASE_AZURE_BLOB_ACCOUNT_NAME': account,
            'POLYBASE_AZURE_BLOB_CONTAINER': container,
            'POLYBASE_AZURE_BLOB_SAS_TOKEN': sas,
            'POLYBASE_DB_MASTER_KEY_PASSWORD': master_key_pwd,
        }.items() if not v]
        raise CetasExportError(f"Missing required environment variables: {', '.join(missing)}")

    if sas.startswith('?'):
        sas = sas[1:]

    timestamp = datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S')
    sanitized_query = _sanitize_name(query_name).upper()
    db_name = f"TMP_CETAS_{sanitized_query}_{timestamp}"
    external_table = f"EXT_{sanitized_query}"

    # Derive effective folder prefix with ENVIRONMENT suffix applied to top-level directory.
    # Example: input 'responsecounts/20250902_120000' + ENVIRONMENT=dev -> 'responsecounts_dev/20250902_120000'
    original_folder_prefix = folder_prefix.strip('/')
    parts = original_folder_prefix.split('/', 1)
    top = parts[0]
    rest = parts[1] if len(parts) > 1 else ''
    env_suffix = ENVIRONMENT.lower().replace(' ', '_') if ENVIRONMENT else 'env'
    if not top.lower().endswith(f"_{env_suffix}"):
        top_effective = f"{top}_{env_suffix}"
    else:
        top_effective = top  # already suffixed
    effective_folder_prefix = top_effective if not rest else f"{top_effective}/{rest}"
    if effective_folder_prefix != folder_prefix:
        logging.info(f"Applying environment suffix to folder prefix: '{folder_prefix}' -> '{effective_folder_prefix}' (ENVIRONMENT={ENVIRONMENT})")
    folder_prefix = effective_folder_prefix  # overwrite for downstream usage

    logging.info(f"Starting CETAS export: query_name={query_name}, db={db_name}, folder_prefix={folder_prefix}")

    hook = MsSqlHook(mssql_conn_id=mssql_conn_id)

    # Build statements executed against server-level (DB creation) then within DB context.
    create_db = f"CREATE DATABASE [{db_name}];"
    drop_db = f"DROP DATABASE IF EXISTS [{db_name}];"  # final cleanup

    # External object creation (executed with USE [db]).
    use_db = f"USE [{db_name}];"
    # NOTE: password purposely not logged; do NOT include master_key_pwd in logs.
    master_key_sql = (
        "IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##') "
        f"CREATE MASTER KEY ENCRYPTION BY PASSWORD = '{master_key_pwd}';"
    )
    credential_sql = (
        "IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = 'AzureStorageCredential') "
        f"CREATE DATABASE SCOPED CREDENTIAL AzureStorageCredential WITH IDENTITY='SHARED ACCESS SIGNATURE', SECRET='?{sas}';"
    )
    data_source_sql = (
        "IF NOT EXISTS (SELECT * FROM sys.external_data_sources WHERE name = 'CETAS_BLOB_DS') "
        f"CREATE EXTERNAL DATA SOURCE CETAS_BLOB_DS WITH ( LOCATION='abs://{container}@{account}.blob.core.windows.net', CREDENTIAL=AzureStorageCredential );"
    )
    # Recreate external file format each run to pick up option changes (must split DROP & CREATE for some drivers)
    drop_file_format_sql = (
        "IF EXISTS (SELECT 1 FROM sys.external_file_formats WHERE name = 'CETAS_CSV_FF') "
        "DROP EXTERNAL FILE FORMAT CETAS_CSV_FF;"
    )
    create_file_format_sql = (
        "CREATE EXTERNAL FILE FORMAT CETAS_CSV_FF WITH ( "
        "FORMAT_TYPE = DELIMITEDTEXT, "
        "FORMAT_OPTIONS ( FIELD_TERMINATOR = ',', STRING_DELIMITER='\"', USE_TYPE_DEFAULT=TRUE ) );"
    )

    # Drop external table if re-run (idempotency within same db)
    drop_ext_table_sql = f"IF EXISTS (SELECT 1 FROM sys.external_tables WHERE name = '{external_table}') DROP EXTERNAL TABLE {external_table};"

    # Ensure SELECT only (basic guard)
    if not re.match(r"^\s*select\b", select_sql, re.IGNORECASE):
        raise CetasExportError("select_sql must start with SELECT")

    cetas_sql = (
    f"CREATE EXTERNAL TABLE {external_table} WITH ( LOCATION = '{folder_prefix}/', DATA_SOURCE = CETAS_BLOB_DS, FILE_FORMAT = CETAS_CSV_FF ) AS "
        f"{select_sql.strip().rstrip(';')};"
    )

    try:
        # ------------------------------------------------------------------
        # Pre-cleanup: remove any existing blobs (and snapshots) under prefix
        # ------------------------------------------------------------------
        def _cleanup_existing_cetas_blobs():
            # Delete all blobs under the parent directory (top-level segment) of the requested folder_prefix.
            # Example: folder_prefix='responsecounts/2025_09_02T12_00_00' -> we purge everything under 'responsecounts/'.
            # If no slash in folder_prefix, we just purge that single directory scope.
            requested = folder_prefix.strip('/')
            top_level = requested.split('/', 1)[0]
            # Safety: basic validation – avoid deleting at container root accidentally.
            if not top_level or top_level in ('.', '..'):
                logging.warning(f"Refusing to perform wide delete; suspicious top-level segment derived from folder_prefix='{folder_prefix}'.")
                return
            # Normalized deletion scope
            prefix = top_level + '/'
            if top_level != requested:
                logging.info(f"Wide CETAS cleanup requested: removing all prior exports under parent directory '{prefix}' (original run folder '{requested}/').")
            else:
                logging.info(f"CETAS cleanup: removing prior exports under directory '{prefix}'.")
            if not requests:
                logging.warning("requests library unavailable; skipping pre-CETAS blob cleanup.")
                return
            base_url = f"https://{account}.blob.core.windows.net/{container}"
            list_url = f"{base_url}?{sas}&restype=container&comp=list&prefix={quote(prefix)}&include=snapshots"
            try:
                logging.info(f"Listing existing blobs for cleanup under prefix '{prefix}'")
                resp = requests.get(list_url, timeout=30)
                if resp.status_code != 200:
                    logging.warning(f"List blobs failed (status {resp.status_code}); skipping cleanup. Body: {resp.text[:300]}")
                    return
                # Parse XML and collect unique blob names
                blob_names: Set[str] = set()
                try:
                    root = ET.fromstring(resp.text)
                    for blob in root.findall('.//Blob'):
                        name_el = blob.find('Name')
                        if name_el is not None and name_el.text:
                            blob_names.add(name_el.text)
                except ET.ParseError:
                    logging.warning("Failed to parse blob list XML; skipping cleanup.", exc_info=True)
                    return
                if not blob_names:
                    logging.info("No existing blobs to clean up under prefix.")
                    return
                logging.info(f"Deleting {len(blob_names)} existing blob(s) (including snapshots) under prefix '{prefix}'")
                deleted = 0
                for name in blob_names:
                    # Safety: only delete within the intended prefix
                    if not name.startswith(prefix):
                        logging.warning(f"Skipping blob outside expected prefix: {name}")
                        continue
                    delete_url = f"{base_url}/{quote(name)}?{sas}"
                    headers = {
                        'x-ms-version': '2021-12-02',
                        'x-ms-delete-snapshots': 'include'
                    }
                    try:
                        dresp = requests.delete(delete_url, headers=headers, timeout=30)
                        if dresp.status_code in (202, 204):
                            deleted += 1
                        elif dresp.status_code == 404:
                            # Already gone (benign race)
                            pass
                        else:
                            logging.warning(f"Failed to delete blob {name} (status {dresp.status_code}): {dresp.text[:200]}")
                    except Exception:
                        logging.warning(f"Exception deleting blob {name}", exc_info=True)
                    # Be polite to service (optional small sleep for large sets)
                    if deleted and deleted % 50 == 0:
                        time.sleep(0.2)
                logging.info(f"Blob cleanup complete. Deleted {deleted}/{len(blob_names)} blobs.")
            except Exception:
                logging.warning("Unexpected error during blob cleanup; proceeding without failing CETAS.", exc_info=True)

        _cleanup_existing_cetas_blobs()

        # Create database (must NOT be inside an open transaction)
        logging.info(f"Creating helper database {db_name}")
        hook.run(create_db, autocommit=True)

        # Execute all setup + CETAS (each autocommit). Some drivers/hooks may not retain DB context
        # after a separate USE call when autocommit=True, so we inline USE per statement to guarantee
        # they run inside the transient database (avoiding 33158 '...not supported in master database').
        statements = [master_key_sql, credential_sql, data_source_sql, drop_file_format_sql, create_file_format_sql, drop_ext_table_sql, cetas_sql]
        for stmt in statements:
            logging.info(f"Executing in {db_name}: {stmt}")
            hook.run(f"{use_db} {stmt}", autocommit=True)

        logging.info("CETAS export completed successfully.")
        return {"folder_prefix": folder_prefix, "external_table": external_table, "database": db_name}
    except Exception as exc:
        logging.error("CETAS export failed", exc_info=True)
        raise CetasExportError(str(exc)) from exc
    finally:
        try:
            # Switch to master to be able to drop the transient database safely
            logging.info(f"Dropping helper database {db_name}")
            hook.run(f"USE master; {drop_db}", autocommit=True)
        except Exception:
            logging.warning(f"Failed to drop helper database {db_name}; manual cleanup may be required.", exc_info=True)


def load_cetas_export_into_snowflake(*, query_name: str, folder_prefix: str, snowflake_database: str,
                                     snowflake_schema: str, target_table: str, columns: list[dict],
                                     use_deployment_agent: bool = True, skip_header: bool = False) -> int:
    """Loads previously CETAS-exported CSV files from Azure Blob directly into Snowflake without downloading.

    Steps:
      1. Ensure external stage (with SAS token) exists / is replaced.
      2. Create target table if missing using provided column metadata.
      3. Truncate target table (full snapshot semantics) and COPY INTO from stage + folder prefix.

    Parameters
    ----------
    query_name : str
        Logical query identifier used to derive stage name.
    folder_prefix : str
        Path inside the container where CETAS wrote CSV files (no leading slash, trailing slash optional).
    snowflake_database : str
        Target Snowflake database name.
    snowflake_schema : str
        Target schema name.
    target_table : str
        Fully-qualified target table name (DATABASE.SCHEMA.TABLE) or just SCHEMA.TABLE? (Expect fully qualified.)
    columns : list[dict]
        List of {'name': <UPPER_COL>, 'type': <SNOWFLAKE_TYPE>, 'nullable': bool}
    use_deployment_agent : bool
        Whether to use deployment agent creds (default True) for connection.
    skip_header : bool
        If True, Snowflake COPY will skip the first line of each file (used when we manually injected a header row
        into the CETAS export via a UNION ALL construct). Default False preserves existing behaviour.

    Returns
    -------
    int : Number of loaded files (Snowflake doesn't always return rowcount for COPY; we return files matched).
    """
    account = os.environ.get('POLYBASE_AZURE_BLOB_ACCOUNT_NAME')
    container = os.environ.get('POLYBASE_AZURE_BLOB_CONTAINER')
    sas = os.environ.get('BLOB_EXPORT_SAS_TOKEN')
    if not all([account, container, sas]):
        missing = [k for k, v in {
            'POLYBASE_AZURE_BLOB_ACCOUNT_NAME': account,
            'POLYBASE_AZURE_BLOB_CONTAINER': container,
            'BLOB_EXPORT_SAS_TOKEN': sas
        }.items() if not v]
        raise CetasExportError(f"Missing env vars for Snowflake load: {', '.join(missing)}")

    if sas.startswith('?'):
        sas = sas[1:]

    sanitized_query = _sanitize_name(query_name).upper()
    stage_name = f"{snowflake_schema}.EXT_{sanitized_query}_STAGE"
    url = f"azure://{account}.blob.core.windows.net/{container}"

    # Normalize folder prefix
    folder = folder_prefix.strip('/') + '/'

    with get_snowflake_connection(use_deployment_agent) as conn:
        cur = conn.cursor()
        # (Re)create stage with latest SAS (CREATE OR REPLACE to avoid stale creds)
        file_format_options = "TYPE=CSV FIELD_DELIMITER=',' FIELD_OPTIONALLY_ENCLOSED_BY='\"' NULL_IF=('NULL')"
        if skip_header:
            # We prefer to embed SKIP_HEADER at stage level so any future COPYs inherit it unless overridden.
            file_format_options += " SKIP_HEADER=1"
        create_stage_sql = (
            f"CREATE OR REPLACE STAGE {stage_name} "
            f"URL='{url}' CREDENTIALS=(AZURE_SAS_TOKEN='{sas}') "
            f"FILE_FORMAT=({file_format_options})"
        )
        logging.info(f"Creating/replacing stage: {create_stage_sql}")
        cur.execute(create_stage_sql)

        # Create table if missing
        col_defs = ', '.join(
            f'"{c['name']}" {c['type']} {'' if c['nullable'] else 'NOT NULL'}' for c in columns
        )
        create_table_sql = f"CREATE TRANSIENT TABLE IF NOT EXISTS {target_table} ({col_defs})"
        logging.info(f"Ensuring target table exists: {create_table_sql}")
        cur.execute(create_table_sql)

        # Truncate target for snapshot
        cur.execute(f"TRUNCATE TABLE {target_table}")

        # COPY
        # We still include FILE_FORMAT in COPY for explicitness; if skip_header True we set it here too (defensive).
        copy_file_format = "TYPE=CSV FIELD_DELIMITER=',' FIELD_OPTIONALLY_ENCLOSED_BY='\"' NULL_IF=('NULL')"
        if skip_header:
            copy_file_format += " SKIP_HEADER=1"
        copy_sql = (
            f"COPY INTO {target_table} FROM @{stage_name}/{folder} "
            f"FILE_FORMAT=({copy_file_format}) "
            "ON_ERROR='ABORT_STATEMENT'"
        )
        logging.info(f"Executing COPY: {copy_sql}")
        cur.execute(copy_sql)
        logging.info("Snowflake COPY completed.")

        # Attempt to get number of files / rows loaded from results (Snowflake returns per file stats)
        try:
            results = cur.fetchall()
            file_count = len(results)
        except Exception:
            file_count = -1
        return file_count
