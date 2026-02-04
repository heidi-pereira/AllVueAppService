namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddChartConfiguration : Migration
    {
        public const string TableName = "ChartConfigurations";
        public const string HistoryTableName = "ChartConfigurations_History";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: TableName,
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProductShortCode = table.Column<string>(maxLength: 20, nullable: false),
                    IsDraft = table.Column<bool>(nullable: false),
                    MetricDefinition = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartConfigurations", x => x.Id);
                });

            migrationBuilder.AddTemporalTableSupport(TableName, HistoryTableName);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTableSupport(TableName, HistoryTableName);
            migrationBuilder.DropTable(
                name: TableName);
        }
    }
}
