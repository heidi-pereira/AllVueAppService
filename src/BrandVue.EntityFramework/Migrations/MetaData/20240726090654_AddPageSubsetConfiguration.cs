#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddPageSubsetConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "MetricConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PageSubsetConfigurations",
                columns: table => new
                {
                    SubsetId = table.Column<int>(type: "int", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageSubsetConfigurations", x => new { x.SubsetId, x.PageId });
                    table.ForeignKey(
                        name: "FK_PageSubsetConfigurations_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageSubsetConfigurations_SubsetConfigurations_SubsetId",
                        column: x => x.SubsetId,
                        principalTable: "SubsetConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageSubsetConfigurations_PageId",
                table: "PageSubsetConfigurations",
                column: "PageId");
            migrationBuilder.Sql(@$"
with SplitSets AS (
    SELECT Id, ProductShortCode, SubProductId, Value
    FROM Pages
    CROSS APPLY STRING_SPLIT(Subset, '|')
)
INSERT INTO PageSubsetConfigurations (PageId, SubsetId, HelpText, Enabled)
SELECT ss.Id as PageId, sc.Id as SubsetId, null as HelpText, 1 as Enabled
FROM SplitSets ss
JOIN SubsetConfigurations sc ON ss.ProductShortCode = sc.ProductShortCode AND ss.Value = sc.Identifier;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageSubsetConfigurations");
            
            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "MetricConfigurations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
