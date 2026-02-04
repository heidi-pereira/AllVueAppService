--Calculation logic - Familliarity
SELECT
  COUNT(RespondentId) AS SampleSize,
  SUM(ResponseMetricValue * ScaleFactor) / SUM(ScaleFactor) AS MetricResult
FROM (SELECT
  sr.RespondentId,
  CASE 
    WHEN Familiarity IN (1, 2, 3, 4) -- {desiredMetric.Field} in {desiredMetric.TrueVals}
    THEN 1 ELSE 0
  END AS ResponseMetricValue,
  ScaleFactor
FROM SurveyResponses AS sr
INNER JOIN SurveyRespondents AS srs
  ON sr.RespondentId = srs.RespondentId
INNER JOIN ScaleFactors AS sf
  ON sf.DemographicCellId = srs.DemographicCellId
WHERE BrandId = 159 -- {desiredBrand.id}
AND ConsumerSegmentBrandAdvantage IN (1, 2, 3, 4) -- {desiredMetric.BaseField} in {desiredMetric.BaseVals}
) AS d
