namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Url = table.Column<string>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    DateLastGenerated = table.Column<DateTime>(nullable: false),
                    DateLastUsed = table.Column<DateTime>(nullable: true),
                    GenerationCount = table.Column<long>(nullable: false),
                    UseCount = table.Column<long>(nullable: false),
                    CreatedByUserName = table.Column<string>(nullable: true),
                    CreatedByIpAddress = table.Column<string>(nullable: true),
                    AppBase = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_AppBase_Url",
                table: "Bookmarks",
                columns: new[] { "AppBase", "Url" },
                unique: true,
                filter: "[AppBase] IS NOT NULL AND [Url] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookmarks");
        }
    }
}
