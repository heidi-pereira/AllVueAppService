#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class FixBAMLCharitiesLoveLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                  UPDATE [Parts] SET [Spec3] = REPLACE(Spec3, 'Love', 'Brand Affinity')
                    WHERE ProductShortCode = 'charities' 
                    AND PaneId = 'Brand Analysis Love_1' 
                    AND Spec1 = 'Love' 
                    AND Spec2 = 'Love' 
                    AND PartType = 'BrandAnalysisScore'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                  UPDATE [Parts] SET [Spec3] = REPLACE(Spec3, 'Brand Affinity', 'Love')
                    WHERE ProductShortCode = 'charities' 
                    AND PaneId = 'Brand Analysis Love_1' 
                    AND Spec1 = 'Love' 
                    AND Spec2 = 'Love' 
                    AND PartType = 'BrandAnalysisScore'";
            migrationBuilder.Sql(sql);
        }
    }
}
