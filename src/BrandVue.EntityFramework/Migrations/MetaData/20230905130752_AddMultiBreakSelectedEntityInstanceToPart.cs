#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddMultiBreakSelectedEntityInstanceToPart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MultiBreakSelectedEntityInstance",
                table: "Parts",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MultiBreakSelectedEntityInstance",
                table: "Parts");
        }
    }
}
