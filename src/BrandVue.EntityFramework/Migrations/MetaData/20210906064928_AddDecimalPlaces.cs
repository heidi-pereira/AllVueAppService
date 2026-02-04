namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDecimalPlaces : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                schema: "Reports",
                table: "SavedReports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
