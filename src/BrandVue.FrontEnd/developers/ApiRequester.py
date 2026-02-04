import requests
from typing import Dict
from urllib import parse

class ApiRequester:
    def __init__(self, baseUrl: str, requestHeaders: Dict[str, str]):
        self.baseUrl = baseUrl.rstrip("/")
        self.requestHeaders = requestHeaders

    def __process_response(self, response, is_json: bool, is_survey_response_api: bool):
        if is_json and is_survey_response_api:
            return response.json()["value"]
        if is_json:
            return response.json()
        return response

    def get_api_response(self, path: str, is_json=True, is_survey_response_api=True):
        trimmedPath = path.lstrip("/")
        encodedPath = parse.quote(trimmedPath)
        response = requests.get(f"{self.baseUrl}/{encodedPath}", headers=self.requestHeaders)
        response.raise_for_status() #Errors for 400-599
        return self.__process_response(response, is_json, is_survey_response_api)

    def post_api_response(self, path: str, body: Dict[str, str], is_json=True, is_survey_response_api=True):
        trimmedPath = path.lstrip("/")
        encodedPath = parse.quote(trimmedPath)
        response = requests.post(f"{self.baseUrl}/{encodedPath}", headers=self.requestHeaders, json=body)
        response.raise_for_status() #Errors for 400-599
        return self.__process_response(response, is_json, is_survey_response_api)