namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddOrderToSavedReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Order",
                schema: "Reports",
                table: "SavedReports",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                schema: "Reports",
                table: "SavedReports");
        }
    }
}
