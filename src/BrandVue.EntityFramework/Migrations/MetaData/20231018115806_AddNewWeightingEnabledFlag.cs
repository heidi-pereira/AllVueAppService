#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddNewWeightingEnabledFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "UseNewWeightingUI",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "UseNewWeightingUI",
                table: "AllVueConfigurations");
        }
    }
}
