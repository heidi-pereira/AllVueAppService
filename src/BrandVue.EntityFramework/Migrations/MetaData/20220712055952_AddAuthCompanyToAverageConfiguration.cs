#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddAuthCompanyToAverageConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthCompanyShortCode",
                table: "Averages",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthCompanyShortCode",
                table: "Averages");
        }
    }
}
