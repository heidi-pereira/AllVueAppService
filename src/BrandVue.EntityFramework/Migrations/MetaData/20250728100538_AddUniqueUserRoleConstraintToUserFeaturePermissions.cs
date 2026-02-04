using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddUniqueUserRoleConstraintToUserFeaturePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserFeaturePermissions_UserId_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                columns: new[] { "UserId", "UserRoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserFeaturePermissions_UserId_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions");
        }
    }
}
