namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddVariableConfigIdColumnToMetricConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariableConfigurationId",
                table: "MetricConfigurations",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariableConfigurationId",
                table: "MetricConfigurations");
        }
    }
}
