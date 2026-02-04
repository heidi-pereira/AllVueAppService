create or replace function savanta_internal__utils.email_to_snake_name(email varchar)
returns varchar
language sql
as
$$
begin
    return upper(replace(split_part(:email, '@', 1), '.', '_'));
end;
$$;
