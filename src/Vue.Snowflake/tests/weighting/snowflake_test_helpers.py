import os

_db_checked = False

def deploy_snowflake_sql_object(session, object_name, sql_relative_path, env_flag="SNOWFLAKE_DEPLOY_TEST_OBJECTS"):
    """
    Deploy a Snowflake SQL object (e.g., sproc, UDF) from a SQL file if the environment flag is set.
    Args:
        session: Snowflake session object
        object_name: Name of the object being deployed (for logging)
        sql_relative_path: Path to the SQL file, relative to the test file
        env_flag: Environment variable name to control deployment
    """
    global _db_checked
    temp_db = os.environ.get("SNOWFLAKE_TEMP_DATABASE")
    if temp_db and not _db_checked:
        try:
            session.sql(f"CREATE DATABASE IF NOT EXISTS {temp_db}").collect()
            session.sql(f"USE DATABASE {temp_db}").collect()
            _db_checked = True
        except Exception as e:
            print(f"Error ensuring database {temp_db}: {e}")
    if os.environ.get(env_flag, "False") == "True":
        sql_file = os.path.abspath(os.path.join(os.path.dirname(__file__), sql_relative_path))
        if os.path.exists(sql_file):
            print(f"Deploying {object_name} from: {sql_file}")
            with open(sql_file, "r", encoding="utf-8") as f:
                sql_content = f.read()
                try:
                    session.sql(sql_content).collect()
                    print(f"{object_name} deployed successfully.")
                except Exception as e:
                    print(f"Error deploying {object_name}: {e}")
        else:
            print(f"{object_name} SQL file not found at: {sql_file}")
