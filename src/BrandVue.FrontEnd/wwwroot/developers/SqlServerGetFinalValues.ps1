param (
	[string]$instanceName = '.\sql2022',
	[string]$sqlUser,
	[string]$sqlPassword,
	[string]$databaseNameToReadFrom = 'BrandVueDataFromApi'
)

function  Calculate-FamiliaritySampleSizeAndAverage([int]$desiredBrandId) {
$getMetricResult = @"
USE [$databaseNameToReadFrom];

SELECT
  SUM(ResponseMetricValue * Multiplier) / SUM(Multiplier) AS MetricResult,
  COUNT(ProfileId) AS SampleSize
FROM (SELECT
  sr.ProfileId,
  CASE
    WHEN Familiarity IN (1, 2, 3, 4)
    THEN 1 ELSE 0
  END AS ResponseMetricValue,
  Multiplier
FROM SurveyResponses AS sr
INNER JOIN SurveyProfiles AS srs
  ON sr.ProfileId = srs.ProfileId
INNER JOIN Multipliers AS sf
  ON sf.WeightingCellId = srs.WeightingCellId
WHERE BrandId = 22
AND ConsumerSegment IN (1, 2, 3, 4)
) AS d
"@

return Invoke-Sqlcmd -Query $getMetricResult -ServerInstance $instanceName -Database $databaseNameToReadFrom -Trust
}