/********************************************************************************
 * SCRIPT TO CONFIGURE CHANGE TRACKING FOR SPECIFIC TABLES (REVISED)
 ********************************************************************************/

-- ================================================================================
-- Step 1: Configuration
-- ================================================================================
-- IMPORTANT: Set your target database and username here.
DECLARE @dbName SYSNAME = 'BrandVueMetaTest';
DECLARE @userName SYSNAME = 'airflow_user';
DECLARE @sqlCmd NVARCHAR(MAX);

-- Set the context to the correct database
IF DB_ID(@dbName) IS NULL
BEGIN
    PRINT 'ERROR: Database ' + QUOTENAME(@dbName) + ' does not exist.';
    RETURN;
END
-- Use dynamic SQL to change database context, as USE must be in its own batch.
SET @sqlCmd = 'USE ' + QUOTENAME(@dbName) + ';';
EXEC sp_executesql @sqlCmd;
PRINT 'Database context set to ' + QUOTENAME(@dbName);

-- Check if the user exists
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @userName)
BEGIN
    PRINT 'ERROR: User ' + QUOTENAME(@userName) + ' does not exist in the database. Please create the user first.';
    RETURN;
END

-- ================================================================================
-- Step 2: Ensure Change Tracking is Enabled on the Database (FIXED)
-- ================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID())
BEGIN
    PRINT 'Change Tracking is not enabled for the database. Enabling it now...';
    -- **FIX**: Wrapped ALTER DATABASE in dynamic SQL to avoid batch errors.
    SET @sqlCmd = N'ALTER DATABASE ' + QUOTENAME(@dbName) + N' SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);';
    EXEC sp_executesql @sqlCmd;
    PRINT 'Change Tracking has been enabled for database ' + QUOTENAME(@dbName);
END
ELSE
BEGIN
    PRINT 'Change Tracking is already enabled for the database.';
END
GO

-- Set database context again after GO batch separator
USE BrandVueMetaTest;
GO

-- ================================================================================
-- Step 3: Identify and Store the List of Tables to Enable CT On
-- ================================================================================
IF OBJECT_ID('tempdb..#TablesToEnableCT') IS NOT NULL
    DROP TABLE #TablesToEnableCT;

CREATE TABLE #TablesToEnableCT (
    TABLE_SCHEMA SYSNAME,
    TABLE_NAME SYSNAME
);

INSERT INTO #TablesToEnableCT (TABLE_SCHEMA, TABLE_NAME)
SELECT TABLE_SCHEMA, TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_SCHEMA IN ('dbo')
  AND TABLE_NAME NOT LIKE '%history'
  AND TABLE_NAME NOT LIKE '%-responseQuotas'
  AND TABLE_NAME NOT LIKE '%feature%'
  AND TABLE_NAME NOT LIKE '%allvue%'
  AND (
      TABLE_NAME LIKE '%configuration%'
      OR TABLE_NAME LIKE '%weight%'
      OR TABLE_NAME LIKE '%average%'
      OR TABLE_NAME LIKE '%customperiods%'
  );

PRINT 'Identified ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' tables to have Change Tracking enabled.';
GO

-- ================================================================================
-- Step 4: Loop Through Tables to Enable CT and Grant Permissions
-- ================================================================================
DECLARE @schemaName_Enable SYSNAME, @tableName_Enable SYSNAME;
DECLARE @sql_Enable NVARCHAR(MAX);
DECLARE @userName_Enable SYSNAME = 'airflow_user';

DECLARE cur_EnableCT CURSOR FOR
    SELECT TABLE_SCHEMA, TABLE_NAME FROM #TablesToEnableCT;

OPEN cur_EnableCT;
FETCH NEXT FROM cur_EnableCT INTO @schemaName_Enable, @tableName_Enable;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        SET @sql_Enable = N'ALTER TABLE ' + QUOTENAME(@schemaName_Enable) + '.' + QUOTENAME(@tableName_Enable) + ' ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);';
        PRINT 'Executing: ' + @sql_Enable;
        EXEC sp_executesql @sql_Enable;

        SET @sql_Enable = N'GRANT VIEW CHANGE TRACKING ON ' + QUOTENAME(@schemaName_Enable) + '.' + QUOTENAME(@tableName_Enable) + ' TO ' + QUOTENAME(@userName_Enable) + ';';
        PRINT 'Executing: ' + @sql_Enable;
        EXEC sp_executesql @sql_Enable;
    END TRY
    BEGIN CATCH
        PRINT 'ERROR enabling Change Tracking or granting permission on table ' + QUOTENAME(@schemaName_Enable) + '.' + QUOTENAME(@tableName_Enable);
        PRINT 'Error Message: ' + ERROR_MESSAGE();
    END CATCH

    FETCH NEXT FROM cur_EnableCT INTO @schemaName_Enable, @tableName_Enable;
END

CLOSE cur_EnableCT;
DEALLOCATE cur_EnableCT;
PRINT 'Finished enabling Change Tracking and granting permissions.';
GO

-- ================================================================================
-- Step 5: Loop Through Other Tables to Disable CT (FIXED)
-- ================================================================================
DECLARE @schemaName_Disable SYSNAME, @tableName_Disable SYSNAME;
DECLARE @sql_Disable NVARCHAR(MAX);

DECLARE cur_DisableCT CURSOR FOR
    SELECT
        s.name AS TABLE_SCHEMA,
        t.name AS TABLE_NAME
    FROM sys.change_tracking_tables ct
    JOIN sys.tables t ON ct.object_id = t.object_id
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE 
        s.name = 'dbo'
        AND NOT EXISTS (
            SELECT 1 
            FROM #TablesToEnableCT target
            -- **FIX**: Added COLLATE DATABASE_DEFAULT to resolve the collation conflict.
            WHERE target.TABLE_SCHEMA = s.name COLLATE DATABASE_DEFAULT
              AND target.TABLE_NAME = t.name COLLATE DATABASE_DEFAULT
        );

OPEN cur_DisableCT;
FETCH NEXT FROM cur_DisableCT INTO @schemaName_Disable, @tableName_Disable;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        SET @sql_Disable = N'ALTER TABLE ' + QUOTENAME(@schemaName_Disable) + '.' + QUOTENAME(@tableName_Disable) + ' DISABLE CHANGE_TRACKING;';
        PRINT 'Disabling CT on table not in target list: ' + QUOTENAME(@schemaName_Disable) + '.' + QUOTENAME(@tableName_Disable);
        EXEC sp_executesql @sql_Disable;
    END TRY
    BEGIN CATCH
        PRINT 'ERROR disabling Change Tracking on table ' + QUOTENAME(@schemaName_Disable) + '.' + QUOTENAME(@tableName_Disable);
        PRINT 'Error Message: ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM cur_DisableCT INTO @schemaName_Disable, @tableName_Disable;
END

CLOSE cur_DisableCT;
DEALLOCATE cur_DisableCT;

DROP TABLE #TablesToEnableCT;
PRINT 'Finished disabling Change Tracking on non-target tables.';
GO

-- ================================================================================
-- Step 6: Verification Query
-- ================================================================================
PRINT '--- Verification Report ---';
PRINT 'Listing all tables in the ''dbo'' schema and their Change Tracking status:';

SELECT 
    s.name AS SchemaName,
    t.name AS TableName,
    CASE 
        WHEN ct.object_id IS NOT NULL THEN 'ENABLED'
        ELSE 'DISABLED'
    END AS ChangeTrackingStatus
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN sys.change_tracking_tables ct ON t.object_id = ct.object_id
WHERE s.name = 'dbo'
ORDER BY s.name, t.name;
GO