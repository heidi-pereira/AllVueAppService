param (
	[string]$instanceName = '(LocalDb)\MSSQLLocalDB',
	[string]$sqlUser,
	[string]$sqlPassword,
	[string]$databaseNameToCreate = 'BrandVueDataFromApi'
)

function Create-InitialTableStructure {
$createDBAndTableStructure = @"
USE master

IF DB_ID('$databaseNameToCreate') IS NOT NULL
	BEGIN
		ALTER DATABASE [$databaseNameToCreate] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
		DROP DATABASE IF EXISTS [$databaseNameToCreate]
	END

CREATE DATABASE [$databaseNameToCreate]

GO

USE [$databaseNameToCreate];

CREATE TABLE Multipliers
(
	WeightingCellId int,
	Multiplier decimal(19, 4)
)

CREATE TABLE SurveyProfiles
(
	ProfileId BIGINT Primary Key,
	COMPROUTE SMALLINT,
	StartDate Datetime2,
	WeightingCellId INT
)

CREATE TABLE SurveyResponses
(
	ProfileId BIGINT foreign key references SurveyProfiles(ProfileId),
	BrandId INT,
	Consider SMALLINT,
	ConsumerSegment SMALLINT,
	Familiarity SMALLINT
)

"@

Invoke-Sqlcmd -Query $createDBAndTableStructure -ServerInstance $instanceName -Database "master" -Trust
}

function BulkInsertToDB([string]$weightingsCsvPath, [string]$respondentsCsvPath, [string]$responsesCsvPath) {
	$weightingsAbsPath = Resolve-Path $weightingsCsvPath | select -ExpandProperty Path
	$respondentsAbsPath = Resolve-Path $respondentsCsvPath | select -ExpandProperty Path
	$responsesAbsPath = Resolve-Path $responsesCsvPath | select -ExpandProperty Path
	$insertScript = @"
USE [$databaseNameToCreate];

SET DATEFORMAT dmy;

BEGIN TRY
BULK INSERT Multipliers
FROM '$weightingsAbsPath'
WITH
(
	FORMAT='CSV', --SQL Server 2017+ only
    FIRSTROW = 2
)

BULK INSERT SurveyProfiles
FROM '$respondentsAbsPath'
WITH
(
	FORMAT='CSV',
    FIRSTROW = 2
)

BULK INSERT SurveyResponses
FROM '$responsesAbsPath'
WITH
(
	FORMAT='CSV',
    FIRSTROW = 2
)
END TRY
BEGIN CATCH
	USE master;
	THROW;
END CATCH

USE master;

"@

Write-Host "Inserting into database"
Invoke-Sqlcmd -Query $insertScript -ServerInstance $instanceName -Database $databaseNameToCreate -Trust
Write-Host "Success"
}