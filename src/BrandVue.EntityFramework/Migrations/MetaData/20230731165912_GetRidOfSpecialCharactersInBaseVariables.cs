#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class GetRidOfSpecialCharactersInBaseVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @" 
UPDATE [dbo].[VariableConfigurations]
SET Definition = REPLACE(STUFF(Definition, CHARINDEX('any(response', Definition), CHARINDEX('(brand', Definition) - CHARINDEX('any(response', Definition), REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(SUBSTRING(Definition, CHARINDEX('any(response', Definition), CHARINDEX('(brand', Definition) - CHARINDEX('any(response', Definition) ), '.', ''), '&',''), ' ',''), '-',''), '''',''),'/',''),',',''),':','')),'any(response','any(response.'),
Identifier = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Identifier, '.', ''), '&',''), ' ',''), '-',''), '''',''),'/',''),',',''),':','')
WHERE ProductShortCode != 'survey'
AND Definition like '%BaseFieldExpressionVariableDefinition%'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
