namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddBaseTypeOverrideToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "BaseTypeOverride",
                schema: "Reports",
                table: "SavedReports",
                type: "tinyint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseTypeOverride",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
