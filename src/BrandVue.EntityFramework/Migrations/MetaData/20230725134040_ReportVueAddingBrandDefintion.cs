#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ReportVueAddingBrandDefintion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserTextForBrandEntity",
                schema: "ReportVue",
                table: "ProjectReleases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Brand");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserTextForBrandEntity",
                schema: "ReportVue",
                table: "ProjectReleases");
        }
    }
}
