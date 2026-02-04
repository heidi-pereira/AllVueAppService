namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class RenamePartBreaksOverride : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OverRideReportSettings",
                table: "Parts",
                newName: "OverrideReportBreaks");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OverrideReportBreaks",
                table: "Parts",
                newName: "OverRideReportSettings");
        }
    }
}
