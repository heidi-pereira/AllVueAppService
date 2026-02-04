create or replace function impl_response_set.sanitize_python_identifier(unsanitized_identifier varchar)
returns string
as
$$
    iff(regexp_like(left(unsanitized_identifier, 1), '[0-9]'), '_', '') || regexp_replace(unsanitized_identifier, '[^a-zA-Z0-9_]+', '_')
$$;
