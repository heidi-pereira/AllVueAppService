#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddButtonLinkToOtherBiggerAndMoreLovedParts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, '], ""buttonLink"": ""' + spec1 + '""', ']')
  FROM [Parts]
  WHERE PartType IN ('BrandAnalysisScore', 'BrandAnalysisPotentialScore', 'BrandAnalysisScoreOverTime')

  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, ']', '], ""buttonLink"": ""' + spec1 + '""')
  FROM [Parts]
  WHERE PartType IN ('BrandAnalysisScore', 'BrandAnalysisPotentialScore', 'BrandAnalysisScoreOverTime')
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, '], ""buttonLink"": ""' + spec1 + '""', ']')
  FROM [Parts]
  WHERE PartType IN ('BrandAnalysisScore', 'BrandAnalysisPotentialScore', 'BrandAnalysisScoreOverTime')
";
            migrationBuilder.Sql(sql);
        }
    }
}
