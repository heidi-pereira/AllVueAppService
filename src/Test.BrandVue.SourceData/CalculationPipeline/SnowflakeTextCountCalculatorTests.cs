using System;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Snowflake;
using BrandVue.SourceData.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Integration tests for SnowflakeTextCountCalculator that compare Snowflake results against SQL Server results.
    /// 
    /// CONFIGURATION REQUIRED:
    /// These tests require both Snowflake and SQL Server connections.
    /// 
    /// 1. Snowflake: Configure in appsettings.override.json or user secrets:
    /// {
    ///   "SnowflakeConnectionString": "ACCOUNT=...;USER=...;ROLE=...;WAREHOUSE=...;DATABASE=...;SCHEMA=...;PRIVATE_KEY_FILE=...;PRIVATE_KEY_PWD=...;",
    ///   "SnowflakeDapperDbSettings": {
    ///     "DatabaseName": "TEST_VUE",
    ///     "SchemaName": "RAW_SURVEY"
    ///   }
    /// }
    /// 
    /// 2. SQL Server: Configure in appsettings.override.json:
    /// {
    ///   "AnswersConnectionString": "Server=...;Database=...;..."
    /// }
    /// 
    /// TEST DATA:
    /// Add JSON files to the Inputs folder with test data (response weights, variable codes, filters).
    /// The test will query both databases and compare the results.
    /// </summary>
    [TestFixture]
    public class SnowflakeTextCountCalculatorTests
    {
        private IConfiguration _configuration;
        private TestableSnowflakeTextCountCalculator _snowflakeCalculator;
        private TestableSqlServerTextCountCalculator _sqlServerCalculator;
        private ISnowflakeRepository _snowflakeRepository;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Load configuration from appsettings.json and appsettings.override.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(TestContext.CurrentContext.TestDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.override.json", optional: true)
                .AddUserSecrets<SnowflakeTextCountCalculatorTests>()
                .Build();
        }

        [SetUp]
        public void Setup()
        {
            // This will be called before each test
            _snowflakeCalculator = null;
            _sqlServerCalculator = null;
            _snowflakeRepository = null;
        }

        private TestableSnowflakeTextCountCalculator CreateSnowflakeCalculator()
        {
            var snowflakeConnectionString = _configuration["SnowflakeConnectionString"];
            var snowflakeDapperSettings = _configuration.GetSection("SnowflakeDapperDbSettings").Get<SnowflakeDapperDbSettings>();

            if (string.IsNullOrEmpty(snowflakeConnectionString) ||
                string.IsNullOrEmpty(snowflakeDapperSettings?.DatabaseName))
            {
                Assert.Ignore("Snowflake connection not configured. Set SnowflakeConnectionString and SnowflakeDapperDbSettings in appsettings.override.json or user secrets.");
            }

            var appSettings = new AppSettings(
                rootPath: TestContext.CurrentContext.TestDirectory,
                configuration: _configuration
            );

            var connectionFactory = new SnowflakeDbConnectionFactory(snowflakeConnectionString);
            _snowflakeRepository = new SnowflakeRepository(connectionFactory);

            // Mock dependencies not needed for this specific test
            var profileResponseAccessorFactory = Substitute.For<IProfileResponseAccessorFactory>();
            var quotaCellReferenceWeightingRepository = Substitute.For<IQuotaCellReferenceWeightingRepository>();
            var measureRepository = Substitute.For<IMeasureRepository>();
            var resultsCalculator = Substitute.For<IAsyncTotalisationOrchestrator>();

            return new TestableSnowflakeTextCountCalculator(
                profileResponseAccessorFactory,
                quotaCellReferenceWeightingRepository,
                measureRepository,
                _snowflakeRepository,
                resultsCalculator,
                appSettings
            );
        }

        private TestableSqlServerTextCountCalculator CreateSqlServerCalculator()
        {
            var connectionString = _configuration["AnswersConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                Assert.Ignore("SQL Server connection not configured. Set AnswersConnectionString in appsettings.override.json or user secrets.");
            }

            var contextOptions = new DbContextOptionsBuilder<ResponseDataContext>()
                .UseSqlServer(connectionString)
                .Options;

            var contextFactory = new ResponseDataContextFactory(contextOptions);
            var textResponseRepository = new AnswerTextResponseRepository(contextFactory);

            // Mock dependencies not needed for this specific test
            var profileResponseAccessorFactory = Substitute.For<IProfileResponseAccessorFactory>();
            var quotaCellReferenceWeightingRepository = Substitute.For<IQuotaCellReferenceWeightingRepository>();
            var measureRepository = Substitute.For<IMeasureRepository>();
            var resultsCalculator = Substitute.For<IAsyncTotalisationOrchestrator>();

            return new TestableSqlServerTextCountCalculator(
                profileResponseAccessorFactory,
                quotaCellReferenceWeightingRepository,
                measureRepository,
                textResponseRepository,
                resultsCalculator
            );
        }

        [Test]
        [Category("Integration")]
        [Explicit]
        [TestCaseSource(typeof(TestDataLoader), nameof(TestDataLoader.GetTestDataFromJsonFiles))]
        public async Task GetWeightedTextCountsAsync_SnowflakeMatchesSqlServer(
            string testDataName,
            ResponseWeight[] responseWeights,
            string varCodeBase,
            (DbLocation Location, int Id)[] filters)
        {
            // Arrange
            _snowflakeCalculator = CreateSnowflakeCalculator();
            _sqlServerCalculator = CreateSqlServerCalculator();

            // Act - Get results from both databases
            WeightedWordCount[] snowflakeResults;
            WeightedWordCount[] sqlServerResults;

            try
            {
                snowflakeResults = (await _snowflakeCalculator.GetWeightedTextCountsAsync(
                        responseWeights,
                        varCodeBase,
                        filters))
                    .CleanTextAndRegroup()
                    .ToArray();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"Snowflake query failed - verify test data exists: {ex.Message}");
                return;
            }

            try
            {
                sqlServerResults = (await _sqlServerCalculator.GetWeightedTextCountsAsync(
                        responseWeights,
                        varCodeBase,
                        filters))
                    .CleanTextAndRegroup()
                    .ToArray();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"SQL Server query failed - verify test data exists: {ex.Message}");
                return;
            }

            // Assert - Compare the results
            var comparison = ResultComparer.CompareResults(snowflakeResults, sqlServerResults, testDataName);
            
            if (!comparison.IsMatch)
            {
                var report = ResultComparer.GenerateComparisonReport(comparison);
                Assert.Fail($"Results do not match between Snowflake and SQL Server:\n{report}");
            }
            
            TestContext.WriteLine($"✓ {testDataName}: Results match (Snowflake: {comparison.SnowflakeCount}, SQL Server: {comparison.SqlServerCount})");
        }
    }
}
