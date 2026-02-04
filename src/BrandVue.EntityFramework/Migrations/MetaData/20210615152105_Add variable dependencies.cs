namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class Addvariabledependencies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "VariableConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "VariableDependencies",
                columns: table => new
                {
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    DependentUponVariableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableDependencies", x => new { x.VariableId, x.DependentUponVariableId });
                    table.ForeignKey(
                        name: "FK_VariableDependencies_VariableConfigurations_DependentUponVariableId",
                        column: x => x.DependentUponVariableId,
                        principalTable: "VariableConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VariableDependencies_VariableConfigurations_VariableId",
                        column: x => x.VariableId,
                        principalTable: "VariableConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VariableDependencies_DependentUponVariableId",
                table: "VariableDependencies",
                column: "DependentUponVariableId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VariableDependencies");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "VariableConfigurations");
        }
    }
}
