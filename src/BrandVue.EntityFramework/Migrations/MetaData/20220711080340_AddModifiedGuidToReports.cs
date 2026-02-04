#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddModifiedGuidToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiedGuid",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedGuid",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
