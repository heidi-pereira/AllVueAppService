using BrandVue.Models;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Llm;

[JsonSchemaFlatten]
public class RankingRequestData :
    InsightRequestData<RankingTableResults, MultiEntityRequestModel, RankingTableResults>
{
    public RankingRequestData(RankingTableResults results, MultiEntityRequestModel request, IReadOnlyCollection<RankingAverageItem> averageData)
    {
        Results = results;
        Request = request;
        AverageData = averageData;
    }

    public override RankingTableResults Results { get;}
    public override MultiEntityRequestModel Request { get; }
    public override IReadOnlyCollection<RankingAverageItem> AverageData { get; }
        
    public override string FormatDataPrompt(ResultsProviderParameters requestParameters)
    {
        var metric = requestParameters.PrimaryMeasure;
        var unit = metric.NumberFormat.Contains("%")
            ? "Percentage"
            : "Value" + (metric.Minimum != null && metric.Maximum != null
                ? $" from {metric.Minimum} to {metric.Maximum}"
                : "");
        var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
        string[] headings = [
            "Brand/Average, Margin of Error",
            $"{Results.Results.First().PreviousWeightedDailyResult.Date:yyyy-MM}",
            $"{Results.Results.First().CurrentWeightedDailyResult.Date:yyyy-MM}", 
            "Old Rank", 
            "New Rank"
        ];
        var result = $"Question: {metric.Description} \n Unit: {unit} \n```csv \n";
        result += string.Join(",", headings) + "\n";
        result += string.Join("\n", Results.Results.Select(rt => 
            InstanceCsvTitleAndMoe([rt.PreviousWeightedDailyResult, rt.CurrentWeightedDailyResult], entityInstances[rt.EntityInstance.Id].Name) +
            InstanceCsvResults([rt.PreviousWeightedDailyResult, rt.CurrentWeightedDailyResult]) + ", " + rt.PreviousRank + ", " + rt.CurrentRank)
        );
        result += "\n```";
        return result;
    }

    public override ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter)
    {
        return requestAdapter.CreateParametersForCalculation(Request);
    }
        
    public class RankingAverageItem : IAverageDataItem<RankingTableResults, MultiEntityRequestModel>
    {
        public RankingAverageItem(RankingTableResults results, MultiEntityRequestModel request, string name)
        {
            Results = results;
            Request = request;
            Name = name;
        }

        public RankingTableResults Results { get; }
        public MultiEntityRequestModel Request { get; }
        public string Name { get; }
    }
}