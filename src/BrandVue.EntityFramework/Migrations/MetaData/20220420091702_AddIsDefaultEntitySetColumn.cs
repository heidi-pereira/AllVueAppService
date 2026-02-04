#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddIsDefaultEntitySetColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "EntitySetConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "EntitySetConfigurations");
        }
    }
}
