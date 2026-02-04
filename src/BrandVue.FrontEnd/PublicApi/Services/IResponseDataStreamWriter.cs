using System.Runtime.CompilerServices;

namespace BrandVue.PublicApi.Services
{
    public interface IResponseDataStreamWriter
    {

        StreamedCsvResult StreamSurveyAnswersetsToHttpResponseMessage<TTuple>(IEnumerable<string> headers,
            IEnumerable<TTuple> rowsWithSameColumnOrder, Func<IEnumerable<(string, string)>> extraLines) where TTuple : ITuple;
        
       StreamedCsvResult StreamSurveyAnswersetsToHttpResponseMessage(IEnumerable<string> headers, IEnumerable<IReadOnlyDictionary<string, string>> surveyResponseData);
    }
}