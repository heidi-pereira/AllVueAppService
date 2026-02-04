namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ReportOptionsAddExistingOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HighlightLowSample",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HighlightSignificance",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeCounts",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SignificanceType",
                schema: "Reports",
                table: "SavedReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SinglePageExport",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighlightLowSample",
                schema: "Reports",
                table: "SavedReports");

            migrationBuilder.DropColumn(
                name: "HighlightSignificance",
                schema: "Reports",
                table: "SavedReports");

            migrationBuilder.DropColumn(
                name: "IncludeCounts",
                schema: "Reports",
                table: "SavedReports");

            migrationBuilder.DropColumn(
                name: "SignificanceType",
                schema: "Reports",
                table: "SavedReports");

            migrationBuilder.DropColumn(
                name: "SinglePageExport",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
