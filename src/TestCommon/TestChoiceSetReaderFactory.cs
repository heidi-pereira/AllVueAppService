using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace TestCommon
{
    public class TestChoiceSetReaderFactory : IAnswerDbContextFactory
    {
        private readonly DbContextOptions<AnswersDbContext> _options = new DbContextOptionsBuilder<AnswersDbContext>()
            .UseInMemoryDatabase(nameof(TestChoiceSetReaderFactory), new InMemoryDatabaseRoot())
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)).Options;

        public AnswersDbContext CreateDbContext(int? commandTimeout = null)
        {
            return new AnswersDbContext(_options);
        }
    }
}