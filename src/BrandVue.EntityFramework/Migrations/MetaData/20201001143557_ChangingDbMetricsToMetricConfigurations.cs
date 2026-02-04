using Microsoft.EntityFrameworkCore.Metadata;

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ChangingDbMetricsToMetricConfigurations : Migration
    {
        private const string MetricConfigurationsTableName = "MetricConfigurations";
        private const string MetricConfigurationsHistoryTableName = "MetricsConfigurations_History";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTableSupport(AddMetricTable.TableName, AddMetricTable.HistoryTableName);
            migrationBuilder.DropTable(
                name: AddMetricTable.TableName);

            migrationBuilder.CreateTable(
                name: MetricConfigurationsTableName,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProductShortCode = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    FieldExpression = table.Column<string>(nullable: true),
                    Field = table.Column<string>(nullable: true),
                    Field2 = table.Column<string>(nullable: true),
                    FieldOp = table.Column<string>(nullable: true),
                    CalType = table.Column<string>(nullable: true),
                    TrueVals = table.Column<string>(nullable: true),
                    BaseExpression = table.Column<string>(nullable: true),
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
                    SubCategory = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricConfigurations_Name_ProductShortCode",
                table: "MetricConfigurations",
                columns: new[] { "Name", "ProductShortCode" },
                unique: true);
            migrationBuilder.AddTemporalTableSupport(MetricConfigurationsTableName, MetricConfigurationsHistoryTableName);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTableSupport(MetricConfigurationsTableName, MetricConfigurationsHistoryTableName);
            migrationBuilder.DropTable(
                name: MetricConfigurationsTableName);
        }
    }
}
