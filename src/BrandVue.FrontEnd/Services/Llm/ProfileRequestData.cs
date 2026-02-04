using BrandVue.Models;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Llm
{
    [JsonSchemaFlatten]
    public class ProfileRequestData :
        InsightRequestData<BreakdownResults, MultiEntityRequestModel, BreakdownResults>
    {
        public ProfileRequestData(BreakdownResults results, MultiEntityRequestModel request, IReadOnlyCollection<BreakdownAverageItem> averageData)
        {
            Results = results;
            Request = request;
            AverageData = averageData;
        }

        public override BreakdownResults Results { get;}
        public override MultiEntityRequestModel Request { get; }
        public override IReadOnlyCollection<BreakdownAverageItem> AverageData { get; }
        
        public override string FormatDataPrompt(ResultsProviderParameters requestParameters)
        {
            var metric = requestParameters.PrimaryMeasure;
            var unit = metric.NumberFormat.Contains("%")
                ? "Percentage"
                : "Value" + (metric.Minimum != null && metric.Maximum != null
                    ? $" from {metric.Minimum} to {metric.Maximum}"
                    : "");
            var result = $"Question: {metric.Description} \n Unit: {unit} \n```csv \n";
            var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
            foreach (var instanceResults in Results.Data)
            {
                result += $"Brand: {entityInstances[instanceResults.EntityInstance.Id]} \n";
                result = AppendTableFromBreakdownResults(instanceResults, result);
            }

            foreach (var averageResultItem in AverageData)
            {
                foreach (var instanceResults in averageResultItem.Results.Data)
                {
                    result += $"Average: {averageResultItem.Name} \n";
                    result = AppendTableFromBreakdownResults(instanceResults, result);
                }
            }
            result += "\n```";
            return result;
        }

        private static string AppendTableFromBreakdownResults(BrokenDownResults instanceResults, string result)
        {
            List<string> headings = ["Period"];
            headings.AddRange(instanceResults.ByAgeGroup.Select(x=>"Age" + x.Category));
            headings.AddRange(instanceResults.ByGender.Select(x=> x.Category));
            headings.AddRange(instanceResults.ByRegion.Select(x=> "Region " + x.Category));
            headings.AddRange(instanceResults.BySocioEconomicGroup.Select(x=> "Social Grade " + x.Category));
            result += string.Join(",", headings) + "\n";
            foreach (var period in instanceResults.ByAgeGroup.First().WeightedDailyResults.Select(x=>x.Date))
            {
                List<string> periodResults = [period.ToString()];
                periodResults.AddRange(instanceResults.ByAgeGroup.Select(group => 
                    group.WeightedDailyResults
                        .First(x => x.Date == period)
                        .WeightedResult.ToString("G3")
                ));
                
                periodResults.AddRange(instanceResults.ByGender.Select(group => 
                    group.WeightedDailyResults
                        .First(x => x.Date == period)
                        .WeightedResult.ToString("G3")
                ));
                
                periodResults.AddRange(instanceResults.ByRegion.Select(group => 
                    group.WeightedDailyResults
                        .First(x => x.Date == period)
                        .WeightedResult.ToString("G3")
                ));
                
                periodResults.AddRange(instanceResults.BySocioEconomicGroup.Select(group => 
                    group.WeightedDailyResults
                        .First(x => x.Date == period)
                        .WeightedResult.ToString("G3")
                ));
                result += string.Join(",", periodResults) + "\n";
            }

            return result;
        }

        public override ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter)
        {
            return requestAdapter.CreateParametersForCalculation(Request);
        }
        
        public class BreakdownAverageItem : IAverageDataItem<BreakdownResults, MultiEntityRequestModel>
        {
            public BreakdownAverageItem(BreakdownResults results, MultiEntityRequestModel request, string name)
            {
                Results = results;
                Request = request;
                Name = name;
            }

            public BreakdownResults Results { get; }
            public MultiEntityRequestModel Request { get; }
            public string Name { get; }
        }
    }
}