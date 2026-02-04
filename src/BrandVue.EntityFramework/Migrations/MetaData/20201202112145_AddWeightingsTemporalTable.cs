using Microsoft.EntityFrameworkCore.Metadata;

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWeightingsTemporalTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTemporalTable(
                name: "Weightings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<string>(nullable: true),
                    SubProductId = table.Column<string>(nullable: true),
                    SubsetId = table.Column<string>(nullable: true),
                    FieldGroupToFieldNameToMapping = table.Column<string>(nullable: true),
                    QuotaCellKeyToWeighting = table.Column<string>(nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "Weightings");
        }
    }
}
