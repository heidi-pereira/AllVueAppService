#!/usr/bin/env python3
"""
Upload Python files to Snowflake stage using connection profiles.

Usage:
    uv run stage_python.py <connection_name> <relative_path>

Examples:
    # Upload a single file
    uv run stage_python.py live schemas/impl_variable_expression/functions/_parse_python_expression.py
    
    # Upload all .py files in a directory as a zip bundle (for UDFs with dependencies)
    uv run stage_python.py live schemas/impl_variable_expression/functions/
 
1. Reads connection details from ~/.snowflake/connections.toml
2. Connects using JWT authentication
3. For a single file: Uploads to @~/udf_stage/<relative_path> with auto-compression
4. For a directory: Creates a zip bundle of all .py files and uploads to @~/udf_stage/<relative_path>/<dirname>_bundle.zip
"""
# /// script
# requires-python = ">=3.13"
# dependencies = [
#     "snowflake-connector-python>=3.0.0",
#     "cryptography>=41.0.0",
#     "toml>=0.10.2",
# ]
# ///

import os
import sys
import traceback
import zipfile
import tempfile
from pathlib import Path

import toml
import snowflake.connector
from cryptography.hazmat.primitives import serialization


def get_connection(connection_name: str) -> snowflake.connector.SnowflakeConnection:
    """Establishes a connection to Snowflake using user profile TOML file with JWT authentication."""
    try:
        toml_path = os.path.expanduser("~/.snowflake/connections.toml")
        config = toml.load(toml_path)
        conn_info = config.get(connection_name)
        if conn_info is None:
            raise KeyError(f"Could not find a connection profile in ~/.snowflake/connections.toml with the name '{connection_name}'")

        # Load private key
        with open(conn_info["private_key_path"], "rb") as key_file:
            p_key = serialization.load_pem_private_key(
                key_file.read(),
                password=conn_info["private_key_file_pwd"].encode(),
            )
        pkb = p_key.private_bytes(
            encoding=serialization.Encoding.DER,
            format=serialization.PrivateFormat.PKCS8,
            encryption_algorithm=serialization.NoEncryption(),
        )

        return snowflake.connector.connect(
            user=conn_info["user"],
            account=conn_info["account"],
            warehouse=conn_info.get("warehouse"),
            database=conn_info.get("database"),
            authenticator=conn_info.get("authenticator"),
            role=conn_info.get("role"),
            private_key=pkb,
        )
    except KeyError as e:
        tb = traceback.format_exc()
        raise ConnectionError(f"Missing TOML config value: {e}\nStack trace:\n{tb}")
    except FileNotFoundError:
        tb = traceback.format_exc()
        raise ConnectionError(f"TOML file not found at {toml_path}\nStack trace:\n{tb}")
    except Exception as e:
        tb = traceback.format_exc()
        raise ConnectionError(f"Failed to connect: {e}\nStack trace:\n{tb}")


def _resolve_stage_base(cursor, relative_path: str) -> str:
    """Resolve the stage base by parsing the schema from 'schemas/<schema>/...'

    Behavior: Always parse the schema from the provided relative path and return
    '@<CURRENT_DATABASE>.<parsed_schema>.udf_stage'. If parsing fails or the
    current database cannot be determined, raise ValueError.
    """
    # Parse schema name from the relative path: look for 'schemas/<schema>/'
    try:
        rel = Path(relative_path)
        parts = rel.parts
        if 'schemas' not in parts:
            raise ValueError(f"Path '{relative_path}' does not contain a 'schemas/<schema>/...' segment")
        idx = parts.index('schemas')
        if idx + 1 >= len(parts):
            raise ValueError(f"Path '{relative_path}' contains 'schemas' but no following schema name")
        parsed_schema = parts[idx + 1]
    except Exception as e:
        raise ValueError(f"Failed to parse schema from path '{relative_path}': {e}")

    # Get the current database and use the parsed schema
    try:
        cursor.execute("SELECT CURRENT_DATABASE()")
        row = cursor.fetchone()
        db_name = row[0] if row and row[0] else None
        if not db_name:
            raise ValueError("Could not determine CURRENT_DATABASE() from Snowflake connection")
    except Exception as e:
        raise ValueError(f"Failed to read CURRENT_DATABASE() from Snowflake connection: {e}")

    return f"@{db_name}.{parsed_schema}.udf_stage"


def stage_file(connection_name: str, relative_path: str) -> None:
    """
    Upload a local file or directory to a Snowflake stage, mirroring the local directory structure.

    Args:
        connection_name: Name of the connection profile (e.g., 'live')
        relative_path: Relative path to the file or directory from repo root
                      (e.g., 'schemas/impl_variable_expression/functions/file.py' or 'schemas/impl_variable_expression/')
    """
    local_path = Path(relative_path)
    if not local_path.exists():
        raise FileNotFoundError(f"Local path not found: {relative_path}")

    # If it's a directory, create a zip archive of all .py files and stage that
    if local_path.is_dir():
        py_files = list(local_path.rglob("*.py"))
        if not py_files:
            print(f"No .py files found in {relative_path}")
            return

        print(f"Found {len(py_files)} .py file(s) in {relative_path}")

        # Create a zip file with a proper name
        zip_name = f"{local_path.name}_bundle.zip"
        temp_dir = Path(tempfile.gettempdir())
        zip_path = temp_dir / zip_name

        try:
            # Create zip archive with all .py files
            with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
                for py_file in py_files:
                    # Add file to zip with relative path preserved
                    arcname = py_file.relative_to(local_path)
                    print(f"  Adding {py_file.name} as {arcname}")
                    zipf.write(py_file, arcname=arcname)

            # Upload the zip file
            conn = get_connection(connection_name)
            cursor = conn.cursor()

            # Resolve stage reference (prefer schema parsed from the provided relative path)
            stage_base = _resolve_stage_base(cursor, relative_path)

            # Build stage directory path and PUT command (quote stage path for SQL)
            stage_dir = f"{stage_base}/{local_path.as_posix()}/"
            put_command = f"PUT 'file://{zip_path.as_posix()}' '{stage_dir}' auto_compress=false overwrite=true"

            print(f"\nUploading bundle to {stage_dir}{zip_name}...")
            print(f"Command: {put_command}")

            cursor.execute(put_command)
            results = cursor.fetchall()

            for row in results:
                print(f"  Status: {row}")

            cursor.close()
            conn.close()

            print(f"✓ Successfully uploaded to {stage_dir}{zip_name}")

        finally:
            # Clean up temp file
            if zip_path.exists():
                zip_path.unlink()

        return

    # Handle single file (keep existing behavior)
    if not local_path.suffix == ".py":
        print(f"Warning: {relative_path} is not a .py file, skipping")
        return

    # Ensure we're working from the directory containing the file
    original_dir = Path.cwd()
    os.chdir(local_path.parent)

    try:
        conn = get_connection(connection_name)
        cursor = conn.cursor()

        # Resolve stage reference (prefer schema parsed from the provided relative path)
        stage_base = _resolve_stage_base(cursor, relative_path)

        # Build stage directory path and construct PUT command
        stage_dir = f"{stage_base}/{local_path.parent.as_posix()}/"
        # Use just the filename since we've changed to that directory; quote the stage target
        put_command = f"PUT file://{local_path.name} '{stage_dir}' auto_compress=true overwrite=true"

        print(f"Uploading {relative_path} to {stage_dir}...")
        print(f"Command: {put_command}")

        cursor.execute(put_command)
        results = cursor.fetchall()

        for row in results:
            print(f"  Status: {row}")

        cursor.close()
        conn.close()

        print(f"✓ Successfully uploaded to {stage_dir}{local_path.name}.gz")

    finally:
        os.chdir(original_dir)


def main():
    """Main entry point."""
    if len(sys.argv) != 3:
        print(__doc__)
        print("\nError: Expected 2 arguments")
        print(f"Usage: uv run {Path(__file__).name} <connection_name> <relative_path>")
        sys.exit(1)

    connection_name = sys.argv[1]
    relative_path = sys.argv[2]

    try:
        stage_file(connection_name, relative_path)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
