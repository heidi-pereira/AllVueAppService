# Introduction

#### Base URL

<!-- On the live site, this is replaced using the replace_variables.js file -->
<code>https://{DomainAndOrg}/{ProductName}</code>

#### What are the Savanta Data APIs?

> Scroll down for [code samples](#endpoint-definitions), and [worked examples](#worked-examples).

There are two different APIs available:
- Calculated metrics API
- Survey response API

These APIs are designed to be accessed from a program/service, so, rather than user login details, all requests must be sent with an [API Key](#authentication-and-authorization).

This [Savanta Data Open API 3 spec](../BrandVueApi.OpenApi3.json) may be useful in generating code to call these APIs, or integrating with other tools.

#### Calculated metrics API
Provides the calculated metric data that is used in Savanta dashboards. This data comes from survey responses that are aggregated and weighted.  

This API is useful if you want to pull metrics from Savanta dashboards into your own reporting systems. For example, you may want to measure the effectiveness of offline marketing efforts. The metrics from this API can be used to measure whether your campaigns have an impact.  

#### Survey response API
Provides all of the data, by individual respondents, from a survey and metadata to interpret the responses. It does not return results for calculated metrics seen in Savanta dashboards.  

This API is useful if you want to do custom results aggregation; combining raw data from Savanta surveys with your own data sources to produce enriched analytics. It can also be used to present survey data in BI solutions. Data scientists can use this API to interrogate and investigate survey responses. 