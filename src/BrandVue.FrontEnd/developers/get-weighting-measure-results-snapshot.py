import pathlib
import os
import json
from MetricResultsWriter import MetricResultsWriter
from ApiRequester import ApiRequester

try:
    baseUrl = "<Your domain and product name>"
    cookieString = "<Your cookie string>"
    requestHeaders = { 'cookie': cookieString }
    apiRequester = ApiRequester(baseUrl, requestHeaders)
    subFolder = apiRequester.baseUrl.split("/")[-1]
    outputDirectory = os.path.join(pathlib.Path.home(), "Desktop", subFolder)
    os.makedirs(outputDirectory, exist_ok=True)
    print("Fetching weighting strategies...")
    weightingStrategy = apiRequester.get_api_response("api/meta/weightingStrategy", is_survey_response_api=False)
    with open(os.path.join(outputDirectory, "weighting-strategies.json"), "w") as wsoutfile:
        json.dump(weightingStrategy, wsoutfile)

    outputFileFullPath = os.path.join(outputDirectory, "metrics-snapshot.csv")
    resultsWriter = MetricResultsWriter(apiRequester, outputFileFullPath)
    resultsWriter.write_results_to_csv()
    print("Finished!")
except KeyboardInterrupt:
    print("Quitting gracefully")
    exit()