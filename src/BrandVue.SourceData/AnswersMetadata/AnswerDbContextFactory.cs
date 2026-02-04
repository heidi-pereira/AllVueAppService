using BrandVue.EntityFramework.Answers;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.AnswersMetadata
{
    public sealed class AnswerDbContextFactory : IAnswerDbContextFactory
    {
        private readonly string _connectionString;

        public AnswerDbContextFactory(string connectionString) => _connectionString = connectionString;

        public AnswersDbContext CreateDbContext(int? commandTimeout = null)
        {
            var builder = new DbContextOptionsBuilder()
                .UseSqlServer(_connectionString, options =>
                {
                    options.MigrationsHistoryTable("__MigrationsHistoryForVue");
                    options.CommandTimeout(commandTimeout);
                });

            var dbContext = new AnswersDbContext(builder.Options);

            // Disable auto detection on change tracking as this is terribly inefficient for our use-case
            // We need to ensure that we always call DetectChanges before saving on the context now.
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            return dbContext;
        }
    }
}