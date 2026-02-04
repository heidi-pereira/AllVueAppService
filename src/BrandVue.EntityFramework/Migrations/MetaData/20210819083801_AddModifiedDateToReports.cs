namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddModifiedDateToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                schema: "Reports",
                table: "SavedReports",
                type: "datetime2",
                nullable: false,
                defaultValue: DateTimeOffset.UtcNow);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
