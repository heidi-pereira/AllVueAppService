#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class VariableDependencyClientCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_DependentUponVariableId",
                table: "VariableDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_VariableId",
                table: "VariableDependencies");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_DependentUponVariableId",
                table: "VariableDependencies",
                column: "DependentUponVariableId",
                principalTable: "VariableConfigurations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_VariableId",
                table: "VariableDependencies",
                column: "VariableId",
                principalTable: "VariableConfigurations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_DependentUponVariableId",
                table: "VariableDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_VariableId",
                table: "VariableDependencies");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_DependentUponVariableId",
                table: "VariableDependencies",
                column: "DependentUponVariableId",
                principalTable: "VariableConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableDependencies_VariableConfigurations_VariableId",
                table: "VariableDependencies",
                column: "VariableId",
                principalTable: "VariableConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
