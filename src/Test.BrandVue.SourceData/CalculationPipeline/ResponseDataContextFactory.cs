using BrandVue.EntityFramework;
using BrandVue.EntityFramework.ResponseRepository;
using Microsoft.EntityFrameworkCore;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Factory for creating ResponseDataContext instances for testing
    /// </summary>
    public class ResponseDataContextFactory : IDbContextFactory<ResponseDataContext>
    {
        private readonly DbContextOptions<ResponseDataContext> _options;

        public ResponseDataContextFactory(DbContextOptions<ResponseDataContext> options)
        {
            _options = options;
        }

        public ResponseDataContext CreateDbContext()
        {
            return new ResponseDataContext(_options);
        }
    }
}
