using BrandVue.Models;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Llm
{
    [JsonSchemaFlatten]
    public class FunnelRequestData :
        InsightRequestData<FunnelResults, CuratedResultsModel, FunnelResults>
    {
        public FunnelRequestData(FunnelResults results, CuratedResultsModel request, IReadOnlyCollection<FunnelAverageItem> averageData)
        {
            Results = results;
            Request = request;
            AverageData = averageData;
        }

        public override FunnelResults Results { get;  }
        public override CuratedResultsModel Request { get; }
        public override IReadOnlyCollection<FunnelAverageItem> AverageData { get; }
        public override string FormatDataPrompt(ResultsProviderParameters requestParameters)
        {
            var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
            var entityType = requestParameters.PrimaryMeasure.EntityCombination.First();
            var csvString = $"Adoption conversion funnel. \n ```csv \n" +
                         "Brand/Average, " +
                         GetMetricHeadings() + "\n";
            csvString += string.Join("\n", AverageData.Select(average =>
                $"{average.Name} {entityType.DisplayNamePlural} Average, " +
                FunnelCsvResults(average.Results.MarketAveragePerMeasures))) + "\n";
            csvString += string.Join("\n", Results.Results.Select(r => 
                $"{entityInstances[r.EntityInstance.Id].Name}, " +
                FunnelCsvResults(r.MetricResults)));
            csvString += "\n```";
            return csvString;
        }
        public override string ChartSpecificSystemPromptInfo()
        {
            return "Focus on the number conversions between each stage of the funnel. ";
        }
        private string FunnelCsvResults(IReadOnlyList<MetricWeightedDailyResult> results)
        {
            var csv = "";
            for(int i = 0; i < results.Count; i++)
            {
                csv += results[i].WeightedDailyResult.WeightedResult.ToString("G3");
                if(i < Results.MarketAveragePerMeasures.Length - 1)
                {
                    var m1 = (results[i + 1].WeightedDailyResult.WeightedResult / 
                              results[i].WeightedDailyResult.WeightedResult).ToString("G3");
                    csv += $", {m1}, ";
                }
            }
            return csv;
        }

        private string GetMetricHeadings()
        {
            var csv = "";
            for(int i = 0; i < Results.MarketAveragePerMeasures.Length; i++)
            {
                csv += Results.MarketAveragePerMeasures[i].MetricName;
                if(i < Results.MarketAveragePerMeasures.Length - 1)
                {
                    csv += $", Conversion % between {Results.MarketAveragePerMeasures[i].MetricName} and {Results.MarketAveragePerMeasures[i+1].MetricName}, ";
                }
            }
            return csv;
        }

        public override ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter)
        {
            return requestAdapter.CreateParametersForCalculation(Request);
        }
        public class FunnelAverageItem : IAverageDataItem<FunnelResults, CuratedResultsModel>
        {
            public FunnelAverageItem(FunnelResults results, CuratedResultsModel request, string name)
            {
                Results = results;
                Request = request;
                Name = name;
            }

            public FunnelResults Results { get; }
            public CuratedResultsModel Request { get; }
            public string Name { get; }
        }
    }
}