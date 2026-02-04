namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UseNonUnicodeForUrlForBookmark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_AppBase_Url",
                table: "Bookmarks");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Bookmarks",
                unicode: false,
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserName",
                table: "Bookmarks",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByIpAddress",
                table: "Bookmarks",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppBase",
                table: "Bookmarks",
                unicode: false,
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_AppBase_Url",
                table: "Bookmarks",
                columns: new[] { "AppBase", "Url" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_AppBase_Url",
                table: "Bookmarks");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Bookmarks",
                nullable: true,
                oldClrType: typeof(string),
                oldUnicode: false);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserName",
                table: "Bookmarks",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByIpAddress",
                table: "Bookmarks",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "AppBase",
                table: "Bookmarks",
                nullable: true,
                oldClrType: typeof(string),
                oldUnicode: false);

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_AppBase_Url",
                table: "Bookmarks",
                columns: new[] { "AppBase", "Url" },
                unique: true,
                filter: "[AppBase] IS NOT NULL AND [Url] IS NOT NULL");
        }
    }
}
