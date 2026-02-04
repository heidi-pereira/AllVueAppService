using System.Security.Cryptography;
using System.Text;
using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using Newtonsoft.Json;

namespace BrandVue.Services.Llm;

// Generic interface that constrains the types
public interface IInsightRequestData<out TResults,out TRequest,out TAverageResults>
    where TResults : AbstractCommonResultsInformation
    where TRequest : IEntityRequestModel
    where TAverageResults : AbstractCommonResultsInformation
{
    TResults Results { get; }
    TRequest Request { get; }
    IReadOnlyCollection<IAverageDataItem<TAverageResults, TRequest>> AverageData { get; }
    string FormatDataPrompt(ResultsProviderParameters requestParameters);
    ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter);
    string ChartSpecificSystemPromptInfo();
    string ToHash();
}

public interface IAverageDataItem<out TAverageResults,out TRequest> where TAverageResults : AbstractCommonResultsInformation where TRequest : IEntityRequestModel
{
    TAverageResults Results { get; }
    TRequest Request { get; }
    public string Name { get; }
}

public abstract class InsightRequestData<TResults, TRequest, TAverageResults> : IInsightRequestData<TResults, TRequest, TAverageResults>
    where TResults : AbstractCommonResultsInformation
    where TRequest : IEntityRequestModel
    where TAverageResults : AbstractCommonResultsInformation
{
    public abstract TResults Results { get;  }
    public abstract TRequest Request { get; }
    public abstract IReadOnlyCollection<IAverageDataItem<TAverageResults, TRequest>> AverageData { get; }
    public abstract string FormatDataPrompt(ResultsProviderParameters requestParameters);
    public abstract ResultsProviderParameters GetResultsProviderParameters(IRequestAdapter requestAdapter);

    public virtual string ChartSpecificSystemPromptInfo()
    {
        return "";
    }

    public virtual string ToHash()
    {
        var averageRequests = AverageData.Select(x => x.Request).ToArray();
        string input = JsonConvert.SerializeObject(new {request = Request, averageRequests = averageRequests.OrderBy(x=>x.GetEntityInstanceIds().Sum())});
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(input)));
    }

    protected string InstanceCsvTitleAndMoe(IList<WeightedDailyResult> results, string name)
    {
        var moe = RoundToSignificantDigits(results
            .Select(x => StandardError(x.WeightedResult, x.WeightedSampleSize)).Min(), 3);
        return name + $",{(double.IsNaN(moe) ? "" : $"±{moe:0.###}")}, ";
    }
    
    protected string InstanceCsvResults(IList<WeightedDailyResult> results)
    {
        return string.Join(",", results
            .Select(wdr => RoundToSignificantDigits(wdr.WeightedResult, 3).ToString("0.###")));
    }
    /// <summary>
    /// This is *a* standard error function, ok for the AI for now, but for time series a proper standard deviation function should be used
    /// </summary>
    /// <param name="value"></param>
    /// <param name="sample"></param>
    /// <returns>95% proportional standard error</returns>
    protected double StandardError(double value, double sample)
    {
        return Math.Sqrt(value * (1 - value) / sample) * 2.0;
    }

    protected double RoundToSignificantDigits(double d, int digits){
        if(d == 0)
            return 0;

        var scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
        return scale * Math.Round(d / scale, digits);
    }
}