#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class addEntityIdMeanCalculationValueMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityInstanceIdMeanCalculationValueMapping",
                table: "MetricConfigurations",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityInstanceIdMeanCalculationValueMapping",
                table: "MetricConfigurations");
        }
    }
}
