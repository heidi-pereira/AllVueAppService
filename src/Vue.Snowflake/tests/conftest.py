import pytest
import os
from snowflake.snowpark.session import Session
from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import serialization
from dotenv import load_dotenv

load_dotenv(dotenv_path=os.path.join(os.path.dirname(__file__), '../.env.test'))

def pytest_addoption(parser):
    parser.addoption(
        "--snowflake-session",
        action="store",
        default="remote",
        help="Set to 'local' to use local Snowflake session for testing"
    )

@pytest.fixture(scope='session')
def session(request) -> Session:
    if request.config.getoption('--snowflake-session') == 'local':
        return Session.builder.configs({'local_testing': True}).create()
    else:
        private_key_path = os.environ.get('SNOWFLAKE_PRIVATE_KEY_PATH')
        private_key_pwd = os.environ.get('SNOWFLAKE_PRIVATE_KEY_FILE_PWD')
        with open(private_key_path, "rb") as key_file:
            private_key = serialization.load_pem_private_key(
                key_file.read(),
                password=private_key_pwd.encode('utf-8') if private_key_pwd else None,
                backend=default_backend()
            )

        pkb = private_key.private_bytes(
            encoding=serialization.Encoding.DER,
            format=serialization.PrivateFormat.PKCS8,
            encryption_algorithm=serialization.NoEncryption()
        )
        
        account = os.environ.get('SNOWFLAKE_ACCOUNT')
        user = os.environ.get('SNOWFLAKE_USER')
        password = os.environ.get('SNOWFLAKE_PASSWORD')
        role = os.environ.get('SNOWFLAKE_ROLE')
        warehouse = os.environ.get('SNOWFLAKE_WAREHOUSE')
        database = os.environ.get('SNOWFLAKE_DATABASE')
        schema = os.environ.get('SNOWFLAKE_SCHEMA')
        region = os.environ.get('SNOWFLAKE_REGION')
        
        connection_parameters = {
            'account': account,
            'user': user,
            'password': password,
            'role': role,
            'warehouse': warehouse,
            'database': database,
            'schema': schema,
            'region': region,
            'private_key': pkb
        }
        return Session.builder.configs(connection_parameters).create()
