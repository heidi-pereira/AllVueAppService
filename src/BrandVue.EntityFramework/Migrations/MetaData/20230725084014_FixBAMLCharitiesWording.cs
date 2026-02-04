#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class FixBAMLCharitiesWording : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                  UPDATE [Parts]
                  SET HelpText = 'Support is a measure of how many people have supported your charity within the last 12 months',
                  Spec3 = 'have supported'
                  where ProductShortCode = 'charities'
                  and PartType = 'BrandAnalysisScorecard'
                  and PaneId like '%Usage%'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
              UPDATE [Parts]
              SET HelpText = 'Support is a measure of how many people have used your brand in the last 12 months',
              Spec3 = 'have supported'
              where ProductShortCode = 'charities'
              and PartType = 'BrandAnalysisScorecard'
              and PaneId like '%Usage%'";
            migrationBuilder.Sql(sql);
        }
    }
}
