namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddingMetricsAndPagesConfigRollbackSPs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(_rollBackMetricsConfiguration_CreateSP);
            migrationBuilder.Sql(_rollBackPagesConfiguration_CreateSP);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropStoredProcedure("RollBackMetricsConfiguration");
            migrationBuilder.DropStoredProcedure("RollBackPagesConfiguration");
        }

        private static string _rollBackMetricsConfiguration_CreateSP = @"
CREATE PROCEDURE [dbo].[RollBackMetricsConfiguration]
    @lastKnownTimeWhenConfigWasCorrect AS DATETIME2
AS
BEGIN

	DROP TABLE IF EXISTS #MetricConfigurationSnapshot

	SELECT *
	INTO #MetricConfigurationSnapshot
	FROM [dbo].[MetricConfigurations] FOR SYSTEM_TIME AS OF @lastKnownTimeWhenConfigWasCorrect

	DECLARE @insertQuery NVARCHAR(MAX)
	DECLARE @columnList NVARCHAR(MAX);

	--Select all columns from MetricConfigurations
	SELECT @columnList = STUFF(
		(
			SELECT ', [' + [name] + ']'
			FROM sys.columns 
			WHERE
				OBJECT_ID = OBJECT_ID('dbo.MetricConfigurations')
				AND [name] NOT IN ('SysStartTime', 'SysEndTime') 
			ORDER BY
				column_id
			FOR XML PATH('')
		)
		,1 --start at the first character
		,1 --take one character
		,''); --replace with nothing to get rid of the first comma

	--Build the insert query
	SELECT @insertQuery = N'INSERT INTO dbo.MetricConfigurations (' + @columnList + ') SELECT * FROM #MetricConfigurationSnapshot';

	BEGIN TRAN;
	BEGIN TRY

		--Perform updates		
		SET IDENTITY_INSERT [dbo].[MetricConfigurations] ON
		DELETE FROM [dbo].[MetricConfigurations]
		EXEC(@insertQuery)
		SET	IDENTITY_INSERT [dbo].[MetricConfigurations] OFF

		COMMIT TRAN
	END TRY
	BEGIN CATCH
		ROLLBACK TRAN;
		SET	IDENTITY_INSERT [dbo].[MetricConfigurations] OFF;
		THROW;
	END CATCH

END
";

        private static string _rollBackPagesConfiguration_CreateSP = @"
CREATE PROCEDURE [dbo].[RollBackPagesConfiguration]
    @lastKnownTimeWhenConfigWasCorrect AS DATETIME2
AS
BEGIN

	DROP TABLE IF EXISTS #PagesSnapshot
	DROP TABLE IF EXISTS #PanesSnapshot
	DROP TABLE IF EXISTS #PartsSnapshot

	SELECT *
	INTO #PagesSnapshot
	FROM [dbo].Pages FOR SYSTEM_TIME AS OF @lastKnownTimeWhenConfigWasCorrect

	SELECT *
	INTO #PanesSnapshot
	FROM [dbo].Panes FOR SYSTEM_TIME AS OF @lastKnownTimeWhenConfigWasCorrect

	SELECT *
	INTO #PartsSnapshot
	FROM [dbo].Parts FOR SYSTEM_TIME AS OF @lastKnownTimeWhenConfigWasCorrect

	DECLARE @pagesInsertQuery NVARCHAR(MAX);
	DECLARE @panesInsertQuery NVARCHAR(MAX);
	DECLARE @partsInsertQuery NVARCHAR(MAX);
	DECLARE @columnList NVARCHAR(MAX);

	--Select all columns
	SELECT @columnList = STUFF(
		(
			SELECT ', [' + [name] + ']'
			FROM sys.columns 
			WHERE
				OBJECT_ID = OBJECT_ID('dbo.Pages')
				AND [name] NOT IN ('SysStartTime', 'SysEndTime') 
			ORDER BY
				column_id
			FOR XML PATH('')
		)
		,1 --start at the first character
		,1 --take one character
		,''); --replace with nothing to get rid of the first comma
	--Build the insert query
	SELECT @pagesInsertQuery = N'INSERT INTO dbo.Pages (' + @columnList + ') SELECT * FROM #PagesSnapshot';

	--Select all columns
	SELECT @columnList = STUFF(
		(
			SELECT ', [' + [name] + ']'
			FROM sys.columns 
			WHERE
				OBJECT_ID = OBJECT_ID('dbo.Panes')
				AND [name] NOT IN ('SysStartTime', 'SysEndTime') 
			ORDER BY
				column_id
			FOR XML PATH('')
		)
		,1 --start at the first character
		,1 --take one character
		,''); --replace with nothing to get rid of the first comma
	--Build the insert query
	SELECT @panesInsertQuery = N'INSERT INTO dbo.Panes (' + @columnList + ') SELECT * FROM #PanesSnapshot';

	--Select all columns
	SELECT @columnList = STUFF(
		(
			SELECT ', [' + [name] + ']'
			FROM sys.columns 
			WHERE
				OBJECT_ID = OBJECT_ID('dbo.Parts')
				AND [name] NOT IN ('SysStartTime', 'SysEndTime') 
			ORDER BY
				column_id
			FOR XML PATH('')
		)
		,1 --start at the first character
		,1 --take one character
		,''); --replace with nothing to get rid of the first comma
	--Build the insert query
	SELECT @partsInsertQuery = N'INSERT INTO dbo.Parts (' + @columnList + ') SELECT * FROM #PartsSnapshot';

	BEGIN TRAN;
	BEGIN TRY		
		
		--Perform updates
		SET IDENTITY_INSERT [dbo].Pages ON
		DELETE FROM [dbo].Pages
		EXEC(@pagesInsertQuery)
		SET IDENTITY_INSERT [dbo].Pages OFF
				
		SET IDENTITY_INSERT [dbo].Panes ON
		DELETE FROM [dbo].Panes
		EXEC(@panesInsertQuery)
		SET IDENTITY_INSERT [dbo].Panes OFF
				
		SET IDENTITY_INSERT [dbo].Parts ON
		DELETE FROM [dbo].Parts
		EXEC(@partsInsertQuery)
		SET IDENTITY_INSERT [dbo].Parts OFF

		COMMIT TRAN
	END TRY
	BEGIN CATCH
		ROLLBACK TRAN;
		SET IDENTITY_INSERT [dbo].Pages OFF;
		SET IDENTITY_INSERT [dbo].Panes OFF;
		SET IDENTITY_INSERT [dbo].Parts OFF;
		THROW;
	END CATCH

END
";
    }
}
