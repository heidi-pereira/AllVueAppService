using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.Rim;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestCommon.Weighting;
using TestAnswer = TestCommon.DataPopulation.TestAnswer;

namespace Test.BrandVue.SourceData.Weightings
{
    public class WeightingAlgorithmServiceTests
    {
        private const int SurveyId = 1000;
        
        private const string WaveMetricName = "DecemberWeeksWaveVariable";
        private const string GenderVarCode = "Gender";
        private const string RegionVarCode = "Region";
        private static readonly EntityType Wave = new(WaveMetricName, "DecemberWeeksWaveVariable", "DecemberWeeksWavesVariable");
        private static readonly EntityValue Wave1 = new(Wave, 1);
        private static readonly EntityValue Wave2 = new(Wave, 2);
        private static readonly EntityValue Wave3 = new(Wave, 3);
        private static readonly EntityValue Wave4 = new(Wave, 4);
        private static readonly EntityValue Wave5 = new(Wave, 5);
        private WeightingAlgorithmService _weightingAlgorithmService;
        private string _subsetId;
        private Question _genderQuestion;
        private Question _regionQuestion;
        private TestDataLoader _loader;
        private DataWaveVariable _dataWaveVariable;
        private Measure _filterMeasure;
        private WeightingSchemeDetails _equallyBalancedRimWeightingSchemeDetails;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var groupedVariableDefinition = WeightingTestObjects.GetGroupedVariableDefinition();
            _dataWaveVariable = new DataWaveVariable(groupedVariableDefinition);
            _filterMeasure = new Measure { Name = WaveMetricName, PrimaryVariable = _dataWaveVariable, BaseExpressionString = null};

            var genderEntityType = new EntityType(GenderVarCode, GenderVarCode, GenderVarCode + "s");
            var genderChoices = CreateChoices("Female", "Male", "Other");
            _genderQuestion = CreateQuestion(GenderVarCode, genderChoices);

            var regionEntityType = new EntityType(RegionVarCode, RegionVarCode, RegionVarCode + "s");
            var regionChoices = CreateChoices("North", "South");
            _regionQuestion = CreateQuestion(RegionVarCode, regionChoices);

            var responseEntityTypeRepository = new TestEntityTypeRepository( genderEntityType, regionEntityType, Wave);
            var rfm = new ResponseFieldManager(NullLogger<ResponseFieldManager>.Instance, responseEntityTypeRepository);
            var genderField = rfm.Add(GenderVarCode, genderEntityType);
            var genderAskedField = rfm.Add(GenderVarCode + "_asked");

            var genderEntities = genderChoices.Select(c => new EntityValue(genderEntityType, c.SurveyChoiceId)).ToArray();
            var genderEntity1 = genderEntities[1];

            var regionField = rfm.Add(RegionVarCode, regionEntityType);
            var regionAskedField = rfm.Add(RegionVarCode + "_asked");
            var regionEntities = regionChoices.Select(c => new EntityValue(regionEntityType, c.SurveyChoiceId)).ToArray();
            var regionEntity1 = regionEntities[1];

            var respondentsAnswers = Enumerable.Repeat(
                new[]
                {
                    TestAnswer.For(genderField, genderEntity1.Value, genderEntity1),
                    TestAnswer.For(genderAskedField, genderEntity1.Value),
                    TestAnswer.For(regionField, regionEntity1.Value, regionEntity1),
                    TestAnswer.For(regionAskedField, regionEntity1.Value),
                }, 3).ToArray();


            _loader = new ProductionCalculatorBuilder()
                .IncludeEntities(genderEntities.Concat(regionEntities).Concat(new[]{Wave1, Wave2, Wave3, Wave4, Wave5}).ToArray())
                .IncludeQuestions(_genderQuestion, _regionQuestion)
                .WithWeightingPlansAndResponses([], new Dictionary<string, TestAnswer[][]>(), _dataWaveVariable)
                .IncludeMeasures(new[]{_filterMeasure}, new[]{groupedVariableDefinition})
                .WithCalculationPeriod(new CalculationPeriod(DateTimeOffset.Parse("2020/12/08"), DateTimeOffset.Parse("2020/12/08").EndOfDay()))
                .WithResponses(respondentsAnswers.Select(answers => new ResponseAnswers(answers)).ToArray())
                .BuildLoaderWithInMemoryDb();
            _subsetId = _loader.SubsetRepository.First().Id;

            _equallyBalancedRimWeightingSchemeDetails = new WeightingSchemeDetails
            {
                Dimensions = new List<Dimension>
                {
                    new()
                    {
                        InterlockedVariableIdentifiers = new[] { _genderQuestion.VarCode },
                        CellKeyToTarget = new Dictionary<string, decimal>
                        {
                            { "1", 0.5m },
                            { "2", 0.5m }
                        }
                    },
                    new()
                    {
                        InterlockedVariableIdentifiers = new[] { _regionQuestion.VarCode },
                        CellKeyToTarget = new Dictionary<string, decimal>
                        {
                            { "1", 0.5m },
                            { "2", 0.5m }
                        }
                    }
                }
            };

            _weightingAlgorithmService = new WeightingAlgorithmService(_loader.SubsetRepository,
                _loader.EntityInstanceRepository,
                _loader.MeasureRepository,
                new RimWeightingCalculator(), _loader.SampleSizeProvider, 
                _loader.RespondentRepositorySource, Substitute.For<IProfileResponseAccessorFactory>(),
                _loader.BaseExpressionGenerator
                );
        }

        [TestCase(WaveMetricName, 1, ExpectedResult = 0)]
        [TestCase(WaveMetricName, 2, ExpectedResult = 3)]
        [TestCase(null, null, ExpectedResult = 3)]
        public async Task<double> GetRimTotalSampleSizeAsync(string filterMetricName, int filterInstanceId) =>
            await _weightingAlgorithmService.GetRimTotalSampleSize(_subsetId, filterMetricName, filterInstanceId, CancellationToken.None);


        [Test]
        public async Task GetRimDimensionSampleSizesWithNoFilterMetricAsync()
        {
            var sampleSizes = await _weightingAlgorithmService.GetRimDimensionSampleSizes(_subsetId, GenderVarCode, null, null, CancellationToken.None);
            var actualResults = sampleSizes.Select(s => (s.Key.Id, s.Value));
            Assert.That(actualResults, Is.EqualTo(new[] { (0, 0f), (1, 3f), (2, 0f) }));
        }

        [Test]
        public async Task GetRimDimensionSampleSizesWithEmptyFilterInstanceIdAsync()
        {
            var sampleSizes = await _weightingAlgorithmService.GetRimDimensionSampleSizes(_subsetId, GenderVarCode, WaveMetricName, 1, CancellationToken.None);
            var actualResults = sampleSizes.Select(s => (s.Key.Id, s.Value));
            Assert.That(actualResults, Is.EqualTo(new[] { (0, 0f), (1, 0f), (2, 0f) }));
        }

        [Test]
        public async Task GetRimDimensionSampleSizesWithPopulatedFilterInstanceIdAsync()
        {
            var sampleSizes = await _weightingAlgorithmService.GetRimDimensionSampleSizes(_subsetId, GenderVarCode, WaveMetricName, 2, CancellationToken.None);
            var actualResults = sampleSizes.Select(s => (s.Key.Id, s.Value));
            Assert.That(actualResults, Is.EqualTo(new[] { (0, 0f), (1, 3f), (2, 0f) }));
        }


        [Test]
        public async Task GetRimDimensionSampleSizesWithPopulatedSingleFilterInstanceArrayAsync()
        {
            var filters = new List<WeightingFilterInstance>();
            filters.Add(new WeightingFilterInstance(WaveMetricName, 2));
            var sampleSizes = await _weightingAlgorithmService.GetRimDimensionSampleSizes(_subsetId, GenderVarCode, filters, CancellationToken.None);
            var actualResults = sampleSizes.Select(s => (s.Key.Id, s.Value));
            Assert.That(actualResults, Is.EqualTo(new[] { (0, 0.0), (1, 3.0), (2, 0.0) }));
        }

        [TestCase(RegionVarCode, 1, 3.0)]
        [TestCase(RegionVarCode, 2, 0.0)]
        public async Task GetRimDimensionSampleSizesWithPopulatedMultipleFilterInstanceArrayAsync(string name, int instanceId, double expectedNumberOfResults)
        {
            var filters = new List<WeightingFilterInstance>();
            filters.Add(new WeightingFilterInstance(WaveMetricName, 2));
            filters.Add(new WeightingFilterInstance(name, instanceId));
            var sampleSizes = await _weightingAlgorithmService.GetRimDimensionSampleSizes(_subsetId, GenderVarCode, filters, CancellationToken.None);
            var actualResults = sampleSizes.Select(s => (s.Key.Id, s.Value));
            Assert.That(actualResults, Is.EqualTo(new[] { (0, 0.0), (1, expectedNumberOfResults), (2, 0.0) }));
        }


        [Test]
        public async Task GetRimDimensionVariableSampleSizesWithPopulatedFilterInstanceIdAsync()
        {
            var sampleSizes = await _weightingAlgorithmService.GetRimDimensionSampleSizes(_subsetId, WaveMetricName, GenderVarCode, 1, CancellationToken.None);
            var actualResults = sampleSizes.Select(s => (s.Key.Id, s.Value));
            Assert.That(actualResults, Is.EqualTo(new[] { (1, 0f), (2, 3f), (3, 0f), (4, 0f), (5, 0f) }));
        }

      

        private Question CreateQuestion(string varCode, params Choice[] choices)
        {
            return new Question()
            {
                SurveyId = SurveyId,
                VarCode = varCode,
                QuestionText = varCode,
                AnswerChoiceSet = new ChoiceSet()
                {
                    SurveyId = SurveyId,
                    Name = varCode,
                    Choices = choices.ToList(),
                },
            };
        }

        private static Choice[] CreateChoices(params string[] choiceNames) =>
            choiceNames.Select((c, i) => new Choice() { Name = c, SurveyChoiceId = i }).ToArray();
    }
}
