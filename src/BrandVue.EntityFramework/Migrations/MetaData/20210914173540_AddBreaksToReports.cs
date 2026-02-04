namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddBreaksToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Breaks",
                schema: "Reports",
                table: "SavedReports",
                type: "varchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Breaks",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
