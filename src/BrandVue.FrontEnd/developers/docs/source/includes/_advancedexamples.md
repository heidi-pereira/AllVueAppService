# Advanced examples

## Calculate 28 day weighted average for awareness metric

### Collect metadata

> Example: To retrieve start and end dates for a surveyset
```curl
GET /api/surveysets/UK
```
> Response:
```json
{
    "value": {
        "earliestResponseDate": "2017-05-17T00:00:00+00:00",
        "latestResponseDate": "2019-08-12T00:00:00+00:00"
    }
}
```
We need to call several endpoints to get the required metadata to calculate a weighted average. 

1) `GET /api/surveysets`  
Returns a list of available **surveysets**.  
A **surveyset** needs to be specified for all other api calls  

2) <code>GET /api/surveysets/<b>UK</b></code>  
Returns the start and end dates of data for the **UK surveyset**.  
You will need to request a date within the start and end dates to return data from the API  

3) <code>GET /api/surveysets/<b>UK</b>/classes</code>  
Returns a list of classes for the **UK surveyset**.  
A **class** needs to be specified when calling the responses API   

4) <code>GET /api/surveysets/<b>UK</b>/averages</code>  
Returns a list of averages for the **UK surveyset**.   
An average needs to be specified when calling the get weightings api

5) <code>GET /api/surveysets/<b>UK</b>/averages/<b>28Days</b>/weightings/<b>2019-02-28</b></code>  
Returns a list of the weightings for the **UK surveyset** for the **28 Day average** and end date **2019-02-28**

6) <code>GET /api/surveysets/<b>UK</b>/metrics</code> 
Returns a list of available metrics for the **UK surveyset**.  
Each metric includes additional data required to calculate a weighted average.

We now have a valid **surveyset**, **date**, **class**, **average**, **weightings** and **metric** for that average and date. Note in this example we are going to calculate a 28 day average and thus we can count back 28 days from the date we selected to get the data.  

#### Downloading the data we need

Now we can call the profile and class answerset endpoints iteratively requesting data from the 2019-02-01 to 2019-02-28. Note that the weightings we apply to the data must match the range for the weightings.  

In our example if we choose 2019-02-28 to request weightings and count back 28 days we get to 2019-02-01 so this is correct. **Be careful with non daily averages** See [weights](#get-weights) endpoint for more information.

1) <code>GET /api/surveysets/<b>UK</b>/profile/answers/<b>2019-02-01</b></code>   
Returns profile data by day in CSV format

2) <code>GET /api/surveysets/<b>UK</b>/classes/<b>brand</b>/answers/<b>2019-02-01</b></code>    
Returns class answerset data by day in CSV format

The data can now be concatenated together to form 28 days of profile data and 28 days of class answerset data. These two sets of data can be joined on Profile_Id (Profile_Id is a foreign key in class answers)

Next we can join the weightings data by weightingCellId (weightingCellId is a foreign key in profiles)

### Calculating the result
- Using the properties `Filter.QuestionId` and `BaseFilter.QuestionId` from metric **Awareness** we know which questions we are interested in. In this case it is **Consumer_Segment** for both.
- Using the properties `Filter.IncludeList` and `BaseFilter.IncludeList` from metric **Awareness** we filter out results where the answer for the base filter's question is not in the list of `BaseIncludeList` and attribute the results contained in the main question as 1 if in the `IncludeList` list and 0 otherwise. We can do this because the calculation type for Awareness is YesNo which is a boolean.
- Sum the attributed values of the **Awareness** metric multiplied by the scale factor for the weighting cell of the respondent.
- Divide through by the sum of the scale factors for the entire surveyset for a weighted average.
- A count of profile ids can also be taken for a sample size. 

The data returned covers all brands. Use the [instances](#get-class-instances) endpoint to get the id of the brand you are interested in.

## Downloading data to a CSV

Here is a sample powershell script [WriteCsv-FromBrandVueApi.ps1](../WriteCsv-FromBrandVueApi.ps1) that creates a CSV for respondent and brand response data.
The script requires 2 parameters:
 - **baseUrl** - your dashboard url
 - **accessToken** - your access token.

For analysis in excel you could join those csvs together on the profile id field using [Join-Csvs.ps1](../Join-Csvs.ps1)
The script requires 2 parameters:
 - **leftCsvPath** - path to respondents csv.
 - **rightCsvPath** - path to responses csv.

## Scripting metric calculation in a SQL database

> Example SQL for 'Familiarty' metric for brand id 159
```sql
SELECT
  COUNT(ProfileId) AS SampleSize,
  SUM(ResponseMetricValue * ScaleFactor) / SUM(ScaleFactor) AS MetricResult
FROM (SELECT
  sr.ProfileId,
  CASE 
    WHEN Familiarity IN (1, 2, 3, 4) -- {desiredMetric.Filter.Name} in {desiredMetric.Filter.IncludeList}
    THEN 1 ELSE 0
  END AS ResponseMetricValue,
  ScaleFactor
FROM SurveyResponses AS sr
INNER JOIN SurveyRespondents AS srs
  ON sr.ProfileId = srs.ProfileId
INNER JOIN ScaleFactors AS sf
  ON sf.WeightingCellId = srs.WeightingCellId
WHERE BrandId = 159 -- {desiredBrand.BrandId}
AND ConsumerSegmentBrandAdvantage IN (1, 2, 3, 4) -- {desiredMetric.BaseFilter.QuestionId} in {desiredMetric.BaseFilter.IncludeList}
) AS d
```

Here is a sample powershell [script](../Create-BrandVueDatabaseFromApi.ps1) that retrieves data from the API, creates a database schema and inserts some data into that schema. It has three dependencies that can be viewed and used individually. Please see powershell scripts for [retrieving data from the API](../BrandVueApi-Shim.ps1), [creating a database schema](../SqlServerDatabase.ps1) and [calculating the result](../SqlServerGetFinalValues.ps1). 
The script requires 3 parameters:
 - **baseUrl** - your dashboard url.
 - **accessToken** - your access token.
 - **instanceName** - an instance of sql server running in your machine.

The script currently fetches and inserts data for the metrics: 
 - **Consider** with base filter **ConsumerSegment**
 - **Familiarity** with base filter **ConsumerSegmentBrandAdvantage**

 From here we can calculate a weighted average for both **Consider** and **Familiarity**. For brevity the sample script doesn't store metric properties. 
 
 We have provided a sample SQL script on the right pane to calculate the sample size and weighted average from the collected data. In this example we have hardcoded the `Filter.IncludeList` and `BaseFilter.IncludeList`, but these should be obtained from the API and populated dynamically. Note that for the example `Filter.IncludeList` and `BaseFilter.IncludeList` for the metric **Familiarity** would have been `[1, 2, 3, 4]`.
