#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class DropColumnsDisplayNameGeneratedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "EntityInstanceConfigurations");

            migrationBuilder.DropColumn(
                name: "GeneratedName",
                table: "EntityInstanceConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedByUser",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedByUser",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedName",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
