namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class UpdateSavedBrakesToSubProductId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefaultBreaksForSurveys",
                schema: "SavedBreaks");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.AlterColumn<string>(
                name: "SubProductId",
                table: "VariableConfigurations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "SubProductId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DefaultBreaksForSubProducts",
                schema: "SavedBreaks",
                columns: table => new
                {
                    SubProductId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SavedBreakCombinationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultBreaksForSubProducts", x => x.SubProductId);
                    table.ForeignKey(
                        name: "FK_DefaultBreaksForSubProducts_SavedBreakCombinations_SavedBreakCombinationId",
                        column: x => x.SavedBreakCombinationId,
                        principalSchema: "SavedBreaks",
                        principalTable: "SavedBreakCombinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefaultBreaksForSubProducts_SavedBreakCombinationId",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts",
                column: "SavedBreakCombinationId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultBreaksForSubProducts_SubProductId",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSubProducts",
                column: "SubProductId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefaultBreaksForSubProducts",
                schema: "SavedBreaks");

            migrationBuilder.DropColumn(
                name: "SubProductId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations");

            migrationBuilder.AlterColumn<string>(
                name: "SubProductId",
                table: "VariableConfigurations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<int>(
                name: "SurveyId",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DefaultBreaksForSurveys",
                schema: "SavedBreaks",
                columns: table => new
                {
                    SurveyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedBreakCombinationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultBreaksForSurveys", x => x.SurveyId);
                    table.ForeignKey(
                        name: "FK_DefaultBreaksForSurveys_SavedBreakCombinations_SavedBreakCombinationId",
                        column: x => x.SavedBreakCombinationId,
                        principalSchema: "SavedBreaks",
                        principalTable: "SavedBreakCombinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefaultBreaksForSurveys_SavedBreakCombinationId",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSurveys",
                column: "SavedBreakCombinationId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultBreaksForSurveys_SurveyId",
                schema: "SavedBreaks",
                table: "DefaultBreaksForSurveys",
                column: "SurveyId",
                unique: true);
        }
    }
}
