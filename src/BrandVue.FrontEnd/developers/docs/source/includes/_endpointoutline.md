# Endpoint outline
The contents of this section apply to all endpoints unless otherwise specified.

* All parameters are **case sensitive**.
* The order of response questions, and the contents of any arrays may change without warning.

## Authentication and authorization
You will need an active **API subscription** and an **API key** before you can make any requests to the API.

##### Getting an API key
Your Savanta account manager will be able to provide you with an API Key. You are responsible for any API access using your key and any data retrieved using your key is subject to our contractual licence with you. Please contact us immediately if there's a possibility the key has been accessed by an unauthorized party.

##### Using your API key

> Request - get available surveysets
```curl
GET /api​/surveysets​ -H 'Authorization: Bearer {access-token}'
```

> Example response
```json
{
    "value": [
        {
            "surveysetId": "UK",
            "name": "UK"
        }
    ]
}
```

Your API key will need to be included in the Authorization header for every request. 

The API key determines what data can be accessed.

##### Revoking/reissuing an API key
Please contact your account manager or Savanta support if you need to revoke an API key. This can be done at any time and a new key provided to you. We will revoke any keys we suspect have been compromised - for example those that significantly exceed allowed API usage thresholds.

#### Responses
Most endpoints return a json response, with the result nested within a `value` property.

The survey answers endpoints return a csv in order to reduce file size, and make it simpler to add directly into a database, see:
* [/api/surveysets/{surveyset}/profile/answers/{date}](#get-profile-answers)
* [/api/surveysets/{surveyset}/classes/{class}/answers/{date}](#get-class-answers)
