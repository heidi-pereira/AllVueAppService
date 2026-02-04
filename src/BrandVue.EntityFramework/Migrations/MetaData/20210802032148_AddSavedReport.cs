namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddSavedReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Reports");

            migrationBuilder.CreateTable(
                name: "SavedReports",
                schema: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReportPageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedReports_Pages_ReportPageId",
                        column: x => x.ReportPageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefaultSavedReports",
                schema: "Reports",
                columns: table => new
                {
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultSavedReports", x => new { x.ProductShortCode, x.SubProductId });
                    table.ForeignKey(
                        name: "FK_DefaultSavedReports_SavedReports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "Reports",
                        principalTable: "SavedReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefaultSavedReports_ReportId",
                schema: "Reports",
                table: "DefaultSavedReports",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_ProductShortCode_SubProductId",
                schema: "Reports",
                table: "SavedReports",
                columns: new[] { "ProductShortCode", "SubProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_ReportPageId",
                schema: "Reports",
                table: "SavedReports",
                column: "ReportPageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefaultSavedReports",
                schema: "Reports");

            migrationBuilder.DropTable(
                name: "SavedReports",
                schema: "Reports");
        }
    }
}
