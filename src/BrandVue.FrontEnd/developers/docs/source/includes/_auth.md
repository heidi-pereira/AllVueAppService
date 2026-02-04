# Authentication and authorization

You will need an **API license** and an **API key** before you can make any requests to the API.

## API keys
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

#### Getting an API key
Your Savanta account manager will be able to provide you with an API Key. You are responsible for any API access using your key and any data retrieved using your key is subject to our contractual licence with you. Please contact us immediately if there's a possibility the key has been accessed by an unauthorized party.

#### Using your API key

Your API key will need to be included in the Authorization header for every request. 

Your API key is tied to your API license which determines the endpoints you can access and what data is available to you.

#### Revoking/reissuing an API key
Please contact your account manager or Savanta support if you need to revoke an API key. This can be done at any time and a new key provided to you. We will revoke any keys we suspect have been compromised - for example those that significantly exceed allowed API usage thresholds.

## API licenses
Your API key is tied to your API license which determines the endpoints you can access and what data is available to you.

You may have either or both of the APIs (Survey response & Calculated metrics) available to you, depending on your API license. Access to specific API endpoints is restricted according to your license. Some API endpoints are available with any license, these typically are used to retrieve metadata necessary to interpret survey responses or describe calculated metrics. 

Endpoints that are only available with the Calculated metrics API license will be marked like this in the documentation. 

<img src="source/images/metric.svg" alt="" /> Calculated metrics API license required.
{ .metric-api}

Endpoints that are only available with the Survey response API license will be marked like this in the documentation. 

<img src="source/images/response.svg" alt="" /> Survey response API license required.
{ .response-api}