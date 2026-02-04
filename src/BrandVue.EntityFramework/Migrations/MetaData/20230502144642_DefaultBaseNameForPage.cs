#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class DefaultBaseNameForPage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultBase",
                table: "Pages",
                type: "nvarchar(256)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultBase",
                table: "Pages");
        }
    }
}
