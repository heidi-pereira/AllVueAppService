#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddAverageConfigurationTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Averages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AverageId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TotalisationPeriodUnit = table.Column<byte>(type: "tinyint", nullable: false),
                    NumberOfPeriodsInAverage = table.Column<int>(type: "int", nullable: false),
                    WeightingMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    WeightAcross = table.Column<byte>(type: "tinyint", nullable: false),
                    AverageStrategy = table.Column<byte>(type: "tinyint", nullable: false),
                    MakeUpTo = table.Column<byte>(type: "tinyint", nullable: false),
                    WeightingPeriodUnit = table.Column<byte>(type: "tinyint", nullable: false),
                    IncludeResponseIds = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    AllowPartial = table.Column<bool>(type: "bit", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    SubsetIds = table.Column<string>(type: "varchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Averages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Averages_ProductShortCode_SubProductId_AverageId",
                table: "Averages",
                columns: new[] { "ProductShortCode", "SubProductId", "AverageId" },
                unique: true,
                filter: "[SubProductId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Averages");
        }
    }
}
