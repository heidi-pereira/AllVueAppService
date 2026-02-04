-- Initialization procedure for derived variables system
create or replace procedure impl_variable_expression.init()
returns string
language sql
as
$$
begin

    -- Place to put the python
    create stage if not exists impl_variable_expression.udf_stage 
        directory = ( enable = true ) 
        encryption = ( type = 'snowflake_sse' );

    -- Ensure up to date data in the dynamic tables before initial population
    alter dynamic table impl_variable_expression.derived_variables refresh;
    
    alter dynamic table impl_variable_expression._variable_availability_state refresh;

    alter task impl_variable_expression._init_derived_variable_answers resume;

/* First time setup. Not sure where to put this.
    -- Ideal: task runner not having the live__vue__owner__d_role directly, but the task still to be created by it and grant permission to the runner role.
    -- Actual: task runner has live__vue__owner__d_role directly

    use role accountadmin;
    create user live__vue__task_runner;
    create role live__vue__task_runner__u_role;
    grant role live__vue__task_runner__u_role to user live__vue__task_runner;
    grant impersonate on user live__vue__task_runner to role live__vue__task_runner__u_role;
    grant usage on warehouse warehouse_xsmall to role live__vue__task_runner__u_role;
    grant usage on database live__vue to role live__vue__task_runner__u_role;
    grant usage on all schemas in database live__vue to role live__vue__task_runner__u_role;
    grant usage on future schemas in database live__vue to role live__vue__task_runner__u_role;
    grant operate on all tasks in database live__vue to role live__vue__task_runner__u_role;
    grant operate on future tasks in database live__vue to role live__vue__task_runner__u_role;
    grant impersonate on user live__vue__task_runner to role live__vue__owner__d_role;
    grant role live__vue__owner__d_role to user live__vue__task_runner;
    grant execute task on account to role live__vue__owner__d_role;
*/

    return 'Derived variables system initialized successfully';
end;
$$;
