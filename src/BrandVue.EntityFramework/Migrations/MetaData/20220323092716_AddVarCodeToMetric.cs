#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddVarCodeToMetric : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VarCode",
                table: "MetricConfigurations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE MetricConfigurations SET VarCode = Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VarCode",
                table: "MetricConfigurations");
        }
    }
}
