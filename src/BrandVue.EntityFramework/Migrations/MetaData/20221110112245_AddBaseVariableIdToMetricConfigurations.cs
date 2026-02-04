#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddBaseVariableIdToMetricConfigurations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseVariableConfigurationId",
                table: "MetricConfigurations",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseVariableConfigurationId",
                table: "MetricConfigurations");
        }
    }
}
