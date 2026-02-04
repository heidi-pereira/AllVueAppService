# Worked examples
See the right hand pane for concrete examples.

* All parameters are **case sensitive**.
* The order of response questions, and the contents of any arrays may change without warning.

#### Responses
Most endpoints return a json response, with the result nested within a `value` property.

The survey answers endpoints return a csv in order to reduce file size, and make it simpler to add directly into a database, see:
* [/api/surveysets/{surveyset}/profile/answers/{date}](#get-profile-answers)
* [/api/surveysets/{surveyset}/classes/{class}/answers/{date}](#get-class-answers)

## Discover what data is available
> Request:
```curl
GET /api/surveysets/UK/classes
```
> Response:
```json
{
    "value": [
        {
            "classId": "brand",
            "name": "brand",
            "childClassIds": []
        },
        {
            "classId": "product",
            "name": "product",
            "childClassIds": [
                "brand"
            ]
        }
    ]
}
```

To explore the data available from the Survey Response API or Calculated Metrics API you will need to have a valid **surveyset** to use in your requests. If you want to download response or metric data then you will need to use a **class** and date with the surveyset. 

#### To get **classes**  &amp; **averages** available for the UK **surveyset**

1) `GET /api/surveysets`  
Returns a list of available **surveysets**.  
A **surveyset** needs to be specified for all other API calls  

2) <code>GET /api/surveysets/<b>UK</b></code>  
Returns the start and end dates of data for the **UK surveyset**.  
You will need to request a date within the start and end dates to return data from the APIs  

3) <code>GET /api/surveysets/<b>UK</b>/classes</code>  
Returns a list of classes for the **UK surveyset**.  
A **class** needs to be specified when calling the APIs

4) <code>GET /api/surveysets/<b>UK</b>/averages</code>  
Returns a list of averages for the **UK surveyset**.  
An **average** needs to be specified when requesting data from the APIs   
  
## Calculated metrics API examples

<img src="source/images/metric.svg" alt="" /> Calculated metrics API license required.
{ .metric-api}

#### Get a brand metric for a chosen brand
> Request:
```curl
GET /api/surveysets/UK/classes/brand/metrics/Net-Buzz/monthly?instanceId=1&startDate=2018-01-01&endDate=2018-12-31
```
> Response:
```csv
EndDate,Value,SampleSize
2018-01-31,0.005001647,1173
2018-02-28,0.01277445,1146
2018-03-31,0.002071999,1156
2018-04-30,0.01358118,1186
2018-05-31,0.005597305,1147
2018-06-30,0.007488097,1124
2018-07-31,0.003319101,1270
2018-08-31,0.01332785,1192
2018-09-30,0.0103398,1204
2018-10-31,0.00704436,1145
2018-11-30,0.007060822,1146
2018-12-31,0.01834407,861
```

Metric data can be retrieved for any supported **class** and **metric** in a **surveyset**. When requesting metric data for a class you will need to specify an **instance ID** for the class as well as a supported **average**. Start and end dates for the date you want are also required. 

The API will return responses in JSON format. 

1) <code>GET /api/surveysets/<b>UK</b>/averages</code>  
Returns a list of averages for the **UK surveyset**.  
An average needs to be specified when requesting calculated metrics. 

2) <code>GET /api/surveysets/<b>UK</b>/classes/<b>brand</b>/metrics</code>  
Returns a list of available brand metrics for the **UK surveyset**.  
A **metric ID** needs to be specified when requesting calculated metrics. 

3) <code>GET /api/surveysets/<b>UK</b>/classes/<b>brand</b>/instances</code>  
Returns a list of brands available for the **UK surveyset**.
A **brand instance ID** is required when requesting calculated metrics. 

4) <code>GET /api/surveysets/<b>UK</b>/classes/brand/metrics/<b>Net-Buzz</b>/<b>monthly</b><br />?instanceId=1&startDate=2018-01-01&endDate=2018-12-31</code>  
Returns **Net Buzz monthly** data, for 2018, for brand **instance ID 1**. 

#### Get a profile metric
> Request:
```curl
GET api/surveysets/UK/profile/metrics/Age/monthly?startDate=2018-01-01&endDate=2018-12-31 
```
> Response:
```csv
EndDate,Value,SampleSize
2018-01-31,0.005001647,1173
2018-02-28,0.01277445,1146
2018-03-31,0.002071999,1156
2018-04-30,0.01358118,1186
2018-05-31,0.005597305,1147
2018-06-30,0.007488097,1124
2018-07-31,0.003319101,1270
2018-08-31,0.01332785,1192
2018-09-30,0.0103398,1204
2018-10-31,0.00704436,1145
2018-11-30,0.007060822,1146
2018-12-31,0.01834407,861
```

Profiles are treated as a unique class in the APIs and do not need an instance ID. Profile metrics are about survey respondents e.g. age, gender and SEG. When requesting profile metrics you will need to specify a supported average. Start and end dates for the date you want are also required. 

The API will return responses in JSON format. 

1) <code>GET /api/surveysets/<b>UK</b>/averages</code>  
Returns a list of averages for the **UK surveyset**.  
An average needs to be specified when requesting calculated metrics. 

2) <code>GET /api/surveysets/<b>UK</b>/<b>profile</b>/metrics</code>  
Returns a list of available profile metrics for the **UK surveyset**.  
A **metric ID** needs to be specified when requesting calculated metrics. 

3) <code>GET /api/surveysets/<b>UK</b>/profile/metrics/<b>Age</b>/<b>monthly</b>?startDate=2018-01-01&endDate=2018-12-31</code>  
Returns **Age monthly** data, for 2018.

## Survey Response API examples
<img src="source/images/response.svg" alt="" /> Survey response API license required.
{ .response-api}

#### Export the latest data

Survey answers can be obtained on a daily basis from the API. The most recent day you can request data for is always the latest complete day e.g. yesterday. You may want to retrieve data on a daily basis to push into your data warehouse, or feed into other BI systems, to provide an up-to-date view on the data.  

The API will return survey answers as CSV files.  

1) <code>GET /api/surveysets/<b>UK</b>/profile/answers/<b>2019-01-31</b></code>  
Returns a CSV file of the profile answers for the **UK surveyset** for the **31st January 2019**.  
Only needs to be called once each day.   

2) <code>GET /api/surveysets/<b>UK</b>/classes/<b>brand</b>/answers/<b>2019-01-31</b>\[?includeText=true\]</code>  
Returns a CSV file of the **brand class** answers for the **UK surveyset** for the **31st January 2019**.  
Only needs to be called once each day. `includeText` is optional (default `false`) and if set to `true` 
will also return any reponses for questions that allow free text.


#### Get weightings for an average period
> Request:
```curl
GET /api/surveysets/UK/averages/monthly/weights/2019-01-31
```
> Response:
```json
{
    "value": [
        {
            "weightingCellId": 1,
            "multiplier": 0.6108187
        },
		...
  ]
}
```

Weightings are required if you want to reproduce values used in Savanta dashboards.   
For example, to get weightings for UK [surveyset](#surveyset) for January 2019 data (i.e. Monthly [average](#average-type)):

1) <code>GET /api/surveysets/<b>UK</b>/averages</code>  
Returns a list of averages for the **UK surveyset**.  
An average needs to be specified when calling the get weightings api.

2) <code>GET /api/surveysets/<b>UK</b>/weightingCells</code>  
Returns a list of the weighting cell ids and associated profile questions used for the **UK surveyset**.  
The weighting cell id is the key that links profile answers and weights.

3) <code>GET /api/surveysets/<b>UK</b>/averages/<b>monthly</b>/weights/<b>2019-01-31</b></code>  
Returns a list of the weights for the  for the **UK surveyset** for **monthly average** for **January 2019**.