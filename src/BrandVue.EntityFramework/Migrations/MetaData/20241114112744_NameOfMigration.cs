#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class NameOfMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MetricConfigurations_BaseVariableConfigurationId",
                table: "MetricConfigurations",
                column: "BaseVariableConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricConfigurations_VariableConfigurationId",
                table: "MetricConfigurations",
                column: "VariableConfigurationId");

            migrationBuilder.AddForeignKey(
                name: "FK_MetricConfigurations_VariableConfigurations_BaseVariableConfigurationId",
                table: "MetricConfigurations",
                column: "BaseVariableConfigurationId",
                principalTable: "VariableConfigurations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MetricConfigurations_VariableConfigurations_VariableConfigurationId",
                table: "MetricConfigurations",
                column: "VariableConfigurationId",
                principalTable: "VariableConfigurations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetricConfigurations_VariableConfigurations_BaseVariableConfigurationId",
                table: "MetricConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_MetricConfigurations_VariableConfigurations_VariableConfigurationId",
                table: "MetricConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_MetricConfigurations_BaseVariableConfigurationId",
                table: "MetricConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_MetricConfigurations_VariableConfigurationId",
                table: "MetricConfigurations");
        }
    }
}
