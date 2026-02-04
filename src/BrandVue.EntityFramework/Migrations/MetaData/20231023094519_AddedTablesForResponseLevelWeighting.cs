#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddedTablesForResponseLevelWeighting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResponseWeightingContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubsetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseWeightingContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponseWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RespondentId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(20,10)", precision: 20, scale: 10, nullable: false),
                    ResponseWeightingContextId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseWeights_ResponseWeightingContexts_ResponseWeightingContextId",
                        column: x => x.ResponseWeightingContextId,
                        principalTable: "ResponseWeightingContexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseWeightingContexts_ProductShortCode_SubProductId_SubsetId_Context",
                table: "ResponseWeightingContexts",
                columns: new[] { "ProductShortCode", "SubProductId", "SubsetId", "Context" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseWeights_ResponseWeightingContextId",
                table: "ResponseWeights",
                column: "ResponseWeightingContextId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponseWeights");

            migrationBuilder.DropTable(
                name: "ResponseWeightingContexts");
        }
    }
}
