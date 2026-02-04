using BrandVue.Models;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Llm
{
    [JsonSchemaFlatten]
    public class CompetitionRequestData :
        InsightRequestData<CompetitionResults, MultiEntityRequestModel, OverTimeAverageResults>
    {
        public CompetitionRequestData(CompetitionResults results, MultiEntityRequestModel request, IReadOnlyCollection<OverTimeRequestData.OverTimeAverageItem> averageData)
        {
            Results = results;
            Request = request;
            AverageData = averageData;
        }

        public override CompetitionResults Results { get;  }
        public override MultiEntityRequestModel Request { get; }
        public override IReadOnlyCollection<OverTimeRequestData.OverTimeAverageItem> AverageData { get; }
        public override string FormatDataPrompt(ResultsProviderParameters requestParameters)
        {
            var metric = requestParameters.PrimaryMeasure;
            var unit = metric.NumberFormat.Contains("%")
                ? "Percentage"
                : "Value" + (metric.Minimum != null && metric.Maximum != null
                    ? $" from {metric.Minimum} to {metric.Maximum}"
                    : "");
            var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
            var entityType = requestParameters.PrimaryMeasure.EntityCombination.First();
            string[] headers =
            [
                "Brand/Average", 
                "Margin of Error",
                ..Results.PeriodResults.Select(wdr => wdr.Period.StartDate.ToString("yyyy-MM"))
            ];
            var csvString = $"Question: {metric.Description} \n Unit: {unit} \n ```csv \n {string.Join(",", headers)} \n";
            csvString += string.Join("\n", AverageData.Select(average => 
                InstanceCsvTitleAndMoe(average.Results.WeightedDailyResults, $"{average.Name} {entityType.DisplayNamePlural} Average") +
                InstanceCsvResults(average.Results.WeightedDailyResults)));
            csvString += string.Join("\n", entityInstances.Select(x =>
                {
                    var results = Results.PeriodResults.Select(y => 
                        y.ResultsPerEntity
                            .First(z => z.EntityInstance.Id == x.Key).WeightedDailyResults.First()
                    ).ToList();
                    return InstanceCsvTitleAndMoe(results, x.Value.Name) +
                           InstanceCsvResults(results);
                })
            );
            csvString += "\n```";
            return csvString;
        }

        public override ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter)
        {
            return requestAdapter.CreateParametersForCalculation(Request);
        }
    }
}