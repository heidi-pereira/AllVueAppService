using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using VerifyNUnit;

namespace Test.BrandVue.SourceData.Measures
{
    public class MetricFactoryTests
    {
        private IMetricFactory _metricFactory;
        private IVariableFactory _variableFactory;

        private IMetricConfigurationRepository _metricConfigurationRepository;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private IVariable<int?> Variable;
        private IVariable<int?> ParsedVariable;

        [OneTimeSetUp]
        public void ConstructVariableFactory()
        {
            var responseFieldManager = Substitute.For<IResponseFieldManager>();
            var entityRepository = Substitute.For<IEntityRepository>();
            var responseEntityTypeRepository = Substitute.For<IResponseEntityTypeRepository>();

            var fieldExpressionParser = new FieldExpressionParser(responseFieldManager, entityRepository, responseEntityTypeRepository);
            Variable = fieldExpressionParser.ParseUserNumericExpressionOrNull("1");
            ParsedVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull("1");

            var substituteFieldExpressionParser = Substitute.For<IFieldExpressionParser>();
            substituteFieldExpressionParser.GetDeclaredVariableOrNull(Arg.Any<string>()).Returns(Variable);
            substituteFieldExpressionParser.GetDeclaredVariableOrNull(Arg.Any<ResponseFieldDescriptor>()).Returns(Variable);
            substituteFieldExpressionParser.ParseUserNumericExpressionOrNull(Arg.Any<string>()).Returns(ParsedVariable);


            responseFieldManager.Get(default).ReturnsForAnyArgs(new ResponseFieldDescriptor("Field", []));
            _variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            var subsetRepository = Substitute.For<ISubsetRepository>();
            _variableFactory = Substitute.For<IVariableFactory>();
            _variableFactory.GetDeclaredVariable(Arg.Any<VariableConfiguration>()).Returns(Variable);
            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            CreateMetricConfigurationSubstitute();
            _metricFactory = new MetricFactory(responseFieldManager, substituteFieldExpressionParser, subsetRepository,
                _variableConfigurationRepository, _variableFactory, baseExpressionGenerator);
        }

        /// <summary>
        /// Test that when loading a measure from a metric configuration, the measure's properties a reset.
        /// </summary>
        /// <param name="fixture">AutoFixture can create test instances for you, like the IoC container in production</param>
        [Test, AutoData]
        public void ExistingConfigurationIsOverwritten(IFixture fixture)
        {
            var metricConfiguration = CreateCheckboxMetricConfiguration(1);
            SetupAutoFixture(fixture);
            var loadedFresh = new Measure();
            // Create a measure with all properties set (even ones we didn't know about when we wrote this test)
            var loadedWithPriorConfig = fixture.Build<Measure>().Create();
            _metricFactory.LoadMetric(metricConfiguration, loadedFresh);
            _metricFactory.LoadMetric(metricConfiguration, loadedWithPriorConfig);
            Assert.That(JsonConvert.SerializeObject(loadedWithPriorConfig), Is.EqualTo(JsonConvert.SerializeObject(loadedFresh)));
        }

        private static void SetupAutoFixture(IFixture fixture)
        {
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            fixture.Customize(new AutoNSubstituteCustomization());
        }

        [TestCase("YnMetricBase")]
        [TestCase("YnMetricNoBase")]
        public async Task MeasureFactoryWillSetBaseDisplayInfo(string measureName)
        {
            var measure = _metricFactory.CreateMetric(_metricConfigurationRepository.Get(measureName));
            Assert.That(measure.BaseDisplayInfo, Is.Not.Null);
            await Verifier.Verify(measure.BaseDisplayInfo);
        }

        [Test]
        public void CheckboxMetricWithVariableAndTrueValsShouldReturnFieldExpressionAndNoTrueVals()
        {
            int variableId = 1;
            var mockVariable = new VariableConfiguration()
            {
                Id = variableId,
                ProductShortCode = "survey",
                SubProductId = "",
                Identifier = "checkbox",
                DisplayName = "checkbox",
                Definition = Substitute.For<QuestionVariableDefinition>(),
            };
            _variableConfigurationRepository.Get(variableId).Returns(mockVariable);

            var mockConfiguration = CreateCheckboxMetricConfiguration(variableId);

            var measure = _metricFactory.CreateMetric(mockConfiguration);
            Assert.That(measure.LegacyPrimaryTrueValues.Values, Is.Null);
            Assert.That(measure.PrimaryVariable, Is.TypeOf<FilteredVariable>());
        }

        private static MetricConfiguration CreateCheckboxMetricConfiguration(int variableId)
        {
            return new MetricConfiguration()
            {
                Name = "checkbox",
                VarCode = "checkbox",
                CalcType = "yn",
                ProductShortCode = "survey",
                Field = "checkbox",
                BaseField = "checkbox_asked",
                TrueVals = "1",
                BaseVals = "-99|1",
                FilterValueMapping = "-99:No|1:Yes",
                VariableConfigurationId = variableId,
                IsAutoGenerated = AutoGenerationType.CreatedFromField
            };
        }

        [Test]
        public void MetricWithNoTrueValsShouldReturnVariableAndNoTrueVals()
        {
            int variableId = 1;
            var mockVariable = new VariableConfiguration()
            {
                Id = variableId,
                ProductShortCode = "survey",
                SubProductId = "",
                Identifier = "single",
                DisplayName = "single",
                Definition = Substitute.For<QuestionVariableDefinition>(),
            };
            _variableConfigurationRepository.Get(variableId).Returns(mockVariable);

            var mockConfiguration = new MetricConfiguration()
            {
                Name = "single",
                VarCode = "single",
                CalcType = "yn",
                ProductShortCode = "survey",
                Field = "single",
                BaseField = "single_asked",
                VariableConfigurationId = variableId,
                IsAutoGenerated = AutoGenerationType.CreatedFromField
            };

            var measure = _metricFactory.CreateMetric(mockConfiguration);
            Assert.That(measure.LegacyPrimaryTrueValues.Values, Is.Null);
            Assert.That(measure.PrimaryVariable, Is.EqualTo(Variable));
        }

        [Test]
        public void MetricWithFieldExpressionWithNoTrueValsShouldLoadFieldExpression()
        {
            var fieldExpression = "max(response._type1Field())";
            var mockConfiguration = new MetricConfiguration()
            {
                Name = "single",
                VarCode = "single",
                CalcType = "yn",
                ProductShortCode = "survey",
                FieldExpression = fieldExpression,
            };

            var measure = _metricFactory.CreateMetric(mockConfiguration);
            Assert.That(measure.PrimaryVariable, Is.EqualTo(ParsedVariable));
        }

        [Test]
        public void MetricWithFieldExpressionWithTrueValsShouldLoadFieldExpressionAndIgnoresTrueVals()
        {
            var fieldExpression = "max(response._type1Field())";
            var mockConfiguration = new MetricConfiguration()
            {
                Name = "single",
                VarCode = "single",
                CalcType = "yn",
                ProductShortCode = "survey",
                FieldExpression = fieldExpression,
                TrueVals = "1",
                VariableConfigurationId = null,
            };

            var measure = _metricFactory.CreateMetric(mockConfiguration);
            Assert.That(measure.LegacyPrimaryTrueValues.Values, Is.Null);
            Assert.That(measure.PrimaryVariable, Is.TypeOf<FilteredVariable>());
        }

        [Test]
        public void MetricWithFieldAndTrueValsAndFVMShouldLoadTrueVals()
        {
            var mockConfiguration = new MetricConfiguration()
            {
                Name = "checkbox",
                VarCode = "checkbox",
                CalcType = "yn",
                ProductShortCode = "survey",
                Field = "checkbox",
                BaseField = "checkbox_asked",
                TrueVals = "1",
                BaseVals = "-99|1",
                FilterValueMapping = "-99:No|1:Yes",
                VariableConfigurationId = null,
            };

            var measure = _metricFactory.CreateMetric(mockConfiguration);
            Assert.That(measure.LegacyPrimaryTrueValues.Values, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void MetricWithFieldAndTrueValsAndNoFVMShouldNotLoadTrueVals()
        {
            var mockConfiguration = new MetricConfiguration()
            {
                Name = "checkbox",
                VarCode = "checkbox",
                CalcType = "yn",
                ProductShortCode = "survey",
                Field = "checkbox",
                BaseField = "checkbox_asked",
                TrueVals = "1",
                BaseVals = "-99|1",
                VariableConfigurationId = null,
            };

            var measure = _metricFactory.CreateMetric(mockConfiguration);
            Assert.That(measure.LegacyPrimaryTrueValues.Values, Is.Null);
            Assert.That(measure.PrimaryVariable, Is.TypeOf<FilteredVariable>());
        }

        [Test]
        public void MeasureForSurveyIdWaveShouldHaveTrueIsSurveyIdMeasureProperty()
        {
            int variableId = 23;
            string identifier = "surveyWave";

            var groupedVariableDefinition = new GroupedVariableDefinition()
            {
                ToEntityTypeName = identifier,
                ToEntityTypeDisplayNamePlural = identifier,
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceId = 1,
                        ToEntityInstanceName = "FirstSurveysGroup",
                        Component = new SurveyIdVariableComponent()
                        {
                            SurveyIds = [1],
                        },
                    },
                    new()
                    {
                        ToEntityInstanceId = 2,
                        ToEntityInstanceName = "SecondSurveysGroup",
                        Component = new SurveyIdVariableComponent()
                        {
                            SurveyIds = [2],
                        },
                    }
                }
            };

            var mockVariable = new VariableConfiguration()
            {
                Id = variableId,
                ProductShortCode = "survey",
                SubProductId = "survey",
                Identifier = identifier,
                DisplayName = identifier,
                Definition = groupedVariableDefinition,
            };
            var expectedVariable = new SurveyIdVariable(groupedVariableDefinition);
            _variableConfigurationRepository.Get(variableId).Returns(mockVariable);
            _variableFactory.GetDeclaredVariable(Arg.Any<VariableConfiguration>()).Returns(expectedVariable);

            var mockConfiguration = new MetricConfiguration()
            {
                Name = identifier,
                VarCode = identifier,
                CalcType = "yn",
                ProductShortCode = "survey",
                SubProductId = "survey",
                Field = identifier,
                BaseField = "surveyWave_asked",
                TrueVals = "1>7",
                BaseVals = "1>7",
                FilterValueMapping = "-99:No|1:Yes",
                VariableConfigurationId = variableId,
                IsAutoGenerated = AutoGenerationType.Original
            };

            _variableFactory.GetDeclaredVariable(Arg.Any<VariableConfiguration>()).Returns(expectedVariable);
            var measure = _metricFactory.CreateMetric(mockConfiguration);

            //we only want to return expected variable for this test so reset it
            _variableFactory.GetDeclaredVariable(Arg.Any<VariableConfiguration>()).Returns(Variable);
            Assert.That(measure.IsSurveyIdMeasure, Is.True);
        }

        private void CreateMetricConfigurationSubstitute()
        {
            var metricConfigs = CreateTestMetricConfigs();
            _metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            _metricConfigurationRepository.Get(Arg.Any<string>()).Returns(args => metricConfigs.First(x => x.Name == args.Arg<string>()));
        }

        private IEnumerable<MetricConfiguration> CreateTestMetricConfigs()
        {
            return new List<MetricConfiguration>()
            {
                new() {
                    Name = "YnMetricBase", VarCode = "Yn", CalcType = "yn", ProductShortCode = "survey", Field = "Field",
                    BaseField = "Field", SubProductId = ""
                },
                new() {
                    Name = "YnMetricNoBase", VarCode = "Yn",  CalcType = "yn", ProductShortCode = "survey", BaseField = "Field",
                    SubProductId = ""
                },
            };
        }
    }
}