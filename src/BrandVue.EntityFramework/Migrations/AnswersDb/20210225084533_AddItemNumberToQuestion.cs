namespace BrandVue.EntityFramework.Migrations.AnswersDb
{
    public partial class AddItemNumberToQuestion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vue");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "ChoiceSets",
                schema: "vue",
                columns: table => new
                {
                    ChoiceSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentChoiceSet1Id = table.Column<int>(type: "int", nullable: true),
                    ParentChoiceSet2Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoiceSets", x => x.ChoiceSetId);
                    table.ForeignKey(
                        name: "FK_ChoiceSets_ChoiceSets_ParentChoiceSet1Id",
                        column: x => x.ParentChoiceSet1Id,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChoiceSets_ChoiceSets_ParentChoiceSet2Id",
                        column: x => x.ParentChoiceSet2Id,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponse",
                schema: "dbo",
                columns: table => new
                {
                    responseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    surveyId = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    segmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponse", x => x.responseId);
                });

            migrationBuilder.CreateTable(
                name: "surveys",
                schema: "dbo",
                columns: table => new
                {
                    surveyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    uniqueSurveyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    surveyStructureId = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveys", x => x.surveyId);
                });

            migrationBuilder.CreateTable(
                name: "surveySegments",
                schema: "dbo",
                columns: table => new
                {
                    surveySegmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    segmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveySegments", x => x.surveySegmentId);
                });

            migrationBuilder.CreateTable(
                name: "surveyStructures",
                schema: "dbo",
                columns: table => new
                {
                    surveyStructureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveyStructures", x => x.surveyStructureId);
                });

            migrationBuilder.CreateTable(
                name: "Choices",
                schema: "vue",
                columns: table => new
                {
                    ChoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChoiceSetId = table.Column<int>(type: "int", nullable: false),
                    SurveyId = table.Column<int>(type: "int", nullable: false),
                    SurveyChoiceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Choices", x => x.ChoiceId);
                    table.ForeignKey(
                        name: "FK_Choices_ChoiceSets_ChoiceSetId",
                        column: x => x.ChoiceSetId,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                schema: "vue",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SectionChoiceSetId = table.Column<int>(type: "int", nullable: true),
                    PageChoiceSetId = table.Column<int>(type: "int", nullable: true),
                    AnswerChoiceSetId = table.Column<int>(type: "int", nullable: true),
                    QuestionChoiceSetId = table.Column<int>(type: "int", nullable: true),
                    VarCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MasterType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ItemNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_Questions_ChoiceSets_AnswerChoiceSetId",
                        column: x => x.AnswerChoiceSetId,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_ChoiceSets_PageChoiceSetId",
                        column: x => x.PageChoiceSetId,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_ChoiceSets_QuestionChoiceSetId",
                        column: x => x.QuestionChoiceSetId,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_ChoiceSets_SectionChoiceSetId",
                        column: x => x.SectionChoiceSetId,
                        principalSchema: "vue",
                        principalTable: "ChoiceSets",
                        principalColumn: "ChoiceSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                schema: "vue",
                columns: table => new
                {
                    ResponseId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SectionChoiceId = table.Column<int>(type: "int", nullable: true),
                    PageChoiceId = table.Column<int>(type: "int", nullable: true),
                    QuestionChoiceId = table.Column<int>(type: "int", nullable: true),
                    AnswerChoiceId = table.Column<int>(type: "int", nullable: true),
                    AnswerValue = table.Column<int>(type: "int", nullable: true),
                    AnswerText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "vue",
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerStats",
                schema: "vue",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    RespondentCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_AnswerStats_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "vue",
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                schema: "vue",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerStats_QuestionId",
                schema: "vue",
                table: "AnswerStats",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Choices_ChoiceSetId_SurveyChoiceId",
                schema: "vue",
                table: "Choices",
                columns: new[] { "ChoiceSetId", "SurveyChoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceSets_ParentChoiceSet1Id",
                schema: "vue",
                table: "ChoiceSets",
                column: "ParentChoiceSet1Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceSets_ParentChoiceSet2Id",
                schema: "vue",
                table: "ChoiceSets",
                column: "ParentChoiceSet2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceSets_SurveyId_Name",
                schema: "vue",
                table: "ChoiceSets",
                columns: new[] { "SurveyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AnswerChoiceSetId",
                schema: "vue",
                table: "Questions",
                column: "AnswerChoiceSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_PageChoiceSetId",
                schema: "vue",
                table: "Questions",
                column: "PageChoiceSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionChoiceSetId",
                schema: "vue",
                table: "Questions",
                column: "QuestionChoiceSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SectionChoiceSetId",
                schema: "vue",
                table: "Questions",
                column: "SectionChoiceSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SurveyId_VarCode",
                schema: "vue",
                table: "Questions",
                columns: new[] { "SurveyId", "VarCode" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Answers",
                schema: "vue");

            migrationBuilder.DropTable(
                name: "AnswerStats",
                schema: "vue");

            migrationBuilder.DropTable(
                name: "Choices",
                schema: "vue");

            migrationBuilder.DropTable(
                name: "SurveyResponse",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "surveys",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "surveySegments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "surveyStructures",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Questions",
                schema: "vue");

            migrationBuilder.DropTable(
                name: "ChoiceSets",
                schema: "vue");
        }
    }
}
