namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddReportType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReportType",
                schema: "Reports",
                table: "SavedReports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportType",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
