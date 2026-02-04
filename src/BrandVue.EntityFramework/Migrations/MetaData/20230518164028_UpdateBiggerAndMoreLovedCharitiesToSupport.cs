#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UpdateBiggerAndMoreLovedCharitiesToSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET HelpText = 'Support is a measure of how many people have used your brand in the last 12 months',
  Spec3 = 'have supported'
  where ProductShortCode = 'charities'
  and PartType = 'BrandAnalysisScorecard'
  and PaneId like '%Usage%'
  
  UPDATE [Parts]
  SET Spec3 = 'have supported'
  where ProductShortCode = 'charities'
  and PartType = 'AnalysisScorecard'
  and spec2 = 'Usage'

  UPDATE Pages
  SET DisplayName = 'Brand Support Analysis'
  where ProductShortCode = 'charities'
  and pageTitle = 'Brand Usage'
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET HelpText = 'Usage is a measure of how many people have used your brand in the last 12 months',
  Spec3 = 'currently support'
  where ProductShortCode = 'charities'
  and PartType = 'BrandAnalysisScorecard'
  and PaneId like '%Usage%'
  
  UPDATE [Parts]
  SET Spec3 = 'currently support'
  where ProductShortCode = 'charities'
  and PartType = 'AnalysisScorecard'
  and spec2 = 'Usage'

  UPDATE Pages
  SET DisplayName = 'Brand Usage Analysis'
  where ProductShortCode = 'charities'
  and pageTitle = 'Brand Usage'
";
            migrationBuilder.Sql(sql);
        }
    }
}
