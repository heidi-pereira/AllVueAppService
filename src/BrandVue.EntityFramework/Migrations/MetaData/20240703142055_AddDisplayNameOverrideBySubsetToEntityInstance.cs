using System.IO;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDisplayNameOverrideBySubsetToEntityInstance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayNameOverrideBySubset",
                table: "EntityInstanceConfigurations",
                type: "varchar(max)",
                maxLength: 4000,
                nullable: true);

            var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\20240703142055_AddDisplayNameOverrideBySubsetToEntityInstance.sql");
            var sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayNameOverrideBySubset",
                table: "EntityInstanceConfigurations");
        }
    }
}
