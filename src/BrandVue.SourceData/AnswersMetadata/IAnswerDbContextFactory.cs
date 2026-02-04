using BrandVue.EntityFramework.Answers;

namespace BrandVue.SourceData.AnswersMetadata
{
    public interface IAnswerDbContextFactory
    {
        AnswersDbContext CreateDbContext(int? commandTimeout = null);
    }
}