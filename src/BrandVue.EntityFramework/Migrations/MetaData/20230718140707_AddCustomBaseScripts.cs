using System.IO;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddCustomBaseScripts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\charities bases.sql");
            var sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\drinks bases.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\eatingout bases.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\finance bases.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\retail bases.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\barometer bases.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\brandvue360 bases.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);

            sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\SetDefaultBase.sql");
            sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"UPDATE pg
SET pg.DefaultBase = NULL
FROM Pages pg
JOIN Panes pn
ON (pn.PageName = pg.Name AND pn.ProductShortCode = pg.ProductShortCode)
WHERE PaneType = 'AudienceProfile'
GO
DELETE FROM [VariableDependencies] WHERE DependentUponVariableId IN (SELECT Id FROM [VariableConfigurations] WHERE ProductShortCode IN ('barometer','brandvue','charities','drinks','eatingout','finance','retail') AND Definition LIKE '%BaseFieldVariableExpressionDefinition%')
GO
DELETE FROM [VariableDependencies] WHERE VariableId IN (SELECT Id FROM [VariableConfigurations] WHERE ProductShortCode IN ('barometer','brandvue','charities','drinks','eatingout','finance','retail') AND Definition LIKE '%BaseFieldVariableExpressionDefinition%')
GO
DELETE FROM [VariableConfigurations] WHERE ProductShortCode IN ('barometer','brandvue','charities','drinks','eatingout','finance','retail') AND Definition LIKE '%BaseFieldVariableExpressionDefinition%'
GO";
            migrationBuilder.Sql(sql);
        }
    }
}
