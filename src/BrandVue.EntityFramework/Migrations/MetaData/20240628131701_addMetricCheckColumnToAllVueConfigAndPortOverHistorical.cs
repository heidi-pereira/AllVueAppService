#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class addMetricCheckColumnToAllVueConfigAndPortOverHistorical : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckOrphanedMetricsForCanonicalVariables",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckOrphanedMetricsForCanonicalVariables",
                table: "AllVueConfigurations");
        }
    }
}
