#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UpdateMultiEntitySplitByAndMainJsonStructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Parts
SET MultipleEntitySplitByAndMain = CASE
WHEN JSON_VALUE(MultipleEntitySplitByAndMain, '$.SplitByEntityType[0]') IS NOT NULL THEN
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                MultipleEntitySplitByAndMain,
                '$.SplitByEntityType',
                JSON_VALUE(MultipleEntitySplitByAndMain, '$.SplitByEntityType[0]')
            ),
        'append $.FilterByEntityTypes',
        JSON_QUERY(CONCAT('{""Type"":""', JSON_VALUE(MultipleEntitySplitByAndMain, '$.MainEntityType'), '""}'))
        ),
    '$.MainEntityType',
    NULL
)
ELSE 
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                MultipleEntitySplitByAndMain,
                'strict $.SplitByEntityType',
                JSON_VALUE(MultipleEntitySplitByAndMain, '$.MainEntityType')
            ),
        '$.FilterByEntityTypes',
        JSON_QUERY('[]')
    ),
    '$.MainEntityType',
    NULL
)
END
WHERE MultipleEntitySplitByAndMain IS NOT NULL
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
