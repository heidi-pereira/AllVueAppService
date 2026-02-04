using System.IO;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UpdateBrandAnalysisWhereNextParts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
UPDATE [Parts]
SET Spec3 = '{""metrics"":[{""key"":""Promoters"",""metricName"":""Brand Love"",""requestType"":""scorecardPerformance""}]}'
WHERE productshortcode = 'brandvue'
AND PaneId = 'BrandLove'
AND PartType = 'BrandAnalysisWhereNext'

UPDATE [Parts]
SET Spec3 = '{""metrics"":[{""key"":""Promotors"",""metricName"":""Promotors Advocacy"",""requestType"":""scorecardPerformance""},{""key"":""Detractors"",""metricName"":""Detractors"",""requestType"":""scorecardPerformance""}]}'
WHERE productshortcode = 'finance'
AND PaneId = 'BrandAdvocacy'
AND PartType = 'BrandAnalysisWhereNext'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        { }
    }
}
