#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddEligibleForCrosstabOrAllVue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EligibleForCrosstabOrAllVue",
                table: "MetricConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                @"UPDATE MetricConfigurations
                  SET EligibleForCrosstabOrAllVue = CASE WHEN (ProductShortCode = 'survey') 
									                    THEN (CASE WHEN (DisableMeasure = 0) THEN 1 ELSE 0 END)
									                    ELSE EligibleForMetricComparison END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EligibleForCrosstabOrAllVue",
                table: "MetricConfigurations");
        }
    }
}
