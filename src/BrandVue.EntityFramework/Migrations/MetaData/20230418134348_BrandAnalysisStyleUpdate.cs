#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class BrandAnalysisStyleUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"update Pages set Layout = 'brand-analysis-cards' where Layout = 'cols2rows3' and Name = 'Brand Analysis'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"update Pages set Layout = 'cols2rows3' where Layout = 'brand-analysis-cards' and Name = 'Brand Analysis'";
            migrationBuilder.Sql(sql);
        }
    }
}
