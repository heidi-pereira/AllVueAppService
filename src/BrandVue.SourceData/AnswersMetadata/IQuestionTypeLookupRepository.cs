using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.SourceData.AnswersMetadata
{
    public interface IQuestionTypeLookupRepository
    {
        IDictionary<string, MainQuestionType> GetForSubset(Subset subset);
    }
}
