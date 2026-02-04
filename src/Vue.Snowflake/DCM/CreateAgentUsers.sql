


create user if not exists test_deployment_agent;
create role if not exists role_alter_test;
--ALTER USER DEPLOYMENT_AGENT SET PASSWORD = --todo;
alter user test_deployment_agent set RSA_PUBLIC_KEY = 'MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwQlftRgBTpOvh7I/PKO/
Ix4XOU8GbKW+hQB2jH33KsF9sDnRKsQviL5ejDQWh+MpiXGh3sAD+L1h9WMuhtQi
8sy0YJovDQwYWN/RxG3hlQe+JX8vwyNuAb9hXm0CuBqWjdPVimpOLYC2r8PRQFhk
TF/bE4q6he7O8M/08T7Eqa+JLxRRPCMEpgc3eOPWeH6HElQ7ZgI2bSb6mcPnd/MX
hI5EoArWL2C1Nx4UK0+OPS4oEJvDkwaEMimCva+Ijl7/AwaGOZalYarP03upQEtm
8haUANxwiAaHbw5yPghRp/OLcb9KeDsJJ7OqvXw5bgCjgMsfDn0PhwJhdd9x53qc
AQIDAQAB';
alter user test_deployment_agent set TYPE = service;
grant role role_alter_test to user test_deployment_agent;
grant usage on database vue_test to role role_alter_test;
grant create schema on database vue_test to role role_alter_test;
grant usage on all schemas in database vue_test to role role_alter_test;
grant all privileges on all schemas in database vue_test to role role_alter_test;
grant all privileges on future schemas in database vue_test to role role_alter_test;
grant create table, create view, create procedure, create function, create sequence on all schemas in database vue_test to role role_alter_test;
grant select, insert, update, delete, truncate on all tables in database vue_test to role role_alter_test;
grant select on all views in database vue_test to role role_alter_test;
grant usage on all procedures in database vue_test to role role_alter_test;
grant usage on all functions in database vue_test to role role_alter_test;
grant usage on future schemas in database vue_test to role role_alter_test;
grant create table, create view, create procedure, create function, create sequence on future schemas in database vue_test to role role_alter_test;
grant select, insert, update, delete, truncate on future tables in database vue_test to role role_alter_test;
grant select on future views in database vue_test to role role_alter_test;
grant usage on future procedures in database vue_test to role role_alter_test;
grant usage on future functions in database vue_test to role role_alter_test;
grant usage on warehouse warehouse_xsmall to role role_alter_test;

create user if not exists test_readwrite_agent;
create role if not exists role_readwrite_test;
--ALTER USER DEPLOYMENT_AGENT SET PASSWORD = --todo;
alter user test_readwrite_agent set RSA_PUBLIC_KEY = 'MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwQlftRgBTpOvh7I/PKO/
Ix4XOU8GbKW+hQB2jH33KsF9sDnRKsQviL5ejDQWh+MpiXGh3sAD+L1h9WMuhtQi
8sy0YJovDQwYWN/RxG3hlQe+JX8vwyNuAb9hXm0CuBqWjdPVimpOLYC2r8PRQFhk
TF/bE4q6he7O8M/08T7Eqa+JLxRRPCMEpgc3eOPWeH6HElQ7ZgI2bSb6mcPnd/MX
hI5EoArWL2C1Nx4UK0+OPS4oEJvDkwaEMimCva+Ijl7/AwaGOZalYarP03upQEtm
8haUANxwiAaHbw5yPghRp/OLcb9KeDsJJ7OqvXw5bgCjgMsfDn0PhwJhdd9x53qc
AQIDAQAB';
alter user test_readwrite_agent set TYPE = service;
grant role role_readwrite_test to user test_readwrite_agent;
grant usage on database vue_test to role role_readwrite_test;
grant usage on all schemas in database vue_test to role role_readwrite_test;
grant select, insert, update, delete on all tables in database vue_test to role role_readwrite_test;
grant select on all views in database vue_test to role role_readwrite_test;
grant usage on all procedures in database vue_test to role role_readwrite_test;
grant usage on all functions in database vue_test to role role_readwrite_test;
grant usage on future schemas in database vue_test to role role_readwrite_test;
grant select, insert, update, delete on future tables in database vue_test to role role_readwrite_test;
grant select on future views in database vue_test to role role_readwrite_test;
grant usage on future procedures in database vue_test to role role_readwrite_test;
grant usage on future functions in database vue_test to role role_readwrite_test;
grant usage on warehouse warehouse_xsmall to role role_readwrite_test;
show grants to role role_readwrite_test;
show grants to user test_readwrite_agent;

create user if not exists test_readonly_agent;
create role if not exists role_readonly_test;
--ALTER USER DEPLOYMENT_AGENT SET PASSWORD = --todo;
alter user test_readonly_agent set RSA_PUBLIC_KEY = 'MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwQlftRgBTpOvh7I/PKO/
Ix4XOU8GbKW+hQB2jH33KsF9sDnRKsQviL5ejDQWh+MpiXGh3sAD+L1h9WMuhtQi
8sy0YJovDQwYWN/RxG3hlQe+JX8vwyNuAb9hXm0CuBqWjdPVimpOLYC2r8PRQFhk
TF/bE4q6he7O8M/08T7Eqa+JLxRRPCMEpgc3eOPWeH6HElQ7ZgI2bSb6mcPnd/MX
hI5EoArWL2C1Nx4UK0+OPS4oEJvDkwaEMimCva+Ijl7/AwaGOZalYarP03upQEtm
8haUANxwiAaHbw5yPghRp/OLcb9KeDsJJ7OqvXw5bgCjgMsfDn0PhwJhdd9x53qc
AQIDAQAB';
alter user test_readonly_agent set TYPE = service;
grant role role_readonly_test to user test_readonly_agent;
grant usage on database vue_test to role role_readonly_test;
grant usage on all schemas in database vue_test to role role_readonly_test;
grant select on all tables in database vue_test to role role_readonly_test;
grant select on all views in database vue_test to role role_readonly_test;
grant usage on all procedures in database vue_test to role role_readonly_test;
grant usage on all functions in database vue_test to role role_readonly_test;
grant usage on future schemas in database vue_test to role role_readonly_test;
grant select on future tables in database vue_test to role role_readonly_test;
grant select on future views in database vue_test to role role_readonly_test;
grant usage on future procedures in database vue_test to role role_readonly_test;
grant usage on future functions in database vue_test to role role_readonly_test;
grant usage on warehouse warehouse_xsmall to role role_readonly_test;
show grants to role role_readonly_test;
show grants to user test_readonly_agent;
