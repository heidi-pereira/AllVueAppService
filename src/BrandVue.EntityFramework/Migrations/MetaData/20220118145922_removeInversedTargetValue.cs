#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class removeInversedTargetValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InversedTargetValue",
                table: "MetricConfigurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InversedTargetValue",
                table: "MetricConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
