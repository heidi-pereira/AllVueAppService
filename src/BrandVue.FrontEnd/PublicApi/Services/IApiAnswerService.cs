using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using System.Threading;

namespace BrandVue.PublicApi.Services
{
    public interface IApiAnswerService
    {
        IEnumerable<string> CreateHeaders(IEnumerable<string> entityCombinationSpecificColumns, IEnumerable<ResponseFieldDescriptor> questionDescriptors);
        Task<ResponseDataWithHeaders> GetVariableResponseData(SurveysetDescriptor surveyset,
            VariableDescriptor variableConfiguration, DateTimeOffset date, CancellationToken cancellationToken);
        Task<ResponseDataWithHeaders> GetMappedClassResponseData(SurveysetDescriptor surveyset,
            ClassDescriptor classDescriptor, DateTimeOffset date, bool includeText, CancellationToken cancellationToken);
        Task<ResponseDataWithHeaders> GetNestedClassResponseData(SurveysetDescriptor surveyset,
            ClassDescriptor parentClass, ClassDescriptor childClass, DateTimeOffset date, bool includeText,
            CancellationToken cancellationToken);
        Task<ResponseDataWithHeaders> GetProfileResponseData(SurveysetDescriptor surveyset,
            DateTimeOffset date, string weightingColumnName, CancellationToken cancellationToken);
    }

    public record ResponseDataWithHeaders(IEnumerable<Dictionary<string, string>> ResponseData, IEnumerable<string> Headers);
}