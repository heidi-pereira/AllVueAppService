#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class EnableBarometerMetricsForAllVue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  update [MetricConfigurations]
  set EligibleForCrosstabOrAllVue = 1
  where ProductShortCode = 'barometer'";

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
  update [MetricConfigurations]
  set EligibleForCrosstabOrAllVue = 0
  where ProductShortCode = 'barometer'";

            migrationBuilder.Sql(sql);
        }
    }
}
