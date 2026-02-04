using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.QuotaCells;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Calculation;
using System;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.Page;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class WaveResultsProviderTests
    {
        private const string MeasureName = "Measure1";
        private const string SubsetId = "subset123";
        private const string ComparandWave = "Wave1";
        private const string WaveName = "Wave1";
        private const string BreakName = "Break1";
        private WaveResultsProvider _provider;
        private IRequestAdapter _requestAdapter;
        private IConvenientCalculator _convenientCalculator;

        [SetUp]
        public void SetUp()
        {
            _requestAdapter = Substitute.For<IRequestAdapter>();
            _convenientCalculator = Substitute.For<IConvenientCalculator>();
            _provider = new WaveResultsProvider(_requestAdapter, _convenientCalculator);
        }

        [Test]
        public async Task GetWaveComparisonResults_CuratedResultsModel_CallsCreateParametersForCalculation()
        {
            // Arrange
            var model = GetDefaultCuratedResultsModel();
            var waves = new List<CompositeFilterModel>()
            {
                new() { Name = WaveName },
            };
            var breaks = new List<CompositeFilterModel>();
            var cancellationToken = CancellationToken.None;
            var weightedDailyResults = new WeightedDailyResult(DateTime.Now);

            _requestAdapter.CreateParametersForCalculation(model, Arg.Any<CompositeFilterModel>())
                .Returns(new ResultsProviderParameters());
            _convenientCalculator.GetCuratedResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(_ => Task.FromResult(new ResultsForMeasure[1]
                { new()
                    {
                        Measure = new Measure(),
                        Data = [ new (new EntityInstance(), [weightedDailyResults]) ],
                        NumberFormat = "",
                    }
                }));

            // Act
            await _provider.GetWaveComparisonResults(model, waves, breaks, ComparandWave, cancellationToken);

            // Assert
            _requestAdapter.Received(2).CreateParametersForCalculation(model, Arg.Any<CompositeFilterModel>());
        }

        [Test]
        public async Task GetWaveComparisonResults_MultiEntityRequestModelWithShowMeanInReports_CallsCreateParametersForCalculation()
        {
            // Arrange
            var model = new MultiEntityRequestModel(
                MeasureName,
                SubsetId,
                new Period(),
                new EntityInstanceRequest("", []),
                [],
                new DemographicFilter(),
                new CompositeFilterModel(),
                [],
                [],
                true,
                SigConfidenceLevel.NinetyFive);
            var waves = new List<CompositeFilterModel>()
            {
                new() { Name = WaveName },
            };
            var breaks = new List<CompositeFilterModel>();
            var cancellationToken = CancellationToken.None;
            var weightedDailyResults = new WeightedDailyResult(DateTime.Now);
            var measure = new Measure();
            measure.CalculationType = CalculationType.Average;
            var resultsProviderParameters = new ResultsProviderParameters();
            resultsProviderParameters.PrimaryMeasure = measure;

            _requestAdapter.CreateParametersForCalculation(model, Arg.Any<CompositeFilterModel>())
                .Returns(resultsProviderParameters);
            _convenientCalculator.GetCuratedResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(_ => Task.FromResult(new ResultsForMeasure[1]
                { new()
                    {
                        Measure = measure,
                        Data = [ new (new EntityInstance(), [weightedDailyResults]) ],
                        NumberFormat = "",
                    }
                }));

            // Act
            await _provider.GetWaveComparisonResults(model, waves, breaks, ComparandWave, cancellationToken);

            // Assert
            _requestAdapter.Received(2).CreateParametersForCalculation(model, Arg.Any<CompositeFilterModel>());
        }

        [Test]
        public async Task GetWaveComparisonResults_CuratedResultsModel_ReturnsExpectedResults()
        {
            // Arrange
            var model = GetDefaultCuratedResultsModel();
            var waves = new List<CompositeFilterModel>()
            {
                new() { Name = WaveName },
            };
            var breaks = new List<CompositeFilterModel>()
            {
                new() { Name = BreakName },
            };
            var cancellationToken = CancellationToken.None;
            var weightedDailyResults = new WeightedDailyResult(DateTime.Now);
            var expectedResults = new WaveComparisonResults();

            _requestAdapter.CreateParametersForCalculation(model, Arg.Any<CompositeFilterModel>())
                .Returns(new ResultsProviderParameters());
            _convenientCalculator.GetCuratedResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(_ => Task.FromResult(new ResultsForMeasure[1]
                { new()
                    {
                        Measure = new Measure(),
                        Data = [ new (new EntityInstance(), [weightedDailyResults]) ],
                        NumberFormat = "",
                    }
                }));

            // Act
            var result = await _provider.GetWaveComparisonResults(model, waves, breaks, null, cancellationToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(expectedResults.GetType(), Is.EqualTo(result.GetType()));
        }

        [Test]
        public async Task GetWaveComparisonResults_MultiEntityRequestModel_ReturnsExpectedResults()
        {
            // Arrange
            var model = new MultiEntityRequestModel(
                MeasureName,
                SubsetId,
                new Period(),
                new EntityInstanceRequest("", []),
                [],
                new DemographicFilter(),
                new CompositeFilterModel(),
                [],
                [],
                true,
                SigConfidenceLevel.NinetyFive);
            var waves = new List<CompositeFilterModel>()
            {
                new() { Name = WaveName },
            };
            var breaks = new List<CompositeFilterModel>();
            var cancellationToken = CancellationToken.None;
            var weightedDailyResults = new WeightedDailyResult(DateTime.Now);
            var expectedResults = new WaveComparisonResults();

            _requestAdapter.CreateParametersForCalculation(model, Arg.Any<CompositeFilterModel>())
                .Returns(new ResultsProviderParameters());
            _convenientCalculator.GetCuratedResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(_ => Task.FromResult(new ResultsForMeasure[1]
                { new()
                    {
                        Measure = new Measure(),
                        Data = [ new (new EntityInstance(), [weightedDailyResults]) ],
                        NumberFormat = "",
                    }
                }));

            // Act
            var result = await _provider.GetWaveComparisonResults(model, waves, breaks, "wave1", cancellationToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(expectedResults.GetType(), Is.EqualTo(result.GetType()));
        }

        #region Utility Methods
        private static CuratedResultsModel GetDefaultCuratedResultsModel()
        {
            var demographicFilter = new DemographicFilter();

            int[] entityInstanceIds = [1, 2, 3];
            string[] measureName = ["Measure1", "Measure2"];
            var period = new Period();
            int activeBrandId = 123;
            var filterModel = new CompositeFilterModel();
            bool includeSignificance = true;
            string[] ordering = ["Order1", "Order2"];
            var orderingDirection = DataSortOrder.Ascending;
            MeasureFilterRequestModel[] additionalMeasureFilters =
            {
                new(MeasureName, [], false, false),
            };
            var baseExpressionOverride = new BaseExpressionDefinition();
            SigDiffOptions sigOptions = new SigDiffOptions(
               includeSignificance,
               SigConfidenceLevel.NinetyFive,
               DisplaySignificanceDifferences.ShowBoth,
               CrosstabSignificanceType.CompareToTotal);

            var curatedResultsModel = new CuratedResultsModel(
                demographicFilter,
                entityInstanceIds,
                SubsetId,
                measureName,
                period,
                activeBrandId,
                filterModel,
                sigOptions,
                ordering,
                orderingDirection,
                additionalMeasureFilters,
                baseExpressionOverride
            );
            return curatedResultsModel;
        }
        #endregion
    }
}