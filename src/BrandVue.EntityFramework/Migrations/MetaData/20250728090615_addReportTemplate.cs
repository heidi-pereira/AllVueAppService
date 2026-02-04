using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class addReportTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportTemplates",
                schema: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TemplateDescription = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BaseVariable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SavedReportTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportTemplateParts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserDefinedVariableDefinitions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportTemplates",
                schema: "Reports");
        }
    }
}
