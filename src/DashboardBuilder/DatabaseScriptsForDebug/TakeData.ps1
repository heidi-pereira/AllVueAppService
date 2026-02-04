param (
    [string]$Surveys = "",
    [string]$SqlInstance = ".\sav1",
    [string]$OutputDir = ".\bcp100k",
    [string]$ZipFileName = "bcp100k.zip"
)

# Script to take Data from SurveyPortalMorar to a zip file for local debug use
# Put a comma separated list of surveys in $Surveys above (e.g. from the map file)
# See https://github.com/MIG-Global/Dashboard-Builder/#with-data for mor details

if($Surveys) {
   $surveysWhere = "WHERE surveyId in ($Surveys)"
}else {
   $surveysWhere = ""
}

New-Item -ItemType Directory -Force -Path $OutputDir

bcp "SELECT TOP (100000) * FROM [SurveyPortalMorar].[dbo].surveyResponse $surveysWhere order by responseId desc;" queryout "$OutputDir\surveyResponse.txt" -T -c -S "$SqlInstance";

bcp "
SELECT *
  FROM [SurveyPortalMorar].[dbo].[panelRespondents]
  where panelRespondentId in (

  select distinct S.respondentId
  from  (
	  SELECT TOP (100000)  respondentId
	  FROM [SurveyPortalMorar].[dbo].surveyResponse
	  $surveysWhere
	  order by responseId desc
  ) as S);
" queryout "$OutputDir\panelRespondents.txt" -T -c -S "$SqlInstance";

bcp "
SELECT *
  FROM [SurveyPortalMorar].[dbo].[data]	
  where responseId in (

  select distinct S.responseId
  from  (
	  SELECT TOP (100000)  responseId
	  FROM [SurveyPortalMorar].[dbo].surveyResponse
	  $surveysWhere
	  order by responseId desc
  ) as S);
" queryout "$OutputDir\data.txt" -T -c -S "$SqlInstance";

bcp "
SELECT *
  FROM [SurveyPortalMorar].[dbo].[workingData]
  where responseId in (

  select distinct S.responseId
  from  (
	  SELECT TOP (100000)  responseId
	  FROM [SurveyPortalMorar].[dbo].surveyResponse
	  $surveysWhere
	  order by responseId desc
  ) as S);
" queryout "$OutputDir\workingData.txt" -T -c -S "$SqlInstance";

bcp "
SELECT *
  FROM [SurveyPortalMorar].[dbo].[surveys]
  $surveysWhere;
" queryout "$OutputDir\surveys.txt" -T -c -S "$SqlInstance";

bcp "
SELECT *
  FROM [SurveyPortalMorar].[dbo].[surveyStructures]
  where surveyStructureId in (
	  select distinct S.surveyStructureId
	  from  (
		  SELECT [surveyStructureId]
		  FROM [SurveyPortalMorar].[dbo].[surveys]
		  $surveysWhere
	  ) as S);
" queryout "$OutputDir\surveyStructures.txt" -T -c -S "$SqlInstance";

bcp "
SELECT *
  FROM [SurveyPortalMorar].[dbo].[surveySegments]
  where surveyStructureId in (
	  select distinct S.surveyStructureId
	  from  (
		  SELECT [surveyStructureId]
		  FROM [SurveyPortalMorar].[dbo].[surveys]
		  $surveysWhere
	  ) as S);
" queryout "$OutputDir\surveySegments.txt" -T -c -S "$SqlInstance";

if (Test-Path "$pwd\$ZipFileName")
{
  Remove-Item "$pwd\$ZipFileName"
}

Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory("$pwd\$OutputDir", "$pwd\$ZipFileName")
