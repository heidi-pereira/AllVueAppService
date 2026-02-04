#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class RemoveGeneratedBaseExpressionsFromVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[MetricConfigurations]
ADD BaseExpression_OLD nvarchar(max) NULL

GO

UPDATE [dbo].[MetricConfigurations] SET BaseExpression_OLD = BaseExpression

UPDATE [dbo].[MetricConfigurations]
SET BaseExpression = NULL
WHERE ProductShortCode = 'survey'
AND VariableConfigurationId IS NOT NULL
AND BaseExpression IS NOT NULL
AND BaseExpression NOT LIKE '%=%'
AND BaseExpression LIKE '%len(%'
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
