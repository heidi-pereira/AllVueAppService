#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class RemoveNonBreakingSpaces : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string removePartsNbspSql = "UPDATE dbo.Parts SET Spec1 = REPLACE(Spec1, NCHAR(0x00A0), ' ')";
            migrationBuilder.Sql(removePartsNbspSql);

            string removeMetricNbspSql = "UPDATE dbo.MetricConfigurations SET Name = REPLACE(Name, NCHAR(0x00A0), ' ')";
            migrationBuilder.Sql(removeMetricNbspSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
