## Service architecture

The word service here is used conceptually. Two services may share a function app, or one service may be split over two. Keep things together (but well-namespaced) by default to simplify debugging and minimise admin/api-chattiness.

### Metadata/result service

The UI will log all requests to these REST APIs and they will be able to return the snowflake query used, so that we can give people the API call and the snowflake SQL to replicate it themselves.

#### Result metadata functions (readonly):

* Backend is a very thin set of azure functions which act as a rest api exactly mapping to snowflake tables (possibly even generated from them).
* UI populates/maintains its redux metadata stores of variables/metrics/entities with  rtk-query calling that rest api.
* The rest api passes through the appropriate user id or role so snowflake can use row-level security policies to return only the stuff they are allowed to access.

#### Result function:

* A single results endpoint should essentially look like the pivot table builder in excel. i.e.
  * List of rows (including nesting and nets)
  * List of columns (including nesting and nets)
  * List of filters
  * Any details of how to calculate/represent values, e.g. significance scoring parameters
Because all charts will render from this, switching between table and chart types is a UI side decision, and exporting a raw data table is trivial for any visualization.
 

### Config service

These are simple CRUD REST APIs that check auth and read/write config models in SQL Server.
At some point we may move directly to write to Postgres within snowflake (to keep referential integrity but avoid replication faff/delay).

#### Result metadata config functions:

* Endpoints to change add/change variables/metrics
* Endpoints to configure entities/averages/weightings

#### Visualization config functions
* Reports/pages/etc.

### Auth service

* For metadata/results, auth will be pushed down into the snowflake query to deal with what's accessible so that API and snowflake requests return the exact same stuff for the user.
* But for config, the UI needs to know "should I show this button or menu item to the user". The auth service answers populates the model for what the user can do, and is called by the other config services to enforce it.

## Background

### It's all AllVue

BrandVue is just a very customised AllVue. All user-facing functionality developed should work in AllVue and BrandVue and be a shared code used in both. e.g. Filters should be the same implementation. Flag up to GH/RP/HP where this can't be the case within the time window. We'll also do some explicit tasks to unify the two, and some config migration projects.
One explicit integration is likely needed. We should decide whether the reports view becomes a page in brandvue, or whether the brandvue charts become a type of report in allvue.

### What is AllVue

Input: A data source. In the context of our survey data, that means one or more survey ids that have some data people want to query together.
Output:
* Calculated metrics via: REST API, web app visualization, excel download (via the web app), and powerpoint (reporting).
* Raw data and data shape description via REST API

Core capabilities:
* Allow reasonably non-technical user control of multi-dimensional data (i.e. filter based on "entities")
* Weight data by a subset of the dimensions (i.e. "Quota cell" weighting)
* Compute multi-dimensional metrics based on multiple variables
* Merge together both data and metadata from many surveys into a single coherent set of responses that can be analysed together

Aims:
* From the input of a survey id we should always be able to show a basic zero-config dashboard to get a quick sense of the data while the survey's in flight.
* We should then be able to tweak the config, and layer components/features mainly seen in BrandVue so far (e.g. Scorecards, home page) on top of that.
* Projects: Ultimately we should be able to give that "project" a user friendly name/url and have it appear in the customer's list of projects - it'll always be restricted to the company that owns the underlying survey(s). "samsung-eu-campaigns" is a project.
* Product: We can apply this concept to surveys that Savanta owns, then open them up to many companies via the auth server. All the BrandVues are products.

![image](https://user-images.githubusercontent.com/2490482/121003213-2fb68280-c785-11eb-9b9b-de82f75b9585.png)

### Key areas to improve

* Currently an entity type can't be formed from multiple marginally different choice sets (e.g. Brand and BrandPlusOther). This exists in the database, but has no ui. Similarly, no entity type hierarchy exists, so entity sets have to be redefined on each.
  * Could: Show some suggestions that are similar (in terms of which survey choice ids are used), or even limit them to require some amount of overlap to avoid issues
* Currently, the entity combination doesn't distinguish the input types from output entity type. The snowflake model rectifies this, but will need to be brought into the UI in appropriate places. Almost everywhere currently trying to use question type is essentially hacking around this.
* The AllVue/survey UI doesn't currently support copying urls. It will start using UrlSync and redux to cover this need.
  * Redux stores the user's intent. Sometimes a priority ordered list based on what they selected recently, so that we can restore the option that is available in their current context.
  * The url is mapped from that store, but only shows details in which the current page display differs from the default, so for a a report it would just have the report id. Then if someone changes the timeframe of a report (but doesn't save it), it'd have that in the URL.

## Scripting for Vue

Example survey for Vue:
https://savanta.all-vue.com/retail/ui/image-and-association/image/over-time
On the page above, you can "split by" image or brand, both are driven by choice sets in the mqml

There are key bits that need to stay the same to make it work over time:
1. Numeric survey choice id
2. Question var code
3. Question type (controls which slots its choice sets are stored in the db)

 
#### Ramifications

If you change varcode, it'll just be a new variable. You could then use a custom variable to "OR" the two together to get a single graph out of them again (with any remapping needed).
If you reuse a choice id, we will assume it was a rename with the same meaning, e.g. a typo fix, or subtle enough change to keep a continuous line (and we'll probably just show the new name). The desktop tools block/discourage people from doing the wrong thing here.
Changing the question type, if it changes the data layout, will split the variable into two, one with the surveyid appended.

