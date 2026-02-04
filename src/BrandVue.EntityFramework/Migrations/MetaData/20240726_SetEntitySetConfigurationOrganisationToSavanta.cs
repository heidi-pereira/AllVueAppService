using System.IO;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class SetEntitySetConfigurationOrganisationToSavanta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\20240726_SetEntitySetConfigurationOrganisationToSavanta.sql");
            var sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
