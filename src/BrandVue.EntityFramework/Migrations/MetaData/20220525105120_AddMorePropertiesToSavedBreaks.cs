#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddMorePropertiesToSavedBreaks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DefaultBreaksForSubProducts",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts");

            migrationBuilder.DropIndex(
                name: "IX_DefaultBreaksForSubProducts_SubProductId",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts");

            migrationBuilder.AlterColumn<string>(
                name: "SubProductId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "AuthCompanyShortCode",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(750)",
                maxLength: 750,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductShortCode",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "survey"); //Existing saved breaks are all for AllVue

            migrationBuilder.AddColumn<string>(
                name: "ProductShortCode",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "survey"); //Existing saved breaks are all for AllVue

            migrationBuilder.AddPrimaryKey(
                name: "PK_DefaultBreaksForSubProducts",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts",
                columns: new[] { "ProductShortCode", "SubProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedBreakCombinations_ProductShortCode_SubProductId_AuthCompanyShortCode",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                columns: new[] { "ProductShortCode", "SubProductId", "AuthCompanyShortCode" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedBreakCombinations_ProductShortCode_SubProductId_AuthCompanyShortCode",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DefaultBreaksForSubProducts",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts");

            migrationBuilder.DropColumn(
                name: "AuthCompanyShortCode",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.DropColumn(
                name: "ProductShortCode",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.DropColumn(
                name: "ProductShortCode",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts");

            migrationBuilder.AlterColumn<string>(
                name: "SubProductId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DefaultBreaksForSubProducts",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts",
                column: "SubProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultBreaksForSubProducts_SubProductId",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts",
                column: "SubProductId",
                unique: true);
        }
    }
}
