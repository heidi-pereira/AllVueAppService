#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddAllVueConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllVueConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsReportVueReportsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsReportsTabAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsDataTabAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ReportVueConfiguration = table.Column<string>(type: "varchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllVueConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllVueConfigurations_SubProductId_ProductShortCode",
                table: "AllVueConfigurations",
                columns: new[] { "SubProductId", "ProductShortCode" },
                unique: true,
                filter: "[SubProductId] IS NOT NULL AND [ProductShortCode] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllVueConfigurations");
        }
    }
}
