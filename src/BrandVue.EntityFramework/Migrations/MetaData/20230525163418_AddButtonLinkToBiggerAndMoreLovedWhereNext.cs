#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddButtonLinkToBiggerAndMoreLovedWhereNext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, '], ""buttonLink"": ""All image associations""', ']')
  FROM [Parts]
  where parttype = 'BrandAnalysisWhereNext'

  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, ']', '], ""buttonLink"": ""All image associations""')
  FROM [Parts]
  where parttype = 'BrandAnalysisWhereNext'
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  UPDATE [Parts]
  SET spec3 = REPLACE(spec3, '], ""buttonLink"": ""All image associations""', ']')
  FROM [Parts]
  where parttype = 'BrandAnalysisWhereNext'
";
            migrationBuilder.Sql(sql);
        }
    }
}
