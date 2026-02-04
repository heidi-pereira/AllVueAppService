namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddScaleFactorToMetricConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ScaleFactor",
                table: "MetricConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScaleFactor",
                table: "MetricConfigurations");
        }
    }
}
