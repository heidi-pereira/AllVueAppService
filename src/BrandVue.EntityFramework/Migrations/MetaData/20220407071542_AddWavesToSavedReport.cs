#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWavesToSavedReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Waves",
                schema: "Reports",
                table: "SavedReports",
                type: "varchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Waves",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
