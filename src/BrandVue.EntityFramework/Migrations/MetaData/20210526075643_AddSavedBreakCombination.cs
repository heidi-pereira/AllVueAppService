namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddSavedBreakCombination : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SavedBreaks");

            migrationBuilder.CreateTable(
                name: "SavedBreakCombinations",
                schema: "SavedBreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SurveyId = table.Column<int>(type: "int", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Breaks = table.Column<string>(type: "varchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBreakCombinations", x => x.Id);
                });

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefaultBreaksForSurveys",
                schema: "SavedBreaks");

            migrationBuilder.DropTable(
                name: "SavedBreakCombinations",
                schema: "SavedBreaks");
        }
    }
}
