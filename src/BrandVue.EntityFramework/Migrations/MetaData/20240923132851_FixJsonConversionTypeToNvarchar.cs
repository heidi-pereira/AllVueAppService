#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class FixJsonConversionTypeToNvarchar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "WeightingSchemeDetails",
                table: "WeightingSchemes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Definition",
                table: "VariableConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SurveyIdToAllowedSegmentNames",
                table: "SubsetConfigurations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Waves",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultFilters",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breaks",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breaks",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Waves",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SelectedEntityInstances",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MultipleEntitySplitByAndMain",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breaks",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BaseExpressionOverride",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AverageType",
                table: "Parts",
                type: "nvarchar(256)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityInstanceIdMeanCalculationValueMapping",
                table: "MetricConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LinkedMetricNames",
                table: "LinkedMetric",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SurveyChoiceSetNames",
                table: "EntityTypeConfigurations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StartDateBySubset",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EnabledBySubset",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayNameOverrideBySubset",
                table: "EntityInstanceConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubsetIds",
                table: "Averages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "varchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Group",
                table: "Averages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReportVueConfiguration",
                table: "AllVueConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);

            migrationBuilder.Sql($@"
DELETE FROM EntitySetAverageMappingConfigurations
WHERE NOT EXISTS
(
    SELECT TOP 1 1 FROM EntitySetConfigurations es
    WHERE es.id = ParentEntitySetId
)
OR NOT EXISTS
(
    SELECT TOP 1 1 FROM EntitySetConfigurations es
    WHERE es.id = ChildEntitySetId
)");

            migrationBuilder.CreateIndex(
                name: "IX_EntitySetAverageMappingConfigurations_ChildEntitySetId",
                table: "EntitySetAverageMappingConfigurations",
                column: "ChildEntitySetId");

            migrationBuilder.AddForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ChildEntitySetId",
                table: "EntitySetAverageMappingConfigurations",
                column: "ChildEntitySetId",
                principalTable: "EntitySetConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations",
                column: "ParentEntitySetId",
                principalTable: "EntitySetConfigurations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ChildEntitySetId",
                table: "EntitySetAverageMappingConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_EntitySetAverageMappingConfigurations_ChildEntitySetId",
                table: "EntitySetAverageMappingConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "WeightingSchemeDetails",
                table: "WeightingSchemes",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Definition",
                table: "VariableConfigurations",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SurveyIdToAllowedSegmentNames",
                table: "SubsetConfigurations",
                type: "varchar(max)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Waves",
                schema: "Reports",
                table: "SavedReports",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultFilters",
                schema: "Reports",
                table: "SavedReports",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "[]");

            migrationBuilder.AlterColumn<string>(
                name: "Breaks",
                schema: "Reports",
                table: "SavedReports",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breaks",
                schema: "SavedBreaks",
                table: "SavedBreakCombinations",
                type: "varchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Waves",
                table: "Parts",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SelectedEntityInstances",
                table: "Parts",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MultipleEntitySplitByAndMain",
                table: "Parts",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Breaks",
                table: "Parts",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BaseExpressionOverride",
                table: "Parts",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AverageType",
                table: "Parts",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityInstanceIdMeanCalculationValueMapping",
                table: "MetricConfigurations",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LinkedMetricNames",
                table: "LinkedMetric",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SurveyChoiceSetNames",
                table: "EntityTypeConfigurations",
                type: "varchar(max)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StartDateBySubset",
                table: "EntityInstanceConfigurations",
                type: "varchar(max)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EnabledBySubset",
                table: "EntityInstanceConfigurations",
                type: "varchar(max)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayNameOverrideBySubset",
                table: "EntityInstanceConfigurations",
                type: "varchar(max)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubsetIds",
                table: "Averages",
                type: "varchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "[]");

            migrationBuilder.AlterColumn<string>(
                name: "Group",
                table: "Averages",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReportVueConfiguration",
                table: "AllVueConfigurations",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations",
                column: "ParentEntitySetId",
                principalTable: "EntitySetConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
