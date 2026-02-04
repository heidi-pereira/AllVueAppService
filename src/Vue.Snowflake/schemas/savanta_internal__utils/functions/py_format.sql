create or replace function savanta_internal__utils.py_format(f_string varchar, params object)
returns varchar
language python
runtime_version = '3.13'
handler = 'format_string'
as
$$
def format_string(f_string, params):
    return f_string.format(**params)
$$;
