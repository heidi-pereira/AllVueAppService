#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddReportVue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ReportVue");

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "ReportVue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectReleases",
                schema: "ReportVue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniqueFolderName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    VersionOfRelease = table.Column<int>(type: "int", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonForRelease = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectReleases_Projects_ParentProjectId",
                        column: x => x.ParentProjectId,
                        principalSchema: "ReportVue",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPages",
                schema: "ReportVue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    FilterId = table.Column<int>(type: "int", nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectReleaseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPages_ProjectReleases_ProjectReleaseId",
                        column: x => x.ProjectReleaseId,
                        principalSchema: "ReportVue",
                        principalTable: "ProjectReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "ReportVue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportVueProjectPageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_ProjectPages_ReportVueProjectPageId",
                        column: x => x.ReportVueProjectPageId,
                        principalSchema: "ReportVue",
                        principalTable: "ProjectPages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPages_ProjectReleaseId_PageId_FilterId_BrandId",
                schema: "ReportVue",
                table: "ProjectPages",
                columns: new[] { "ProjectReleaseId", "PageId", "FilterId", "BrandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_ParentProjectId",
                schema: "ReportVue",
                table: "ProjectReleases",
                column: "ParentProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_UniqueFolderName",
                schema: "ReportVue",
                table: "ProjectReleases",
                column: "UniqueFolderName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProductShortCode_SubProductId_Name",
                schema: "ReportVue",
                table: "Projects",
                columns: new[] { "ProductShortCode", "SubProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ReportVueProjectPageId",
                schema: "ReportVue",
                table: "Tags",
                column: "ReportVueProjectPageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tags",
                schema: "ReportVue");

            migrationBuilder.DropTable(
                name: "ProjectPages",
                schema: "ReportVue");

            migrationBuilder.DropTable(
                name: "ProjectReleases",
                schema: "ReportVue");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "ReportVue");
        }
    }
}
