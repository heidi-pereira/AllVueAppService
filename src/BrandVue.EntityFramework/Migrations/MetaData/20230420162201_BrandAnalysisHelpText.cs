#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class BrandAnalysisHelpText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
update parts set HelpText = 'Advocacy is a measure of how many of your customers would recommend your brand to friends or family. It is shaped by experience, and impacts the buzz generated around your brand.' where spec2 = 'Advocacy' and PartType = 'BrandAnalysisScorecard'
update parts set HelpText = 'Buzz is a measure of how much people are talking about you, and impacts the likelihood people will consider using your brand.' where spec2 = 'Buzz' and PartType = 'BrandAnalysisScorecard'
update parts set HelpText = 'Usage is a measure of how many people have used your brand in the last 12 months' where spec2 = 'Usage' and PartType = 'BrandAnalysisScorecard'
update parts set HelpText = 'Brand Love is a measure of the emotional link people have with your brand, and impacts the likelihood your brand will be used over your competitors.' where spec2 = 'Love' and PartType = 'BrandAnalysisScorecard'
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
