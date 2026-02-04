namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class addIsDataWeighted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDataWeighted",
                schema: "Reports",
                table: "SavedReports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDataWeighted",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
