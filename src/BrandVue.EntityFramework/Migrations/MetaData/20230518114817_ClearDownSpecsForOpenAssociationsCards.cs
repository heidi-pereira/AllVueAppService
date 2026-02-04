#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ClearDownSpecsForOpenAssociationsCards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
UPDATE [Parts]
SET [Spec2] = NULL, [Spec3] = NULL
WHERE [ProductShortCode] IS NOT NULL
AND PartType = 'OpenAssociationsCard'
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
