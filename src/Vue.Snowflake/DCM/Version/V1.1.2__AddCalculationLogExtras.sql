set database_name = 'vue_test';
set table_creation_role = 'sysadmin';
set user_creation_role = 'accountadmin';
set logging_role = 'test_vue_logging_role';
set logging_user = 'test_allvue_user';
set warehouse = 'warehouse_xsmall';

use database identifier($database_name);

use role identifier($user_creation_role);

create role if not exists identifier($logging_role);
create user if not exists identifier($logging_user);
grant role identifier($logging_role) to user identifier($logging_user);

use role identifier($table_creation_role);

create schema if not exists logging;

grant usage on database identifier($database_name) to role identifier($logging_role);
grant usage on schema logging to role identifier($logging_role);
grant usage on warehouse identifier($warehouse) to role identifier($logging_role);
grant insert on future tables in schema logging to role identifier($logging_role);

alter user identifier($logging_user) set
default_role = $logging_role,
rsa_public_key = 'MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA41QM8UOE0ERD+D1EpX3pRMOSgq2fZBDu5szp18a3sBADThbP+PHYl2mveY/QFksDM4zSXpGVbQzjQxCFVnNh1Rti3sVNELBOqgytOVQwM0l15cY3matJbLVh63qXX6toB7it3MW6HkMUBFoNacok6RswiFgQJdgTu0I6zmPe3FkVm64hPbCWI1r0wVzGZuvMP60kizza9JYSCWHTUQDLgGku9B/CgfCsUjAZjlqfbFNeGeZpxANBw0WV7H5D+tC/10IqRHXWpA7qLyQd1mqk/ivnoSvyE94j9w+MUTT2UvL8/TndpsfvhYbeRbGudN9Zg5J2lP0Zt6ml2IuKJ6YJQwIDAQAB';
