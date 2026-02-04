#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class MigrateMetricFieldExpressionsToVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
Create Function [dbo].[RemoveNonAlphaCharacters](@Temp VarChar(1000))
Returns VarChar(1000)
AS
Begin

    Declare @KeepValues as varchar(50)
    Set @KeepValues = '%[^a-z0-9]%'
    While PatIndex(@KeepValues, @Temp) > 0
        Set @Temp = Stuff(@Temp, PatIndex(@KeepValues, @Temp), 1, '')

    Return @Temp
End;
GO

ALTER TABLE VariableConfigurations
ADD GeneratedFromMetricId INT NULL
GO

BEGIN TRANSACTION

INSERT INTO VariableConfigurations(ProductShortCode, SubProductId, DisplayName, Definition, Identifier, GeneratedFromMetricId)
SELECT 
    m.ProductShortCode,
    m.SubProductId,
    CONCAT(m.Name, ' (Field Expression)'),
    CONCAT('{""Expression"": ""', m.FieldExpression, '"", ""discriminator"": ""FieldExpressionVariableDefinition""}'),
    CONCAT('_', [dbo].[RemoveNonAlphaCharacters](m.Name), '_fieldExpressionVariable'),
    m.Id
FROM MetricConfigurations m
WHERE m.FieldExpression IS NOT NULL AND m.VariableConfigurationId IS NULL

UPDATE MetricConfigurations
SET FieldExpression = NULL, VariableConfigurationId = v.Id
FROM MetricConfigurations m
JOIN VariableConfigurations v ON v.GeneratedFromMetricId = m.Id

COMMIT TRANSACTION

ALTER TABLE VariableConfigurations
DROP COLUMN GeneratedFromMetricId

DROP FUNCTION IF EXISTS [dbo].[RemoveNonAlphaCharacters]
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
