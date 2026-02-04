#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UpdateCompetitiveAverageNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var renameSql = @"UPDATE [EntitySetConfigurations] SET [Name] = REPLACE(Name, ' (average)', ' (competitive average)') WHERE Name LIKE '% (average)'";

            migrationBuilder.Sql(renameSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var renameSql = @"UPDATE [EntitySetConfigurations] SET [Name] = REPLACE(Name, ' (competitive average)', ' (average)') WHERE Name LIKE '% (competitive average)'";

            migrationBuilder.Sql(renameSql);
        }
    }
}
