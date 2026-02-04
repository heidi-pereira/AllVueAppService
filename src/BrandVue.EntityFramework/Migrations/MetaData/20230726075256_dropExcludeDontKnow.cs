#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class dropExcludeDontKnow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludeDontKnows",
                schema: "Reports",
                table: "SavedReports");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludeDontKnows",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
