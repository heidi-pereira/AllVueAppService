#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AllVueConfigurationRemoveObsoleteAndAddWaveDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReportVueReportsAvailable",
                table: "AllVueConfigurations");

            migrationBuilder.DropColumn(
                name: "ReportVueConfiguration",
                table: "AllVueConfigurations");

            migrationBuilder.AddColumn<int>(
                name: "SurveyType",
                table: "AllVueConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AllVueConfiguration_WaveVariableForSubset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubsetIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    VariableIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AllVueConfigurationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllVueConfiguration_WaveVariableForSubset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllVueConfiguration_WaveVariableForSubset_AllVueConfigurations_AllVueConfigurationId",
                        column: x => x.AllVueConfigurationId,
                        principalTable: "AllVueConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllVueConfiguration_WaveVariableForSubset_AllVueConfigurationId",
                table: "AllVueConfiguration_WaveVariableForSubset",
                column: "AllVueConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllVueConfiguration_WaveVariableForSubset");

            migrationBuilder.DropColumn(
                name: "SurveyType",
                table: "AllVueConfigurations");

            migrationBuilder.AddColumn<bool>(
                name: "IsReportVueReportsAvailable",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReportVueConfiguration",
                table: "AllVueConfigurations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
