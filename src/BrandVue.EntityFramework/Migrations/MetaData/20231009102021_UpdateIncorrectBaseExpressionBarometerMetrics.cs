#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UpdateIncorrectBaseExpressionBarometerMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var renameSql = @"
UPDATE [MetricConfigurations] SET [BaseExpression] = REPLACE(BaseExpression, 'ApparelBrowsedYesterday(productcategory', 'ApparelBrowsedYesterday(apparelcategory') WHERE ProductShortCode = 'barometer'
UPDATE [MetricConfigurations] SET [BaseExpression] = REPLACE(BaseExpression, 'FootwearBrowsedYesterday(productcategory', 'FootwearBrowsedYesterday(footwearcategory') WHERE ProductShortCode = 'barometer'
UPDATE [MetricConfigurations] SET [BaseExpression] = REPLACE(BaseExpression, 'AccessoriesBrowsedYesterday(productcategory', 'AccessoriesBrowsedYesterday(accessoriescategory') WHERE ProductShortCode = 'barometer'
";

            migrationBuilder.Sql(renameSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var renameSql = @"
UPDATE [MetricConfigurations] SET [BaseExpression] = REPLACE(BaseExpression, 'ApparelBrowsedYesterday(apparelcategory', 'ApparelBrowsedYesterday(productcategory') WHERE ProductShortCode = 'barometer'
UPDATE [MetricConfigurations] SET [BaseExpression] = REPLACE(BaseExpression, 'FootwearBrowsedYesterday(footwearcategory', 'FootwearBrowsedYesterday(productcategory') WHERE ProductShortCode = 'barometer'
UPDATE [MetricConfigurations] SET [BaseExpression] = REPLACE(BaseExpression, 'AccessoriesBrowsedYesterday(accessoriescategory', 'AccessoriesBrowsedYesterday(productcategory') WHERE ProductShortCode = 'barometer'
";
            migrationBuilder.Sql(renameSql);
        }
    }
}
