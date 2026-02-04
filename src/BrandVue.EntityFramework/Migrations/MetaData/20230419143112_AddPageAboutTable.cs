#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddPageAboutTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTemporalTable(
                name: "PageAbout",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    AboutTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AboutContent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    User = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Editable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageAbout", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageAbout_ProductShortCode_PageId",
                table: "PageAbout",
                columns: new[] { "ProductShortCode", "PageId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "PageAbout");
        }
    }
}
