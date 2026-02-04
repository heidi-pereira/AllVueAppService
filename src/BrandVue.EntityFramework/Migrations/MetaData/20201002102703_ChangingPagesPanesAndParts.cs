namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ChangingPagesPanesAndParts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Subset",
                table: "Panes");

            migrationBuilder.RenameColumn(
                name: "ChildPages",
                table: "Pages",
                newName: "Roles");

            migrationBuilder.AlterColumn<string>(
                name: "ProductShortCode",
                table: "Parts",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaneId",
                table: "Parts",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductShortCode",
                table: "Panes",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaneId",
                table: "Panes",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductShortCode",
                table: "Pages",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Pages",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parts_ProductShortCode_PaneId",
                table: "Parts",
                columns: new[] { "ProductShortCode", "PaneId" });

            migrationBuilder.CreateIndex(
                name: "IX_Panes_ProductShortCode_PaneId",
                table: "Panes",
                columns: new[] { "ProductShortCode", "PaneId" },
                unique: true,
                filter: "[ProductShortCode] IS NOT NULL AND [PaneId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ProductShortCode_Name",
                table: "Pages",
                columns: new[] { "ProductShortCode", "Name" },
                unique: true,
                filter: "[ProductShortCode] IS NOT NULL AND [Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parts_ProductShortCode_PaneId",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Panes_ProductShortCode_PaneId",
                table: "Panes");

            migrationBuilder.DropIndex(
                name: "IX_Pages_ProductShortCode_Name",
                table: "Pages");

            migrationBuilder.RenameColumn(
                name: "Roles",
                table: "Pages",
                newName: "ChildPages");

            migrationBuilder.AlterColumn<string>(
                name: "ProductShortCode",
                table: "Parts",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaneId",
                table: "Parts",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Parts",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductShortCode",
                table: "Panes",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaneId",
                table: "Panes",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subset",
                table: "Panes",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductShortCode",
                table: "Pages",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Pages",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
