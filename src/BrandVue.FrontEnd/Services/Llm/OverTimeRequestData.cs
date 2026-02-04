using BrandVue.Models;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Llm
{
    [JsonSchemaFlatten]
    public class OverTimeRequestData :
        InsightRequestData<OverTimeResults, MultiEntityRequestModel, OverTimeAverageResults>
    {
        public OverTimeRequestData(OverTimeResults results, MultiEntityRequestModel request, IReadOnlyCollection<OverTimeAverageItem> averageData)
        {
            Results = results;
            Request = request;
            AverageData = averageData;
        }

        public override OverTimeResults Results { get; }
        public override MultiEntityRequestModel Request { get; }
        public override IReadOnlyCollection<OverTimeAverageItem> AverageData { get; }

        public override string FormatDataPrompt(ResultsProviderParameters requestParameters)
        {
            var metric = requestParameters.PrimaryMeasure;
            var unit = metric.NumberFormat.Contains("%")
                ? "Percentage"
                : "Value" + (metric.Minimum != null && metric.Maximum != null
                    ? $" from {metric.Minimum} to {metric.Maximum}"
                    : "");
            var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
            var entityType = requestParameters.PrimaryMeasure.EntityCombination.FirstOrDefault();
            var result = $"Question: {metric.Description} \n Unit: {unit} \n ```csv \n" +
            "Brand/Average, Margin of Error," +
                         string.Join(",", Results.EntityWeightedDailyResults.First().WeightedDailyResults.Select(wdr => wdr.Date.ToString("yyyy-MM"))) + "\n";
            if (entityType != null) {
                result += string.Join("\n", AverageData.Select(average => 
                    InstanceCsvTitleAndMoe(average.Results.WeightedDailyResults, $"{average.Name} {entityType.DisplayNamePlural} Average") +
                    InstanceCsvResults(average.Results.WeightedDailyResults))) + "\n";
            }
            result += string.Join("\n", Results.EntityWeightedDailyResults.Select(ewdr => 
                (entityType != null ? InstanceCsvTitleAndMoe(ewdr.WeightedDailyResults, entityInstances[ewdr.EntityInstance.Id].Name) : metric.DisplayName) +
                InstanceCsvResults(ewdr.WeightedDailyResults))
            );
            result += "\n```";
            return result;
        }

        public override ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter)
        {
            return requestAdapter.CreateParametersForCalculation(Request);
        }

        public class OverTimeAverageItem : IAverageDataItem<OverTimeAverageResults, MultiEntityRequestModel>
        {
            public OverTimeAverageItem(OverTimeAverageResults results, MultiEntityRequestModel request, string name)
            {
                Results = results;
                Request = request;
                Name = name;
            }

            public OverTimeAverageResults Results { get; }
            public MultiEntityRequestModel Request { get; }
            public string Name { get; }
        }
    }
}