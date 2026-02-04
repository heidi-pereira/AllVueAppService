#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDefaultViewTypeToPage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultPaneViewType",
                table: "Pages",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPaneViewType",
                table: "Pages");
        }
    }
}
