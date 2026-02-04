#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class BrandAnalysisTextChanges1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
update parts
SET Spec3 = JSON_MODIFY(Spec3, '$.pastTenseVerb', null)
WHERE PartType in ('BrandAnalysisBasedOn')

UPDATE [Parts]
SET Spec3 = 'currently support'
WHERE productshortcode = 'charities'
AND Spec2 in ('Advocacy', 'Usage')
AND PartType in ('AnalysisScorecard', 'BrandAnalysisScorecard')


UPDATE [Parts]
SET Spec3 = JSON_MODIFY(Spec3, '$.pastTenseVerb', 'currently support')
WHERE productshortcode = 'charities'
And Spec2 in ('Usage','Advocacy')
AND PartType in ('BrandAnalysisBasedOn')


UPDATE [Parts]
SET Spec3 = 'purchased from'
WHERE productshortcode = 'drinks'
AND Spec2 in ('Advocacy', 'Usage')
AND PartType in ('AnalysisScorecard', 'BrandAnalysisScorecard')

UPDATE [Parts]
SET Spec3 = JSON_MODIFY(Spec3, '$.pastTenseVerb', 'purchased from')
WHERE productshortcode = 'drinks'
And Spec2 in ('Usage','Advocacy')
AND PartType in ('BrandAnalysisBasedOn')


UPDATE [Parts]
SET Spec3 = 'purchased from'
WHERE productshortcode = 'finance'
AND Spec2 in ('Advocacy', 'Usage')
AND PartType in ('AnalysisScorecard', 'BrandAnalysisScorecard')

UPDATE [Parts]
SET Spec3 = JSON_MODIFY(Spec3, '$.pastTenseVerb', 'purchased from')
WHERE productshortcode = 'finance'
And Spec2 in ('Usage','Advocacy')
AND PartType in ('BrandAnalysisBasedOn')


UPDATE [Parts]
SET Spec3 = 'purchased from'
WHERE productshortcode = 'retail'
AND Spec2 in ('Advocacy', 'Usage')
AND PartType in ('AnalysisScorecard', 'BrandAnalysisScorecard')

UPDATE [Parts]
SET Spec3 = JSON_MODIFY(Spec3, '$.pastTenseVerb', 'purchased from')
WHERE productshortcode = 'retail'
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
