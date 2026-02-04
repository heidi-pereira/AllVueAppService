namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDefaultSplitByEntityTypeToMetricConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultSplitByEntityType",
                table: "MetricConfigurations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultSplitByEntityType",
                table: "MetricConfigurations");
        }
    }
}
