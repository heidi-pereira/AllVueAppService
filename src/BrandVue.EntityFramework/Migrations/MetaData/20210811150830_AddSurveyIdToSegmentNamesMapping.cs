namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddSurveyIdToSegmentNamesMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowedSegmentNames",
                table: "SubsetConfigurations",
                newName: "SurveyIdToAllowedSegmentNames");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SurveyIdToAllowedSegmentNames",
                table: "SubsetConfigurations",
                newName: "AllowedSegmentNames");
        }
    }
}
