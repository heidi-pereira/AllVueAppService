namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ChangeToIdAndAddGeneratedNameToInstanceConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntityInstanceConfigurationId",
                table: "EntityInstanceConfigurations",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "EntityTypeIdentifier",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedName",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedName",
                table: "EntityInstanceConfigurations");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "EntityInstanceConfigurations",
                newName: "EntityInstanceConfigurationId");

            migrationBuilder.AlterColumn<string>(
                name: "EntityTypeIdentifier",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
