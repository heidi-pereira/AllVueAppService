#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ChangeBAMLWhereNextTo3MonthlyAverage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, ']', '], ""averageId"": ""MonthlyOver3Months""')
  FROM [Parts]
  WHERE PartType = 'BrandAnalysisWhereNext'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, ', ""averageId"": ""MonthlyOver3Months""', '')
  FROM [Parts]
  WHERE PartType = 'BrandAnalysisWhereNext'";
            migrationBuilder.Sql(sql);
        }
    }
}
