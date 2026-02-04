#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddEntitySetConfigUserIdForLogging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedUserId",
                table: "EntitySetConfigurations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedUserId",
                table: "EntitySetConfigurations");
        }
    }
}
