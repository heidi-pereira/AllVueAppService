using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData;

/// <inheritdoc />
public partial class AddRenameSurveyGroupProcedure : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var sql = @"
            CREATE PROCEDURE renameSurveyGroup
                @oldName NVARCHAR(200),
                @newName NVARCHAR(200)
            AS
            BEGIN
                UPDATE dbo.Averages SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE Reports.SavedReports SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE Reports.DefaultSavedReports SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.WeightingPlans SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.WeightingTargets SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.EntityInstanceConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.EntityTypeConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.WeightingStrategies SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.CustomPeriods SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE SavedBreaks.SavedBreakCombinations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE ReportVue.Projects SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.Weightings SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.VariableConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE SavedBreaks.DefaultBreaksForSubProducts SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.AllVueConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.MetricConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.SubsetConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.LinkedMetric SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.Pages SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.Panes SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.EntitySetConfigurations SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.Parts SET SubProductId = @newName WHERE SubProductId = @oldName;
                UPDATE dbo.ResponseWeightingContexts SET SubProductId = @newName WHERE SubProductId = @oldName;
            END";

        migrationBuilder.Sql(sql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS renameSurveyGroup");
    }
}
