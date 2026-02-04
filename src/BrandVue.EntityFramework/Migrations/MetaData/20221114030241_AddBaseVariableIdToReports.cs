#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddBaseVariableIdToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseVariableId",
                schema: "Reports",
                table: "SavedReports",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseVariableId",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
