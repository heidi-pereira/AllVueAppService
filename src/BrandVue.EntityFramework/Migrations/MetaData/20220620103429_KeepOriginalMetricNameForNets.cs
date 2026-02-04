#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class KeepOriginalMetricNameForNets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalMetricName",
                table: "MetricConfigurations",
                type: "nvarchar(450)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalMetricName",
                table: "MetricConfigurations");
        }
    }
}
