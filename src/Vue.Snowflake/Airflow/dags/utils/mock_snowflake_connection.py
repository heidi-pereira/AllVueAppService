from snowflake.snowpark import Session
from contextlib import contextmanager

@contextmanager
def get_local_snowflake_connection(useDeploymentAgent):
    session = Session.builder.config('local_testing', True).create()
    try:
        yield session
    finally:
        session.close()