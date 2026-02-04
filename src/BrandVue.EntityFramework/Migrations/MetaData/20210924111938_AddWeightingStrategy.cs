namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWeightingStrategy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "Weightings");

            migrationBuilder.CreateTemporalTable(
                name: "WeightingStrategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubsetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilterMetricName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightingStrategies", x => x.Id);
                });

            migrationBuilder.CreateTemporalTable(
                name: "WeightingSchemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeightingStrategyId = table.Column<int>(type: "int", nullable: false),
                    FilterMetricEntityId = table.Column<int>(type: "int", nullable: true),
                    WeightingSchemeDetails = table.Column<string>(type: "varchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightingSchemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightingSchemes_WeightingStrategies_WeightingStrategyId",
                        column: x => x.WeightingStrategyId,
                        principalTable: "WeightingStrategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightingSchemes_WeightingStrategyId_FilterMetricEntityId",
                table: "WeightingSchemes",
                columns: new[] { "WeightingStrategyId", "FilterMetricEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightingStrategies_ProductShortCode_SubProductId_Name",
                table: "WeightingStrategies",
                columns: new[] { "ProductShortCode", "SubProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightingStrategies_ProductShortCode_SubProductId_SubsetId",
                table: "WeightingStrategies",
                columns: new[] { "ProductShortCode", "SubProductId", "SubsetId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "WeightingSchemes");

            migrationBuilder.DropTemporalTable(
                name: "WeightingStrategies");

            migrationBuilder.CreateTemporalTable(
                name: "Weightings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldGroupToFieldNameToMapping = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    QuotaCellKeyToWeighting = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubProductId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SubsetId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weightings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Weightings_ProductId_SubProductId_SubsetId",
                table: "Weightings",
                columns: new[] { "ProductId", "SubProductId", "SubsetId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL AND [SubProductId] IS NOT NULL AND [SubsetId] IS NOT NULL");
        }
    }
}
