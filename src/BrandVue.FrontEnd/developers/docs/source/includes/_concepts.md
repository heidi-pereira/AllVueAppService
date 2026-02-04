# API Concepts

## API model diagram

<a href="source/images/api_model.svg" class="diagram"><img src="source/images/api_model.svg" alt="Diagram showing the different API endpoints and the relationship between responses from endpoints" /></a>
<p>API model showing relationship between objects returned from endpoints within a surveyset</p> 


## Survey definition
These endpoints are available with any license, they typically are used to retrieve metadata necessary to be used in further API calls to retrieve survey responses or calculated metrics. 

### Surveyset
> Geographical examples: 
> * **UK** (United Kingdom)
> * **US** (United States)
> * **DE** (Germany)

A surveyset contains responses to a common set of questions asked in a common context within a survey project. It's usually best to aggregate data within a surveyset first before comparing between surveysets. Many projects will only have a small number of surveysets, often only one.

Even if the questions are identical, the respondent's context can differ between surveysets. For example in a fashion survey project, the majority of men and women would be considering substantially different products - therefore **UKWomen** and **UKMen** would be separate surveysets for that survey project.

See [/api/surveysets](#get-surveysets)

### Question
> **Range**:  
> Min : 0  
> Max : 10    

> **Listed answers**:  
> -999: Don't know  
> 1 : I have NEVER made a complaint  
> 2 : I have made a complaint OVER A YEAR AGO  

> **Question with checkbox**:   
> -99 : Unchecked  
> 1 : Checked  

A question corresponds to a survey question. It includes the question text, and the defintition of all possible answers to the question.
Answers are typically stored as a numeric value, either representing:
* A direct numerical answer from an allowed range
* An id matching a single-select listed answer
* An id representing the selection status of an answer

For ids, negative numbers are usually used to represent special cases such as "don't know".

Each question has a name which acts as a unique identifier for reference within the surveyset.

* [/api/surveysets/{surveyset}/questions](#get-questions)

### Class
Common examples: Brand, Product

A class is a key focus area for the survey questions. For example: A respondent may be asked the same set of questions about several brands. The response will have a brand id associated with it. Each brand in this example is a **class instance**.
Each class has its own set of questions - the ones asked within that class context.

Endpoint references:
* [/api/surveysets/{surveyset}/classes](#get-classes)
* [/api/surveysets/{surveyset}/classes/{class}/instances](#get-class-instances)

## Metric results
Returns aggregated results for a metrics visible in the dashboard. If you require filtering or more advanced calculations, see [Custom result aggregation](#custom-result-aggregation).

### Average type
Two different types of average are offered for results:
* Daily rolling: One result per day.
  * Weighting is applied over the entire average period.
* Monthly fixed: One result per fixed period. Aligned to calendar months (monthly, quarterly, annual, etc).
  * Each month is weighted independently.

Endpoint reference: [/api/surveysets/{surveyset}/averages](#get-averages)

### Metric

A metric describes how to calculate a results seen in the dashboard by aggregating answers to 1 or 2 questions across all responses.
For a percentage, the "baseFilter" determines the denominator and the "filter" determines the numerator.

> List: `[1, 2, 3, 4]`

> Range: `{ "Min": 18, "Max": 74 }`

The filter and base filter can have several different value inclusion criteria:
* List - This means "any of", and can be implemented with the SQL IN operator.
* Range - This is an inclusive range and can be implemented with the SQL BETWEEN operator.
* NotNull - Any non empty value. Assuming empty values were mapped to null, this can be implemented with a NOT NULL expression.

There are two types of metrics:
* Class metric (e.g. Brand metric): The filter or base filter use a question linked to a defined class.
* Profile metric: The filter and base filter only relate to questions asked with no defined class context.


### MetricResult

<img src="source/images/metric.svg" alt="" /> Calculated metrics API license required.
{ .metric-api}

> MetricResult contents:
> EndDate: 2019-10-31
> Value: 44.56578
> SampleSize: 5015

The result of applying aggregating a metric over all responses within the requested time period. For percentage ("yn") metrics the value will be between 0 and 1.
For class metrics, the figure is only for responses for that class instance. e.g. All responses for a specific brand.
For profile metrics, the figure is for all responses.

## Custom result aggregation

<img src="source/images/response.svg" alt="" /> Survey response API license required.
{ .response-api}

For queries requiring custom segmentation, other data sources or filtering, you'll need to retrieve the survey answers. This offers complete flexibility, but requires a deeper understanding of the data. To ensure consistency with other results, we'd suggest first using this data to repdroduce a figure available from the dashboard / MetricResults API.

### Profile
A profile represents a person's complete survey run-through. If the same person completes the survey next month they will get a new profile id.
Each profile belongs to a [weighting cell](#weighting-cell).

### Answerset
An answerset is a Profile's answers for a set of questions.
Each profile will only have answers for a subset of the questions in order to keep the survey to a manageable length.

#### Profile answerset
> Profile answerset contents:
> * ProfileId: **346**
> * Age: **36**
> * How many times a month do you eat out at a restaurant?: **3**

A profile answerset includes questions at the profile level. These help us to place the profile in a weighting cell as well as better understand their habits and preferences. Each profile is defined by exactly one profile response. Each profile response contains:
* [Weighting cell](#weighting-cell) id
* Survey start time
* Answer mappings for profile questions

Endpoint reference: [/api/surveysets/{surveyset}/profile/answers/{date}](#get-profile-answers)

#### Class answerset
> Brand X class answerset contents:
> * ProfileId: **346**
> * Brand: **Brand X**
> * Where did you last shop?: **Brand X**  
> * On your last visit to Brand X, how much did you spend?: **&pound;36**

These are class level questions and dig deeper into the respondents experience with a particular class instance. A respondent can be asked about more than one class instance in a survey. These are shown as a separate row in the response. Each class answerset contains:  
* Answer mappings for the class' questions.
* Class id of the class instance.

Endpoint reference: [/api/surveysets/{surveyset}/classes/{class}/answers/{date}](#get-class-answers)

### Weighting

When aggregating responses over time, to create the most population-accurate result BrandVue applies weighting.

Endpoint reference: [/api/surveysets/{surveyset}/averages/{average}/weights/{date}](#get-weights)

### Weighting cell

> Consider a time period where 10% of our survey respondents within a time period were Men aged 25-34.

> If the population contained 15% Men aged 25-34, then they are underrepresented in the sample, and will be scaled up.

Respondents are classified into a weighting cell based on factors such as age, gender, location and socio-economic status (part of their profile answerset).

We aim to capture data from a nationally representative sample for each of these weighting cells, normalised over 7 day periods. However, inevitably there is some variation in each time window.

The BrandVue dashboard applies weighting during statistical analysis to counteract imperfections in sampling.

Endpoint reference: [/api/surveysets/{surveyset}/weightingcells](#get-weighting-cells)
