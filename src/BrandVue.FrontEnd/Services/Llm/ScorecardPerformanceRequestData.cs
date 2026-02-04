using BrandVue.Models;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Llm
{
    [JsonSchemaFlatten]
    public class ScorecardPerformanceRequestData :
        InsightRequestData<ScorecardPerformanceResults, CuratedResultsModel, ScorecardPerformanceCompetitorResults>
    {
        public ScorecardPerformanceRequestData(ScorecardPerformanceResults results, CuratedResultsModel request, IReadOnlyCollection<ScorecardPerformanceAverageItem> averageData)
        {
            Results = results;
            Request = request;
            AverageData = averageData;
        }

        public override ScorecardPerformanceResults Results { get;  }
        public override CuratedResultsModel Request { get; }
        public override IReadOnlyCollection<ScorecardPerformanceAverageItem> AverageData { get; }
        public override string FormatDataPrompt(ResultsProviderParameters requestParameters)
        {
            var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
            var entityType = requestParameters.PrimaryMeasure.EntityCombination.First();
            var currentDate = Results.MetricResults.First().PeriodResults.Max(y => y.Date);
            var currentDateString = currentDate.ToString("yyyy-MM"); 
            string[] headings = [
                "Metric Name",
                ..Results.MetricResults.First().PeriodResults.Select(pr => pr.Date.ToString("yyyy-MM")),
                currentDateString + "Competitor Min",
                currentDateString + "Competitor Max",
                ..AverageData.Select(x=> $"{x.Name} {entityType.DisplayNamePlural} Average"),
            ];
            var result = $"Main {entityType.DisplayNameSingular}: {entityInstances[Request.ActiveBrandId].Name} \n" + 
                "```csv \n" +
                string.Join(",", headings) + "\n";
            result += string.Join("\n", Results.MetricResults.Select(mr =>
            {
                var averageValues = AverageData.SelectMany(ad =>
                    ad.Results.MetricResults.First(x => x.MetricName == mr.MetricName).CompetitorData
                        .Select(x => x.Result.WeightedResult)
                ).ToArray();
                string[] entries =
                [
                    requestParameters.Measures.First(x => x.Name == mr.MetricName).Name, // Metric Name
                    ..mr.PeriodResults.Select(pr => pr.WeightedResult.ToString("G3")),
                    averageValues.Min().ToString("G3"),
                    averageValues.Max().ToString("G3"),
                    ..AverageData.Select(ad => ad.Results.MetricResults.First(x => x.MetricName == mr.MetricName).CompetitorAverage.ToString("G3"))
                ];
                return string.Join(",", entries);
            }));
            result += "\n```";
            return result;
        }

        public override ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter)
        {
            return requestAdapter.CreateParametersForCalculation(Request);
        }
        
        public class ScorecardPerformanceAverageItem : IAverageDataItem<ScorecardPerformanceCompetitorResults, CuratedResultsModel>
        {
            public ScorecardPerformanceCompetitorResults Results { get; init; }
            public CuratedResultsModel Request { get; init; }
            public string Name { get; init; }
        }
    }
}