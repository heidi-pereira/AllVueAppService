namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDefaultFiltersToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultFilters",
                schema: "Reports",
                table: "SavedReports",
                type: "varchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultFilters",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
