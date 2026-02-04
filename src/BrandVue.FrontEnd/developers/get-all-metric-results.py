import pathlib
import os
from MetricResultsWriter import MetricResultsWriter
from ApiRequester import ApiRequester

try:
    requestHeaders = { "Authorization": "Bearer <Your API Key>" }
    apiRequester = ApiRequester("<Your domain and product name>", requestHeaders)
    subFolder = apiRequester.baseUrl.split("/")[-1]
    outputDirectory = os.path.join(pathlib.Path.home(), "Desktop", subFolder)
    os.makedirs(outputDirectory, exist_ok=True)
    outputFileFullPath = os.path.join(outputDirectory, "metric-results.csv")
    resultsWriter = MetricResultsWriter(apiRequester, outputFileFullPath)
    resultsWriter.write_results_to_csv()
    print("Finished!")
except KeyboardInterrupt:
    print("Quitting gracefully")
    exit()