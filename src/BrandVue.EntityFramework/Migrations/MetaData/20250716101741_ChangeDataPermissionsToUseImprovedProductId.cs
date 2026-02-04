using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class ChangeDataPermissionsToUseImprovedProductId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AllVueRules_Organisation_SubProduct_AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.DropColumn(
                name: "SubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.AddColumn<int>(
                name: "ProjectOrProductId",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectType",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AllVueRules_Organisation_ProjectType_ProjectOrProductId_AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                columns: new[] { "Organisation", "ProjectType", "ProjectOrProductId", "AllUserAccessForSubProduct" },
                unique: true,
                filter: "[AllUserAccessForSubProduct] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AllVueRules_Organisation_ProjectType_ProjectOrProductId_AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.DropColumn(
                name: "ProjectOrProductId",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.DropColumn(
                name: "ProjectType",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.AddColumn<string>(
                name: "SubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AllVueRules_Organisation_SubProduct_AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                columns: new[] { "Organisation", "SubProduct", "AllUserAccessForSubProduct" },
                unique: true,
                filter: "[AllUserAccessForSubProduct] = 1");
        }
    }
}
