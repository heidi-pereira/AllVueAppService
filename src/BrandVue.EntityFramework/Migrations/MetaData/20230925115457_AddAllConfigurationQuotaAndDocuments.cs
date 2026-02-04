#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddAllConfigurationQuotaAndDocuments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDocumentsTabAvailable",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuotaTabAvailable",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDocumentsTabAvailable",
                table: "AllVueConfigurations");

            migrationBuilder.DropColumn(
                name: "IsQuotaTabAvailable",
                table: "AllVueConfigurations");
        }
    }
}
