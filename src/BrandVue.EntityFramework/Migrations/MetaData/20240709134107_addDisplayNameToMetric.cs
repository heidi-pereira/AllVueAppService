#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class addDisplayNameToMetric : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "MetricConfigurations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE MetricConfigurations
                SET DisplayName = VarCode
                WHERE DisplayName IS NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "MetricConfigurations");
        }
    }
}
