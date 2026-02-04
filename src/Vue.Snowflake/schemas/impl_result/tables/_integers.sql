create or replace transient table impl_result._integers (number int)
as
select row_number() over (order by 0) as number
from table(generator(rowcount => 10000));
