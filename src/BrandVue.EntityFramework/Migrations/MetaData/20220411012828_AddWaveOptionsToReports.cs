#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWaveOptionsToReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Reports].[SavedReports]
SET [Waves] = NULL
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
