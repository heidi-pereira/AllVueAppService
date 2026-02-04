using System.IO;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddBrandAnalysisPages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Migrations\\SqlScripts\\AddBrandAnalysisPages.sql");
            var sql = File.ReadAllText(sqlScriptPath);
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"Print 'Starting migration rollback script'
SET NOCOUNT Off;

DECLARE @pageName NVARCHAR(450)
SET @pageName = 'Brand Analysis'

DECLARE @partName NVARCHAR(450)
SET @partName = 'BrandAnalysis_1'

DELETE [dbo].[Panes] WHERE PageName = @pageName
DELETE [dbo].[Parts] WHERE PaneId = @partName
DELETE [dbo].[Pages] WHERE [Name] = @pageName

DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandAdvocacy%'
DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandBuzz%'
DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandLove%'
DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandUsage%'

DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandAdvocacy%'
DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandBuzz%'
DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandLove%'
DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandUsage%'

DELETE [dbo].[Pages] WHERE [Name] IN ('Brand Analysis Advocacy', 'Brand Analysis Buzz', 'Brand Analysis Love', 'Brand Analysis Usage')

DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Affinity (Analysis)';

DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) FR';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) ES';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) DE';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) US';

DELETE MetricConfigurations WHERE ProductShortCode = 'brandvue' AND Name IN ('Promoters')
DELETE MetricConfigurations WHERE ProductShortCode = 'finance' AND Name = 'Promotors Advocacy'";

            migrationBuilder.Sql(sql);
        }
    }
}
