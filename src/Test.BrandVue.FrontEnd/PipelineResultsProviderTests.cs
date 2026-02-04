using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon.Extensions;
using TestCommon.Mocks;
using Vue.AuthMiddleware;
using Vue.Common.Auth.Permissions;

namespace Test.BrandVue.FrontEnd
{
    [TestFixture]
    public class PipelineResultsProviderTests
    {
        private const string BRAND_FIELD = "BrandField";
        private const string RANKING_FIELD = "RankingField";
        private const string MONTHLY_AVERAGE = "Monthly";
        private const string CONSIDERATION_MEASURE = "Consideration";
        public static readonly EntityType BrandEntityType = new("brand", "Brand", "Brands");
        public static readonly EntityInstance Brand1 = new() { Id = 1, Name = nameof(Brand1) };
        private static readonly EntityInstance Brand2 = new() { Id = 2, Name = nameof(Brand2) };
        private static readonly EntityInstance Brand3 = new() { Id = 3, Name = nameof(Brand3) };
        private static readonly EntityInstance Brand4 = new() { Id = 4, Name = nameof(Brand4) };

        private static readonly EntityType ProductEntityType = new("product", "Product", "Products");
        private static readonly EntityInstance Product1 = new() { Id = 1, Name = nameof(Product1) };
        private static readonly EntityInstance Product2 = new() { Id = 2, Name = nameof(Product2) };

        public static readonly EntityType ImageEntityType = new("image", "Image", "Images");
        public static readonly EntityInstance Image1 = new() {Id = 21, Name = nameof(Image1)};
        private static readonly EntityInstance Image2 = new() {Id = 22, Name = nameof(Image2)};
        private static readonly EntityInstance Image3 = new() {Id = 23, Name = nameof(Image3)};

        private static readonly EntityType RankingEntityType = new("ranking", "Ranking", "Rankings");
        private static readonly EntityInstance Ranking1 = new() {Id = 10, Name = nameof(Ranking1) };
        private static readonly EntityInstance Ranking2 = new() {Id = 20, Name = nameof(Ranking2) };
        private static readonly EntityInstance Ranking3 = new() {Id = 30, Name = nameof(Ranking3) };
        private static readonly EntityInstance Ranking4 = new() {Id = 40, Name = nameof(Ranking4) };
        private static readonly EntityInstance Ranking5 = new() {Id = 50, Name = nameof(Ranking5) };
        private static readonly EntityInstance Ranking6 = new() {Id = 60, Name = nameof(Ranking6) };

        private static readonly Measure BrandMeasureOnly = new()
        {
            Name = nameof(BrandMeasureOnly),
            Field = new ResponseFieldDescriptor(BRAND_FIELD, TestEntityTypeRepository.Brand),
            BaseField = new ResponseFieldDescriptor(BRAND_FIELD, TestEntityTypeRepository.Brand),
        };
        private static readonly Measure BrandAndProductMeasure = new()
        {
            Name = nameof(BrandAndProductMeasure),
            Field = new ResponseFieldDescriptor("SomeOtherField", ProductEntityType),
            BaseField = new ResponseFieldDescriptor(BRAND_FIELD, TestEntityTypeRepository.Brand),
        };

        private static readonly Measure ThreeEntityMeasure = new()
        {
            Name = nameof(ThreeEntityMeasure),
            Field = new ResponseFieldDescriptor("ProductImageField", ProductEntityType, ImageEntityType),
            BaseField = new ResponseFieldDescriptor(BRAND_FIELD, TestEntityTypeRepository.Brand)
        };

        private static readonly Measure DownIsGoodBrandMeasureOnly = new()
        {
            Name = nameof(DownIsGoodBrandMeasureOnly),
            Field = new ResponseFieldDescriptor(BRAND_FIELD, TestEntityTypeRepository.Brand),
            BaseField = new ResponseFieldDescriptor(BRAND_FIELD, TestEntityTypeRepository.Brand),
            DownIsGood = true
        };

        private static readonly Measure RankingMeasure = new()
        {
            Name = nameof(RankingMeasure),
            Field = new ResponseFieldDescriptor(RANKING_FIELD, RankingEntityType),
            BaseField = new ResponseFieldDescriptor(RANKING_FIELD, RankingEntityType),
        };

        private static readonly Measure RankingDownIsGoodMeasure = new()
        {
            Name = nameof(RankingDownIsGoodMeasure),
            Field = new ResponseFieldDescriptor(RANKING_FIELD, RankingEntityType),
            BaseField = new ResponseFieldDescriptor(RANKING_FIELD, RankingEntityType),
            DownIsGood = true
        };

        private static readonly DateTimeOffset StartOfJan = DateTimeOffset.Parse("2021-01-01");
        private static readonly DateTimeOffset EndOfJan = DateTimeOffset.Parse("2021-01-31");
        private static readonly Subset UkSubset = new() { Id = "UK" };

        private PipelineResultsProvider _pipelineResultsProvider;

        [OneTimeSetUp]
        public void ConstructPipelineResultsProvider()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            TestContext.AddFormatter<CategoryResult>(c => JsonConvert.SerializeObject(c, Formatting.Indented));
            var measureCalculationOrchestrator = Substitute.For<IMetricCalculationOrchestrator>();
            var subsetRepository = new SubsetRepository { UkSubset };
            var dailyQuotaCellRespondentsSource = Substitute.For<IProfileResponseAccessorFactory>();
            var appSettings = new AppSettings();
            var configuration = Substitute.For<IConfiguration>();
            var initialWebAppConfig = new InitialWebAppConfig(appSettings, configuration);
            var breakdownCategoryFactory =  Substitute.For<IBreakdownCategoryFactory>();
            var convenientCalculator = CreateAndSetupConvenientCalculator(cancellationToken);
            var entityRepository = CreateEntityRepository();
            var demographicFilterToQuotaCellMapper = Substitute.For<IDemographicFilterToQuotaCellMapper>();
            demographicFilterToQuotaCellMapper.MapQuotaCellsFor(Arg.Any<Subset>(), Arg.Any<DemographicFilter>(),
                    Arg.Any<AverageDescriptor>())
                .ReturnsForAnyArgs(args => MockMetadata.CreateNonInterlockedQuotaCells(UkSubset, 2));
            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            var breakdownResultsProvider =  Substitute.For<IBreakdownResultsProvider>();
            var productContext = Substitute.For<IProductContext>();
            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            var filterRepository = Substitute.For<IFilterRepository>();
            baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(Arg.Any<Measure>(), Arg.Any<BaseExpressionDefinition>())
                .Returns(args => (Measure)args[0]);
            var measureRepository = CreateMeasureRepository();
            var metricConfigurationRepository = CreateMetricConfigurationRepository();
            var userDataPermissionsService = Substitute.For<IUserDataPermissionsService>();
            userDataPermissionsService.GetDataPermission().Returns(null as DataPermissionDto);
            var requestAdapter = new RequestAdapter(subsetRepository,
                SourceDataRepositoryMocks.GetAverageDescriptorRepository(),
                measureRepository,
                entityRepository,
                demographicFilterToQuotaCellMapper,
                CreateResponseEntityTypeRepository(),
                weightingPlanRepository,
                filterRepository,
                productContext,
                baseExpressionGenerator,
                new RequestScope(),
                Substitute.For<IQuestionTypeLookupRepository>(),
                userDataPermissionsService);
            var textResponseRepository = Substitute.For<IResponseRepository>();

            var profileResultsCalculator = Substitute.For<IProfileResultsCalculator>();
            var securityRestrictionsProvider = Substitute.For<ISubProductSecurityRestrictionsProvider>();
            var fakeRequestScope = new RequestScope("test", null, "test", RequestResource.PublicApi);

            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            var productConfigurationRepository = Substitute.For<IAllVueConfigurationRepository>();
            var logger = Substitute.For<ILogger<PipelineResultsProvider>>();

            _pipelineResultsProvider = new PipelineResultsProvider(measureCalculationOrchestrator,
                subsetRepository,
                dailyQuotaCellRespondentsSource,
                breakdownCategoryFactory,
                convenientCalculator,
                requestAdapter,
                breakdownResultsProvider,
                productContext,
                textResponseRepository,
                null,
                entityRepository,
                profileResultsCalculator,
                measureRepository,
                appSettings, 
                fakeRequestScope,
                filterRepository,
                metricConfigurationRepository,
                variableConfigurationRepository,
                logger);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetProfileResultsForTwoEntitiesFocusBrandInclusive(bool includeMarketAverage)
        {
            string[] measureNames = { BrandMeasureOnly.Name, BrandAndProductMeasure.Name };
            var multiEntityProfileModel = CreateMultiEntityProfileRequestModel(measureNames, 1, new[] {1, 2, 3}, includeMarketAverage);
            var categoryResults = await _pipelineResultsProvider.GetProfileResultsForMultipleEntities(multiEntityProfileModel, CancellationToken.None);
            Assert.That(categoryResults, Is.EqualTo(ExpectedTwoEntityResults(includeMarketAverage)).AsCollection.Using(CategoryEqualityComparer));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetProfileResultsForTwoEntitiesFocusBrandExclusive(bool includeMarketAverage)
        {
            string[] measureNames = { BrandMeasureOnly.Name, BrandAndProductMeasure.Name };
            var multiEntityProfileModel = CreateMultiEntityProfileRequestModel(measureNames, 4, new[] {1, 2, 3}, includeMarketAverage);
            var categoryResults = await _pipelineResultsProvider.GetProfileResultsForMultipleEntities(multiEntityProfileModel, CancellationToken.None);
            Assert.That(categoryResults, Is.EqualTo(ExpectedTwoEntityResults(includeMarketAverage)).AsCollection.Using(CategoryEqualityComparer));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetProfileResultsForThreeEntitiesFocusBrandInclusive(bool includeMarketAverage)
        {
            string[] measureNames = { BrandMeasureOnly.Name, ThreeEntityMeasure.Name };
            var multiEntityProfileModel = CreateMultiEntityProfileRequestModel(measureNames, 1, new[] {1, 2, 3}, includeMarketAverage);
            var categoryResults = await _pipelineResultsProvider.GetProfileResultsForMultipleEntities(multiEntityProfileModel, CancellationToken.None);
            Assert.That(categoryResults, Is.EqualTo(ExpectedThreeEntityResults(includeMarketAverage)).AsCollection.Using(CategoryEqualityComparer));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetProfileResultsForThreeEntitiesFocusBrandExclusive(bool includeMarketAverage)
        {
            string[] measureNames = { BrandMeasureOnly.Name, ThreeEntityMeasure.Name };
            var multiEntityProfileModel = CreateMultiEntityProfileRequestModel(measureNames, 4, new[] {1, 2, 3}, includeMarketAverage);
            var categoryResults = await _pipelineResultsProvider.GetProfileResultsForMultipleEntities(multiEntityProfileModel, CancellationToken.None);
            Assert.That(categoryResults, Is.EqualTo(ExpectedThreeEntityResults(includeMarketAverage)).AsCollection.Using(CategoryEqualityComparer));
        }

        [Test]
        public async Task Get_Sample_Size_For_Split_Metric_ResultsAsync()
        {
            const int nonExistingBrand=5;
            MultiEntityRequestModel model = new MultiEntityRequestModel(BrandMeasureOnly.Name, UkSubset.Id,
                MakePeriod(), 
                new EntityInstanceRequest("brand", [1, 2, 3, 4, nonExistingBrand]), 
                [
                    new ("brand", [1])
                ],
                new DemographicFilter(new FilterRepository()),
                new CompositeFilterModel(), 
                new MeasureFilterRequestModel[]
                {
                    new (CONSIDERATION_MEASURE, new Dictionary<string, int[]> {{"brand",[-1]}},false, false, new[]{-1}), 
                    new (CONSIDERATION_MEASURE, new Dictionary<string, int[]> {{"brand",[-1]}}, true,false, new []{-1})
                }, Array.Empty<BaseExpressionDefinition>(), false, SigConfidenceLevel.NinetyFive, 1);

            SplitMetricResults splitMetricResults = await _pipelineResultsProvider.GetSplitMetricResults(model, CancellationToken.None);

            var result = splitMetricResults.SampleSizeMetadata.SampleSize.Unweighted;
            Assert.That(result, Is.EqualTo(120));
            Assert.That(splitMetricResults.LowSampleSummary.Length, Is.EqualTo(2), "Was expecting 2 Low samples for Brand1 & Brand2");
            Assert.That(splitMetricResults.LowSampleSummary[0].EntityInstanceId, Is.EqualTo(Brand1.Id), $"Low sample id incorrect for {Brand1.Name}");
            Assert.That(splitMetricResults.LowSampleSummary[1].EntityInstanceId, Is.EqualTo(Brand2.Id), $"Low sample id incorrect for {Brand2.Name}");
        }

        [Test]
        public async Task SingleEntityGroupedResultsAreRankedCorrectlyAsync()
        {
            string[] measureNames = { BrandMeasureOnly.Name };
            var model = CreateCuratedResultRequestModel(measureNames, 4, [1, 2, 3]);
            var rankedResults = await _pipelineResultsProvider.GetRankingTableResult(model, CancellationToken.None);

            // Expect:
            // Id 1 => weighted result 40 => rank1
            // Id 2 => weighted result 30 => joint rank2
            // Id 3 => weighted result 20 => rank4
            // Id 4 => weighted result 30 => joint rank2

            Assert.Multiple(() =>
            {
                Assert.That(rankedResults.Results.Count, Is.EqualTo(4));

                var resultId1 = rankedResults.Results[0];
                Assert.That(resultId1.EntityInstance.Id, Is.EqualTo(1));
                Assert.That(resultId1.CurrentRank, Is.EqualTo(1));
                Assert.That(resultId1.MultipleWithCurrentRank, Is.False);

                var resultId2 = rankedResults.Results[1];
                Assert.That(resultId2.EntityInstance.Id, Is.EqualTo(2));
                Assert.That(resultId2.CurrentRank, Is.EqualTo(2));
                Assert.That(resultId2.MultipleWithCurrentRank, Is.True);

                var resultId3 = rankedResults.Results[2];
                Assert.That(resultId3.EntityInstance.Id, Is.EqualTo(4));
                Assert.That(resultId3.CurrentRank, Is.EqualTo(2));
                Assert.That(resultId3.MultipleWithCurrentRank, Is.True);

                var resultId4 = rankedResults.Results[3];
                Assert.That(resultId4.EntityInstance.Id, Is.EqualTo(3));
                Assert.That(resultId4.CurrentRank, Is.EqualTo(4));
                Assert.That(resultId4.MultipleWithCurrentRank, Is.False);
            });
        }

        [Test]
        public async Task SingleEntityGroupedDownIsGoodResultsAreRankedCorrectlyAsync()
        {
            string[] measureNames = { DownIsGoodBrandMeasureOnly.Name, ThreeEntityMeasure.Name };
            var model = CreateCuratedResultRequestModel(measureNames, 4, new[] { 1, 2, 3 });
            var rankedResults = await _pipelineResultsProvider.GetRankingTableResult(model, CancellationToken.None);
            
            // Expect:
            // Id 1 => weighted result 40 => rank4
            // Id 2 => weighted result 30 => joint rank2
            // Id 3 => weighted result 20 => rank1
            // Id 4 => weighted result 30 => joint rank2

            Assert.Multiple(() =>
            {
                Assert.That(rankedResults.Results.Count, Is.EqualTo(4));

                var resultId1 = rankedResults.Results[0];
                Assert.That(resultId1.EntityInstance.Id, Is.EqualTo(3));
                Assert.That(resultId1.CurrentRank, Is.EqualTo(1));
                Assert.That(resultId1.MultipleWithCurrentRank, Is.False);

                var resultId2 = rankedResults.Results[1];
                Assert.That(resultId2.EntityInstance.Id, Is.EqualTo(2));
                Assert.That(resultId2.CurrentRank, Is.EqualTo(2));
                Assert.That(resultId2.MultipleWithCurrentRank, Is.True);

                var resultId3 = rankedResults.Results[2];
                Assert.That(resultId3.EntityInstance.Id, Is.EqualTo(4));
                Assert.That(resultId3.CurrentRank, Is.EqualTo(2));
                Assert.That(resultId3.MultipleWithCurrentRank, Is.True);

                var resultId4 = rankedResults.Results[3];
                Assert.That(resultId4.EntityInstance.Id, Is.EqualTo(1));
                Assert.That(resultId4.CurrentRank, Is.EqualTo(4));
                Assert.That(resultId4.MultipleWithCurrentRank, Is.False);
            });
        }

        [Test]
        public async Task SingleEntityResultsAreRankedCorrectlyAsync()
        {
            string[] measureNames = { RankingMeasure.Name };
            var model = CreateCuratedResultRequestModel(measureNames, 10, new[] { 10, 20, 30, 40, 50, 60 });
            var rankedResults = await _pipelineResultsProvider.GetRankingTableResult(model, CancellationToken.None);

            // Expect:
            // Id 10 => weighted result 30 => rank4
            // Id 20 => weighted result 40 => rank3
            // Id 30 => weighted result 60 => rank1
            // Id 40 => weighted result 50 => rank2
            // Id 50 => weighted result 20 => rank5
            // Id 60 => weighted result 10 => rank6

            Assert.Multiple(() =>
            {
                Assert.That(rankedResults.Results.Count, Is.EqualTo(6));

                var resultId1 = rankedResults.Results[0];
                Assert.That(resultId1.EntityInstance.Id, Is.EqualTo(30));
                Assert.That(resultId1.CurrentRank, Is.EqualTo(1));
                Assert.That(resultId1.MultipleWithCurrentRank, Is.False);

                var resultId2 = rankedResults.Results[1];
                Assert.That(resultId2.EntityInstance.Id, Is.EqualTo(40));
                Assert.That(resultId2.CurrentRank, Is.EqualTo(2));
                Assert.That(resultId2.MultipleWithCurrentRank, Is.False);

                var resultId3 = rankedResults.Results[2];
                Assert.That(resultId3.EntityInstance.Id, Is.EqualTo(20));
                Assert.That(resultId3.CurrentRank, Is.EqualTo(3));
                Assert.That(resultId3.MultipleWithCurrentRank, Is.False);

                var resultId4 = rankedResults.Results[3];
                Assert.That(resultId4.EntityInstance.Id, Is.EqualTo(10));
                Assert.That(resultId4.CurrentRank, Is.EqualTo(4));
                Assert.That(resultId4.MultipleWithCurrentRank, Is.False);

                var resultId5 = rankedResults.Results[4];
                Assert.That(resultId5.EntityInstance.Id, Is.EqualTo(50));
                Assert.That(resultId5.CurrentRank, Is.EqualTo(5));
                Assert.That(resultId5.MultipleWithCurrentRank, Is.False);

                var resultId6 = rankedResults.Results[5];
                Assert.That(resultId6.EntityInstance.Id, Is.EqualTo(60));
                Assert.That(resultId6.CurrentRank, Is.EqualTo(6));
                Assert.That(resultId6.MultipleWithCurrentRank, Is.False);
            });
        }

        [Test]
        public async Task SingleEntityDownIsGoodResultsAreRankedCorrectlyAsync()
        {
            string[] measureNames = { RankingDownIsGoodMeasure.Name };
            var model = CreateCuratedResultRequestModel(measureNames, 10, new[] { 10, 20, 30, 40, 50, 60 });
            var rankedResults = await _pipelineResultsProvider.GetRankingTableResult(model, CancellationToken.None);

            // Expect:
            // Id 10 => weighted result 30 => rank3
            // Id 20 => weighted result 40 => rank4
            // Id 30 => weighted result 60 => rank6
            // Id 40 => weighted result 50 => rank5
            // Id 50 => weighted result 20 => rank2
            // Id 60 => weighted result 10 => rank1

            Assert.Multiple(() =>
            {
                Assert.That(rankedResults.Results.Count, Is.EqualTo(6));

                var resultId1 = rankedResults.Results[0];
                Assert.That(resultId1.EntityInstance.Id, Is.EqualTo(60));
                Assert.That(resultId1.CurrentRank, Is.EqualTo(1));
                Assert.That(resultId1.MultipleWithCurrentRank, Is.False);

                var resultId2 = rankedResults.Results[1];
                Assert.That(resultId2.EntityInstance.Id, Is.EqualTo(50));
                Assert.That(resultId2.CurrentRank, Is.EqualTo(2));
                Assert.That(resultId2.MultipleWithCurrentRank, Is.False);

                var resultId3 = rankedResults.Results[2];
                Assert.That(resultId3.EntityInstance.Id, Is.EqualTo(10));
                Assert.That(resultId3.CurrentRank, Is.EqualTo(3));
                Assert.That(resultId3.MultipleWithCurrentRank, Is.False);

                var resultId4 = rankedResults.Results[3];
                Assert.That(resultId4.EntityInstance.Id, Is.EqualTo(20));
                Assert.That(resultId4.CurrentRank, Is.EqualTo(4));
                Assert.That(resultId4.MultipleWithCurrentRank, Is.False);

                var resultId5 = rankedResults.Results[4];
                Assert.That(resultId5.EntityInstance.Id, Is.EqualTo(40));
                Assert.That(resultId5.CurrentRank, Is.EqualTo(5));
                Assert.That(resultId5.MultipleWithCurrentRank, Is.False);

                var resultId6 = rankedResults.Results[5];
                Assert.That(resultId6.EntityInstance.Id, Is.EqualTo(30));
                Assert.That(resultId6.CurrentRank, Is.EqualTo(6));
                Assert.That(resultId6.MultipleWithCurrentRank, Is.False);
            });
        }
        
        private static Period MakePeriod()
        {
            var calculationPeriodSpan = new CalculationPeriodSpan { StartDate = StartOfJan, EndDate = EndOfJan };
            return new Period { Average = MONTHLY_AVERAGE, ComparisonDates = calculationPeriodSpan.Yield().ToArray() };
        }

        private static MultiEntityProfileModel CreateMultiEntityProfileRequestModel(string[] measureNames, int activeEntityId, int[] requestedEntityIds, bool includeMarketAverage)
        {
            var multipleInstanceRequest = new EntityInstanceRequest(EntityType.Brand, requestedEntityIds);
            var calculationPeriodSpan = new CalculationPeriodSpan {StartDate = StartOfJan, EndDate = EndOfJan};
            var period = new Period {Average = MONTHLY_AVERAGE, ComparisonDates = calculationPeriodSpan.Yield().ToArray()};
            return new MultiEntityProfileModel(UkSubset.Id, period, multipleInstanceRequest, activeEntityId, measureNames, Array.Empty<int>(), includeMarketAverage);
        }

        private static CuratedResultsModel CreateCuratedResultRequestModel(string[] measureNames, int activeEntityId,
            int[] requestedEntityIds)
        {
            var calculationPeriodSpan = new CalculationPeriodSpan { StartDate = StartOfJan, EndDate = EndOfJan };
            var period = new Period { Average = MONTHLY_AVERAGE, ComparisonDates = calculationPeriodSpan.Yield().ToArray() };
            SigDiffOptions sigOptions = new SigDiffOptions(
                false,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);
            return new(new DemographicFilter(), requestedEntityIds, UkSubset.Id, measureNames,
                period, activeEntityId, new CompositeFilterModel(), sigOptions);
        }

        private static CategoryResult[] ExpectedTwoEntityResults(bool includeMarketAverage)
        {
            int? average = includeMarketAverage ? 0 : null;
            return new CategoryResult[]
            {
                new(BrandMeasureOnly.Name, null, new WeightedDailyResult(EndOfJan), average),
                new(BrandAndProductMeasure.Name, Product1.Name, new WeightedDailyResult(EndOfJan), average),
                new(BrandAndProductMeasure.Name, Product2.Name, new WeightedDailyResult(EndOfJan), average)
            };
        }

        private static CategoryResult[] ExpectedThreeEntityResults(bool includeMarketAverage)
        {
            int? average = includeMarketAverage ? 0 : null;
            return new CategoryResult[]
            {
                new(BrandMeasureOnly.Name, null, new WeightedDailyResult(EndOfJan), average),
                new(ThreeEntityMeasure.Name, $"{Image1.Name}, {Product1.Name}", new WeightedDailyResult(EndOfJan), average),
                new(ThreeEntityMeasure.Name, $"{Image1.Name}, {Product2.Name}", new WeightedDailyResult(EndOfJan), average),
                new(ThreeEntityMeasure.Name, $"{Image2.Name}, {Product1.Name}", new WeightedDailyResult(EndOfJan), average),
                new(ThreeEntityMeasure.Name, $"{Image2.Name}, {Product2.Name}", new WeightedDailyResult(EndOfJan), average),
                new(ThreeEntityMeasure.Name, $"{Image3.Name}, {Product1.Name}", new WeightedDailyResult(EndOfJan), average),
                new(ThreeEntityMeasure.Name, $"{Image3.Name}, {Product2.Name}", new WeightedDailyResult(EndOfJan), average),
            };
        }

        private static IConvenientCalculator CreateAndSetupConvenientCalculator(CancellationToken cancellationToken)
        {
            var convenientCalculator = Substitute.For<IConvenientCalculator>();
            convenientCalculator.GetCuratedMarketResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(args =>
                {
                    var resultsProviderParameters = args.Arg<ResultsProviderParameters>();
                    var primaryMeasure = resultsProviderParameters.PrimaryMeasure;
                    var entityWeightedDailyResultsArray = resultsProviderParameters.EntityInstances.Select(b =>
                        new EntityWeightedDailyResults(b, new List<WeightedDailyResult>
                        {
                            new(EndOfJan)
                        })).ToArray();
                    return (primaryMeasure, entityWeightedDailyResultsArray).Yield().ToArray();
                });
            convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(args =>
                {
                    var resultsProviderParameters = args.Arg<ResultsProviderParameters>();
                    var primaryMeasure = resultsProviderParameters.PrimaryMeasure;
                    var weightedDailyResults = new List<WeightedDailyResult>
                    {
                        new(EndOfJan)
                    };
                    var entityWeightedDailyResultsArray = resultsProviderParameters.EntityInstances.Select(b =>
                        new EntityWeightedDailyResults(b, new List<WeightedDailyResult>
                        {
                            new(EndOfJan)
                        })).ToArray();
                    return (primaryMeasure, (IList<WeightedDailyResult>) weightedDailyResults, entityWeightedDailyResultsArray)
                        .Yield().ToArray();
                });
            convenientCalculator.CalculateWeightedForMeasure(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(args =>
                {
                    var resultsProviderParameters = args.Arg<ResultsProviderParameters>();
                    return resultsProviderParameters.EntityInstances.Select(b =>
                        new EntityWeightedDailyResults(b, new List<WeightedDailyResult>
                        {
                            new(EndOfJan)
                            {
                                WeightedResult = GetWeightedResult(b)
                            }
                        })).ToArray();
                });
            convenientCalculator.GetCuratedResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), CancellationToken.None)
                .Returns(callInfo=>
                {
                    var instancesOfData = callInfo.Arg<ResultsProviderParameters>().EntityInstances;
                    var data = instancesOfData.Select(instance => new EntityWeightedDailyResults(
                        instance,
                        new List<WeightedDailyResult>() { new(EndOfJan) { UnweightedSampleSize = (uint) instance.Id * 10 + 50} }
                    )).ToArray();
                    return new ResultsForMeasure[]
                    {
                        new()
                        {
                            Measure = new Measure(),
                            Data = data,
                        }
                    };
                });
            return convenientCalculator;
        }

        // Used in ranking tests
        private static uint GetWeightedResult(EntityInstance e)
        {
            switch (e.Id)
            {
                // Grouped ranking
                case 1:
                    return 40;
                case 2:
                case 4:
                    return 30;
                case 3:
                    return 20;

                // Non-grouped ranking
                case 10:
                    return 30;
                case 20:
                    return 40;
                case 30:
                    return 60;
                case 40:
                    return 50;
                case 50:
                    return 20;
                case 60:
                    return 10;

                default:
                    return (uint)e.Id;
            }
        }

        private static EntityTypeRepository CreateResponseEntityTypeRepository()
        {
            var responseEntityTypeRepository = new EntityTypeRepository();
            responseEntityTypeRepository.TryAdd(EntityType.Brand, TestEntityTypeRepository.Brand);
            responseEntityTypeRepository.TryAdd(ProductEntityType.Identifier, ProductEntityType);
            responseEntityTypeRepository.TryAdd(ImageEntityType.Identifier, ImageEntityType);
            return responseEntityTypeRepository;
        }

        private static MetricRepository CreateMeasureRepository()
        {
            var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
            var measureRepository = new MetricRepository(userPermissionsService);
            measureRepository.TryAdd(BrandMeasureOnly.Name, BrandMeasureOnly);
            measureRepository.TryAdd(BrandAndProductMeasure.Name, BrandAndProductMeasure);
            measureRepository.TryAdd(ThreeEntityMeasure.Name, ThreeEntityMeasure);
            measureRepository.TryAdd(DownIsGoodBrandMeasureOnly.Name, DownIsGoodBrandMeasureOnly);
            measureRepository.TryAdd(RankingMeasure.Name, RankingMeasure);
            measureRepository.TryAdd(RankingDownIsGoodMeasure.Name, RankingDownIsGoodMeasure);
            return measureRepository;
        }

        private static IMetricConfigurationRepository CreateMetricConfigurationRepository()
        {
            var metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            metricConfigurationRepository.GetAll().Returns(_ => new List<MetricConfiguration>
            {
                new() { Name = BrandMeasureOnly.Name },
                new() { Name = BrandAndProductMeasure.Name },
                new() { Name = ThreeEntityMeasure.Name },
                new() { Name = DownIsGoodBrandMeasureOnly.Name },
                new() { Name = RankingMeasure.Name },
                new() { Name = RankingDownIsGoodMeasure.Name },
            });
            metricConfigurationRepository.Get(Arg.Any<string>())
                .Returns(args => new MetricConfiguration { Name = (string)args[0] });
            return metricConfigurationRepository;
        }

        public static IEntityRepository CreateEntityRepository()
        {
            var entityRepository = new EntityInstanceRepository();
            entityRepository.Add(TestEntityTypeRepository.Brand, Brand1);
            entityRepository.Add(TestEntityTypeRepository.Brand, Brand2);
            entityRepository.Add(TestEntityTypeRepository.Brand, Brand3);
            entityRepository.Add(TestEntityTypeRepository.Brand, Brand4);
            entityRepository.Add(ProductEntityType, Product1);
            entityRepository.Add(ProductEntityType, Product2);
            entityRepository.Add(ImageEntityType, Image1);
            entityRepository.Add(ImageEntityType, Image2);
            entityRepository.Add(ImageEntityType, Image3);
            entityRepository.Add(RankingEntityType, Ranking1);
            entityRepository.Add(RankingEntityType, Ranking2);
            entityRepository.Add(RankingEntityType, Ranking3);
            entityRepository.Add(RankingEntityType, Ranking4);
            entityRepository.Add(RankingEntityType, Ranking5);
            entityRepository.Add(RankingEntityType, Ranking6);
            return entityRepository;
        }

        private static CategoryResultsEqualityComparer CategoryEqualityComparer => new();

        private class CategoryResultsEqualityComparer : IEqualityComparer<CategoryResult>
        {
            public bool Equals(CategoryResult x, CategoryResult y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.MeasureName == y.MeasureName && x.EntityInstanceName == y.EntityInstanceName;
            }

            public int GetHashCode(CategoryResult obj)
            {
                return 0;
            }
        }
    }
}