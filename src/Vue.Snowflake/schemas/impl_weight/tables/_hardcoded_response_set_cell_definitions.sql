-- response_set_cell_definitions: defines cell parts and ranges for each response set
create or alter transient table impl_weight._hardcoded_response_set_cell_definitions (
    response_set_id number(38, 0) not null,
    cell_part number(38, 0) not null,
    cell_part_id number(38, 0) not null,
    min_value number(38, 0) not null,
    max_value number(38, 0) not null,
    variable_identifier varchar(50) not null
) change_tracking = true;


insert into impl_weight._hardcoded_response_set_cell_definitions (response_set_id, cell_part, cell_part_id, min_value, max_value, variable_identifier)
select rs.response_set_id, eating_out_us_cell_definitions.cell_part, eating_out_us_cell_definitions.cell_part_id, eating_out_us_cell_definitions.min_value, eating_out_us_cell_definitions.max_value, eating_out_us_cell_definitions.variable_identifier from impl_response_set.response_sets rs
inner join impl_sub_product.sub_products sp on sp.sub_product_id = rs.sub_product_id
cross join
    (
        -- west
        select column1 as cell_part, column2 as cell_part_id, column3 as min_value, column4 as max_value, column5 as variable_identifier
        from
            values
            (0, 0, 2, 3, 'US_state'),
            (0, 0, 5, 6, 'US_state'),
            (0, 0, 12, 13, 'US_state'),
            (0, 0, 27, 27, 'US_state'),
            (0, 0, 29, 29, 'US_state'),
            (0, 0, 32, 32, 'US_state'),
            (0, 0, 38, 38, 'US_state'),
            (0, 0, 45, 45, 'US_state'),
            (0, 0, 48, 48, 'US_state'),
            (0, 0, 51, 51, 'US_state'),
            --south
            (0, 1, 1, 1, 'US_state'),
            (0, 1, 4, 4, 'US_state'),
            (0, 1, 8, 8, 'US_state'),
            (0, 1, 9, 9, 'US_state'),
            (0, 1, 10, 11, 'US_state'),
            (0, 1, 18, 19, 'US_state'),
            (0, 1, 21, 21, 'US_state'),
            (0, 1, 25, 25, 'US_state'),
            (0, 1, 34, 34, 'US_state'),
            (0, 1, 37, 37, 'US_state'),
            (0, 1, 41, 41, 'US_state'),
            (0, 1, 43, 44, 'US_state'),
            (0, 1, 47, 47, 'US_state'),
            (0, 1, 49, 49, 'US_state'),
            -- midwest
            (0, 2, 14, 17, 'US_state'),
            (0, 2, 23, 24, 'US_state'),
            (0, 2, 26, 26, 'US_state'),
            (0, 2, 28, 28, 'US_state'),
            (0, 2, 35, 36, 'US_state'),
            (0, 2, 42, 42, 'US_state'),
            (0, 2, 50, 50, 'US_state'),
            -- northeast
            (0, 3, 7, 7, 'US_state'),
            (0, 3, 20, 20, 'US_state'),
            (0, 3, 22, 22, 'US_state'),
            (0, 3, 30, 31, 'US_state'),
            (0, 3, 33, 33, 'US_state'),
            (0, 3, 39, 40, 'US_state'),
            (0, 3, 46, 46, 'US_state'),
            -- gender
            (1, 0, 0, 0, 'Gender'),
            (1, 1, 1, 1, 'Gender'),
            -- age
            (2, 0, 16, 24, 'Age'),
            (2, 1, 25, 39, 'Age'),
            (2, 2, 40, 54, 'Age'),
            (2, 3, 55, 74, 'Age'),
            -- household_income
            (3, 0, 0, 74999, 'Household_income'),
            (3, 1, 75000, 500000, 'Household_income')
    ) eating_out_us_cell_definitions
where response_set_identifier = 'US' and sp.product_identifier = 'eatingout';
