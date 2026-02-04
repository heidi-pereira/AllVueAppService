namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddExcludeDontKnowsToReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludeDontKnows",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludeDontKnows",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
