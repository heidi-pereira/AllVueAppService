namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class hideEmptyRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideEmptyRows",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideEmptyRows",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
