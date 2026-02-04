#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddOverrideStartDateToSegments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OverriddenStartDate",
                schema: "dbo",
                table: "SubsetConfigurations",
                type: "datetimeoffset",
                nullable: true,
                defaultValue: null);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverriddenStartDate",
                schema: "dbo",
                table: "SubsetConfigurations");
        }
    }
}
