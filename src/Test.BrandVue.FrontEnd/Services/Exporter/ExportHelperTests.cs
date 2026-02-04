using NUnit.Framework;
using BrandVue.Services.Exporter;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Measures;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.Variable;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Subsets;

namespace Test.BrandVue.FrontEnd.Services.Exporter
{
    [TestFixture]
    public class ExportHelperTests
    {
        private Measure _singleChoiceMeasure;
        private VariableConfiguration _singleChoiceVariable;
        const string singleChoiceMeasureAndFieldName = "SingleChoice";

        private Measure _multiChoiceMeasure;
        private VariableConfiguration _multiChoiceVariable;
        const string multiChoiceMeasureAndFieldName = "MultiChoice";

        private Measure _userCreatedSingleChoiceMeasure;
        private VariableConfiguration _userCreatedSingleChoiceVariable;
        const string userCreatedSingleChoiceMeasureAndFieldName = "UserCreatedSingleChoice";

        private Measure _userCreatedMultiChoiceMeasure;
        private VariableConfiguration _userCreatedMultiChoiceVariable;
        const string userCreatedMultiChoiceMeasureAndFieldName = "UserCreatedMultiChoice";

        private IEnumerable<Measure> _measures;
        private IEnumerable<VariableConfiguration> _variables;

        private Dictionary<string, MainQuestionType> _questionTypeLookup;
        private AverageType[] _averageTypes;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private IExportAverageHelper _exportHelper;
        private Subset _subset;

        [SetUp]
        public void SetUp()
        {
            _subset = new Subset();
            GenerateMeasuresAndVariables();

            _averageTypes = new AverageType[]
            {
                AverageType.Mean,
                AverageType.Median,
            };

            _measures = new List<Measure>
            {
                _singleChoiceMeasure,
                _multiChoiceMeasure,
                _userCreatedSingleChoiceMeasure,
                _userCreatedMultiChoiceMeasure
            };

            _variables = new List<VariableConfiguration>
            {
                _singleChoiceVariable,
                _multiChoiceVariable,
                _userCreatedSingleChoiceVariable,
                _userCreatedMultiChoiceVariable
            };

            _questionTypeLookup = new Dictionary<string, MainQuestionType>
            {
                { singleChoiceMeasureAndFieldName, MainQuestionType.SingleChoice },
                { multiChoiceMeasureAndFieldName, MainQuestionType.MultipleChoice },
                { userCreatedSingleChoiceMeasureAndFieldName, MainQuestionType.CustomVariable },
                { userCreatedMultiChoiceMeasureAndFieldName, MainQuestionType.CustomVariable }
            };

            var questionTypeLookupRepository = Substitute.For<IQuestionTypeLookupRepository>();
            questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(_questionTypeLookup);

            _variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            _variableConfigurationRepository.Get(Arg.Any<int>()).Returns(x =>
            {
                int id = x.Arg<int>();
                return _variables.Single(v => v.Id == id);
            });

            var measureRepository = Substitute.For<IMeasureRepository>();
            measureRepository.GetAll().Returns(_measures);

            _exportHelper = new ExportAverageHelper(measureRepository,
                _variableConfigurationRepository,
                questionTypeLookupRepository);
        }

        private void GenerateMeasuresAndVariables()
        {
            _singleChoiceMeasure = new Measure()
            {
                Name = singleChoiceMeasureAndFieldName,
                VariableConfigurationId = 1,
                VarCode = singleChoiceMeasureAndFieldName,
                GenerationType = AutoGenerationType.CreatedFromField,
                PrimaryVariableIdentifier = singleChoiceMeasureAndFieldName
            };

            _singleChoiceVariable = new VariableConfiguration()
            {
                Id = _singleChoiceMeasure.VariableConfigurationId.Value,
                Identifier = singleChoiceMeasureAndFieldName,
                Definition = new GroupedVariableDefinition()
            };

            _multiChoiceMeasure = new Measure()
            {
                Name = multiChoiceMeasureAndFieldName,
                VariableConfigurationId = 2,
                VarCode = multiChoiceMeasureAndFieldName,
                GenerationType = AutoGenerationType.CreatedFromField,
                PrimaryVariableIdentifier = multiChoiceMeasureAndFieldName
            };

            _multiChoiceVariable = new VariableConfiguration()
            {
                Id = _multiChoiceMeasure.VariableConfigurationId.Value,
                Identifier = multiChoiceMeasureAndFieldName,
                Definition = new GroupedVariableDefinition()
            };

            _userCreatedSingleChoiceMeasure = new Measure()
            {
                Name = userCreatedSingleChoiceMeasureAndFieldName,
                VariableConfigurationId = 3,
                VarCode = userCreatedSingleChoiceMeasureAndFieldName,
                GenerationType = AutoGenerationType.Original
            };

            _userCreatedSingleChoiceVariable = new VariableConfiguration()
            {
                Id = _userCreatedSingleChoiceMeasure.VariableConfigurationId.Value,
                Identifier = userCreatedSingleChoiceMeasureAndFieldName,
                Definition = new GroupedVariableDefinition()
                {
                    ToEntityTypeName = userCreatedSingleChoiceMeasureAndFieldName,
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "1",
                            ToEntityInstanceId = 1,
                            Component = new InstanceListVariableComponent()
                            {
                                InstanceIds = new List<int> {1, 4},
                                FromVariableIdentifier = singleChoiceMeasureAndFieldName,
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "2",
                            ToEntityInstanceId = 2,
                            Component = new InstanceListVariableComponent()
                            {
                                InstanceIds = new List<int> {2, 3},
                                FromVariableIdentifier = singleChoiceMeasureAndFieldName,
                            }
                        }
                    }
                }
            };

            _userCreatedMultiChoiceMeasure = new Measure()
            {
                Name = userCreatedMultiChoiceMeasureAndFieldName,
                VariableConfigurationId = 4,
                VarCode = userCreatedMultiChoiceMeasureAndFieldName,
                GenerationType = AutoGenerationType.Original,
                PrimaryVariableIdentifier = userCreatedMultiChoiceMeasureAndFieldName
            };

            _userCreatedMultiChoiceVariable = new VariableConfiguration()
            {
                Id = _userCreatedMultiChoiceMeasure.VariableConfigurationId.Value,
                Identifier = userCreatedMultiChoiceMeasureAndFieldName,
                Definition = new GroupedVariableDefinition()
                {
                    ToEntityTypeName = userCreatedMultiChoiceMeasureAndFieldName,
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "1",
                            ToEntityInstanceId = 1,
                            Component = new InstanceListVariableComponent()
                            {
                                InstanceIds = new List<int> {1, 4},
                                FromVariableIdentifier = userCreatedMultiChoiceMeasureAndFieldName,
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "2",
                            ToEntityInstanceId = 2,
                            Component = new InstanceListVariableComponent()
                            {
                                InstanceIds = new List<int> {2, 3},
                                FromVariableIdentifier = userCreatedMultiChoiceMeasureAndFieldName,
                            }
                        }
                    }
                }
            };
        }

        [Test]
        public void FieldGeneratedSingleChoiceShouldReturnEntityIdMean()
        {
            var validatedAverageTypes = _exportHelper.VerifyAverageTypesForMeasure(_singleChoiceMeasure,
                _averageTypes,
                _subset);

            Assert.That(validatedAverageTypes.Contains(AverageType.Median));
            Assert.That(validatedAverageTypes.Contains(AverageType.EntityIdMean));
        }

        [Test]
        public void FieldGeneratedMultiChoiceShouldReturnResultMean()
        {
            var validatedAverageTypes = _exportHelper.VerifyAverageTypesForMeasure(_multiChoiceMeasure,
                _averageTypes,
                _subset);

            Assert.That(validatedAverageTypes.Contains(AverageType.Median));
            //We treat result mean and mean the same way so either is fine
            Assert.That(validatedAverageTypes.Contains(AverageType.Mean));
        }

        [Test]
        public void UserGeneratedBasedOnSingleChoiceShouldReturnEntityIdMean()
        {
            var validatedAverageTypes = _exportHelper.VerifyAverageTypesForMeasure(_userCreatedSingleChoiceMeasure,
                _averageTypes,
                _subset);

            Assert.That(validatedAverageTypes.Contains(AverageType.Median));
            Assert.That(validatedAverageTypes.Contains(AverageType.EntityIdMean));
        }

        [Test]
        public void UserGeneratedBasedOnMultiChoiceShouldReturnResultMean()
        {
            var validatedAverageTypes = _exportHelper.VerifyAverageTypesForMeasure(_userCreatedMultiChoiceMeasure,
                _averageTypes,
                _subset);

            Assert.That(validatedAverageTypes.Contains(AverageType.Median));
            //We treat result mean and mean the same way so either is fine
            Assert.That(validatedAverageTypes.Contains(AverageType.Mean));
        }
    }
}
