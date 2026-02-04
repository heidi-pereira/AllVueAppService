using BrandVue.SourceData.AnswersMetadata;

namespace BrandVue.SourceData.Measures;

internal interface ILoadableQuestionTypeLookupRepository : IQuestionTypeLookupRepository
{
    void AddOrUpdate(Measure measure);
    void Remove(Measure measureName);
}