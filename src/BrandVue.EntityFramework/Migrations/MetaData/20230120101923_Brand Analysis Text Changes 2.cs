#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class BrandAnalysisTextChanges2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
UPDATE [Parts]
SET Spec3 = 'purchased'
WHERE productshortcode = 'drinks'
AND Spec2 in ('Advocacy', 'Usage')
AND PartType in ('AnalysisScorecard', 'BrandAnalysisScorecard')

UPDATE [Parts]
SET Spec3 = JSON_MODIFY(Spec3, '$.pastTenseVerb', 'purchased')
WHERE productshortcode = 'drinks'
And Spec2 in ('Usage','Advocacy')
AND PartType in ('BrandAnalysisBasedOn')
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
