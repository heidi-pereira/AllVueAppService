import itertools
import csv
from typing import Iterator, Dict, List, Tuple
from requests.exceptions import HTTPError
from ApiRequester import ApiRequester

class MetricResultsWriter:
    def __init__(self, apiRequester: ApiRequester, outputFileFullPath: str):
        self.apiRequester = apiRequester
        self.outputFileFullPath = outputFileFullPath

    def __get_class_to_instances_lookup(self, subsetId: str) -> Dict[str, Dict[str, str]]:
        print("Fetching classes and instances...")
        apiClasses = self.apiRequester.get_api_response(f"api/surveysets/{subsetId}/classes")
        classToInstances = {}
        for apiClass in apiClasses:
            apiClassId = apiClass["classId"]
            lowerCaseApiClassName = apiClass["name"].lower()
            apiClassInstances = self.apiRequester.get_api_response(f"api/surveysets/{subsetId}/classes/{apiClassId}/instances")
            classToInstances[lowerCaseApiClassName] = {str(apiClassInstance["classInstanceId"]):apiClassInstance["name"] for apiClassInstance in apiClassInstances}
        return classToInstances

    def __get_start_and_end_date(self, subsetId: str) -> Tuple[str, str]:
        subsetDates = self.apiRequester.get_api_response(f"api/surveysets/{subsetId}")
        startDate = subsetDates["earliestResponseDate"]
        endDate = subsetDates["latestResponseDate"]
        return (startDate, endDate)

    def __get_averages(self, subsetId: str) -> List[str]:
        apiAverages = self.apiRequester.get_api_response(f"api/surveysets/{subsetId}/averages")
        return (apiAverage["averageId"] for apiAverage in apiAverages)

    def __lower_first(self, iterator: Iterator) -> itertools.chain:
        return itertools.chain([next(iterator).lower()], iterator)

    def __extract_instance_at_position(self, row: Dict[str, str], metricClasses: List[str], classesToInstances: Dict[str, Dict[str, str]], position: int) -> str:
        if len(metricClasses) <= position:
            return ""
        entityClass = metricClasses[position].lower()
        return classesToInstances[entityClass][row[f"{entityClass}id"]]

    def write_results_to_csv(self):
        subsets = self.apiRequester.get_api_response("api/surveysets")
        csvHeaders = ["Subset", "Average", "MetricId", "EndDate", "Value", "SampleSize", "EntityInstance1", "EntityInstance2", "EntityInstance3"]
        with open(self.outputFileFullPath, "w", newline="") as resultsCsv:
            writer = csv.writer(resultsCsv)
            writer.writerow(csvHeaders)
            for subset in subsets:
                subsetId = subset["surveysetId"]
                subsetName = subset["name"]
                averages = self.__get_averages(subsetId)
                (startDate, endDate) = self.__get_start_and_end_date(subsetId)
                classesToInstances = self.__get_class_to_instances_lookup(subsetId)
                metricsForSubset = self.apiRequester.get_api_response(f"api/surveysets/{subsetId}/metrics")
                for average in averages:
                    for metric in metricsForSubset:
                        metricId = metric["metricId"]
                        metricName = metric["name"]
                        print(f"Working on {subsetName}, {average}, {metricName}")
                        metricClasses = metric["questionClasses"]
                        classInstances = {metricClass: list(classesToInstances[metricClass.lower()].keys()) for metricClass in metricClasses}
                        requestBody = {
                            "startDate": startDate,
                            "endDate": endDate,
                            "classInstances": classInstances
                        }
                        try:
                            results = self.apiRequester.post_api_response(f"api/surveysets/{subsetId}/metrics/{metricId}/{average}", requestBody, is_json=False)
                            resultsRows = csv.DictReader(self.__lower_first(results.iter_lines(decode_unicode='utf-8')))
                            for resultRow in resultsRows:
                                entityInstance1 = self.__extract_instance_at_position(resultRow, metricClasses, classesToInstances, 0)
                                entityInstance2 = self.__extract_instance_at_position(resultRow, metricClasses, classesToInstances, 1)
                                entityInstance3 = self.__extract_instance_at_position(resultRow, metricClasses, classesToInstances, 2)
                                row = [subsetName, average, metricId, resultRow["enddate"], resultRow["value"], resultRow["samplesize"], entityInstance1, entityInstance2, entityInstance3]
                                writer.writerow(row)
                        except HTTPError as err:
                            if err.response.status_code == 400:
                                message = err.response.json()["message"]
                                print(f"Metric {metricId} failed. {message}")

