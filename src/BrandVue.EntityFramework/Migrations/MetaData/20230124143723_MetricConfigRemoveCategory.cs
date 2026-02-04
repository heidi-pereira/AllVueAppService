#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class MetricConfigRemoveCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "MetricConfigurations");

            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "MetricConfigurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MetricConfigurations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "MetricConfigurations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
