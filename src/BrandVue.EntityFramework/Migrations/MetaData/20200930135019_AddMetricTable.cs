using Microsoft.EntityFrameworkCore.Metadata;

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddMetricTable : Migration
    {
        public const string TableName = "Metrics";
        public const string HistoryTableName = "Metrics_History";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: TableName,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false),
                    FieldExpression = table.Column<string>(nullable: true),
                    Field = table.Column<string>(nullable: true),
                    Field2 = table.Column<string>(nullable: true),
                    FieldOp = table.Column<string>(nullable: true),
                    CalType = table.Column<string>(nullable: true),
                    TrueVals = table.Column<string>(nullable: true),
                    BaseField = table.Column<string>(nullable: true),
                    BaseVals = table.Column<string>(nullable: true),
                    MarketAverageBaseMeasure = table.Column<string>(nullable: true),
                    KeyImage = table.Column<string>(nullable: true),
                    Measure = table.Column<string>(nullable: true),
                    HelpText = table.Column<string>(nullable: true),
                    NumFormat = table.Column<string>(nullable: true),
                    Min = table.Column<int>(nullable: true),
                    Max = table.Column<int>(nullable: true),
                    ExcludeWaves = table.Column<int>(nullable: true),
                    StartDate = table.Column<DateTime>(nullable: false),
                    FilterValueMapping = table.Column<string>(nullable: true),
                    FilterGroup = table.Column<string>(nullable: true),
                    FilterMulti = table.Column<bool>(nullable: false),
                    PreNormalisationMinimum = table.Column<int>(nullable: true),
                    PreNormalisationMaximum = table.Column<int>(nullable: true),
                    Subset = table.Column<string>(nullable: true),
                    DisableMeasure = table.Column<bool>(nullable: false),
                    DisableFilter = table.Column<bool>(nullable: false),
                    InversedTargetValue = table.Column<bool>(nullable: false),
                    ExcludeList = table.Column<string>(nullable: true),
                    EligibleForMetricComparison = table.Column<bool>(nullable: false),
                    Category = table.Column<string>(nullable: true),
                    SubCategory = table.Column<string>(nullable: true),
                    ProductShortCode = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metrics", x => x.Id);
                });
            
            migrationBuilder.CreateIndex(
                name: "IX_Metrics_Name_ProductShortCode",
                table: "Metrics",
                columns: new[] { "Name", "ProductShortCode" },
                unique: true);
            migrationBuilder.AddTemporalTableSupport(TableName, HistoryTableName);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTableSupport(TableName, HistoryTableName);
            migrationBuilder.DropTable(
                name: "Metrics");
        }
    }
}
