#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddLinkedMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkedMetric",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MetricName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LinkedMetricNames = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedMetric", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedMetric_ProductShortCode_MetricName",
                table: "LinkedMetric",
                columns: new[] { "ProductShortCode", "MetricName" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinkedMetric");
        }
    }
}
