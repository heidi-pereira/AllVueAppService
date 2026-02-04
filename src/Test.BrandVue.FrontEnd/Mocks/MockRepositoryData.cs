using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.Models;
using BrandVue.PublicApi.Definitions;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Newtonsoft.Json.Linq;
using NSubstitute;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestCommon.Weighting;
using Vue.AuthMiddleware;
using TestAnswer = TestCommon.DataPopulation.TestAnswer;
using AverageDescriptor = BrandVue.SourceData.Averages.AverageDescriptor;
using ResponseFieldDescriptor = BrandVue.SourceData.Respondents.ResponseFieldDescriptor;

namespace Test.BrandVue.FrontEnd.Mocks
{
    /// <summary>
    /// It's OK for these to make use of ExpectedOutputs, but shouldn't convert from SourceData -> API type, only the other way around.
    /// This ensures we don't break a conversion used in production and have the tests still pass.
    /// </summary>
    public static class MockRepositoryData
    {
        public static readonly List<Subset> AllowedSubsetList = GetAllowedSubsets().ToList();
        public static readonly List<Subset> FullSubsetList = CreateSampleSubsets().ToList();

        public static readonly Subset UkSubset = AllowedSubsetList[0];
        public static readonly Subset USSubset = AllowedSubsetList[1];

        public static IEnumerable<Subset> CreateSampleSubsets()
        {
            return GetAllowedSubsets().Concat(new List<Subset>
            {
                new Subset
                {
                    Id = "UnavailableSubset",
                    DisplayName = "UnavailableSubset",
                    Iso2LetterCountryCode = "gb",
                    Alias = "UNSB",
                    EnableRawDataApiAccess = true,
                    ProductId = 99
                }
            });
        }

        public static List<UiWeightingConfigurationRoot> GetAllowedWeightPlans()
        {
            return new List<UiWeightingConfigurationRoot>();
        }
        public static UiWeightingConfigurationRoot GetEmptyWeightPlan(string subset)
        {
            return new UiWeightingConfigurationRoot(subset);
        }
        public static IEnumerable<Subset> GetAllowedSubsets()
        {
            return new List<Subset>
            {
                new Subset
                {
                    Id = "UKSubset",
                    DisplayName = "UKSubset",
                    Iso2LetterCountryCode = "gb",
                    Alias = "UK",
                    EnableRawDataApiAccess = true,
                    ProductId = 1
                },
                new Subset
                {
                    Id = "USSubset",
                    DisplayName = "USSubset",
                    Iso2LetterCountryCode = "gb",
                    Alias = "US",
                    EnableRawDataApiAccess = true,
                    ProductId = 2
                }
            };
        }

        public static IEnumerable<EntityInstance> CreateBrands()
        {
            return ClassInstanceDescriptors()
                .Select(cid => new EntityInstance { Id = cid.ClassInstanceId, Name = cid.Name }).ToList();
        }

        public static IReadOnlyCollection<EntityInstance> CreateProducts()
        {
            return ProductClassInstanceDescriptors()
                .Select(cid => new EntityInstance { Id = cid.ClassInstanceId, Name = cid.Name }).ToList();
        }


        public static IEnumerable<Measure> CreateSampleMeasures()
        {
            var rfm = new ResponseFieldManager(EntityTypeRepository.GetDefaultEntityTypeRepository());
            var tvPlatformEntityType = new EntityType("tvplatform", "tvplatform", "tvplatform");
            return new List<Measure>
            {
                new Measure
                {
                    UrlSafeName = "net-buzz",
                    Name = "Net Buzz",
                    Field = AddDataAccessModelForSubset(rfm.Add("Positive_Buzz", UkSubset, TestEntityTypeRepository.Brand), UkSubset),
                    StartDate = new DateTime(2017, 6, 30, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Consumer_Segment", UkSubset, TestEntityTypeRepository.Brand), UkSubset),
                    LegacyPrimaryTrueValues = { Values =  new [] { 1 } },
                    LegacyBaseValues = { Values = new [] { 1, 2, 3, 4} },
                    VarCode = "Net Buzz"
                },
                new Measure
                {
                    UrlSafeName = "buzz-noise",
                    Name = "Buzz Noise",
                    Field = AddDataAccessModelForSubset(rfm.Add("Positive_Buzz", UkSubset, TestEntityTypeRepository.Brand), UkSubset),
                    StartDate = new DateTime(2017, 6, 30, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Consumer_Segment", UkSubset, TestEntityTypeRepository.Brand), UkSubset),
                    LegacyPrimaryTrueValues = { Values =  new [] { 1 } },
                    LegacyBaseValues = { Values = new int[] { } },
                    VarCode = "Buzz Noise"
                },
                new Measure
                {
                    UrlSafeName = "gender",
                    Name = "Gender",
                    Field = AddDataAccessModelForSubset(rfm.Add("Gender", UkSubset), UkSubset),
                    StartDate =  new DateTime(2000, 1, 1, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Gender", UkSubset), UkSubset),
                    VarCode = "Gender"
                },
                new Measure
                {
                    UrlSafeName = "age",
                    Name = "Age",
                    Field = AddDataAccessModelForSubset(rfm.Add("Age", UkSubset), UkSubset),
                    StartDate = new DateTime(2000, 1, 1, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Age", UkSubset), UkSubset),
                    LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 16, Maximum = 74 },
                    LegacyBaseValues =
                    {
                        Minimum = 16,
                        Maximum = 74,
                    },
                    VarCode = "Age"
                },
                new Measure
                {
                    UrlSafeName = "products-used-insurance-breakdown-cover",
                    Name = "Products Used: Insurance - Breakdown Cover",
                    Field = AddDataAccessModelForSubset(rfm.Add("BreakdownCover", UkSubset), UkSubset),
                    StartDate = new DateTime(2000, 1, 1, 0, 0, 0),
                    BaseField = null,
                    LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 16, Maximum = 74 },
                    VarCode = "Products Used: Insurance - Breakdown Cover"
                },
                new Measure
                {
                    UrlSafeName = "brand-awareness-other",
                    Name = "Brand Awareness Other",
                    Field = AddDataAccessModelForSubset(rfm.Add("Brand_Awareness_Other", UkSubset, TestEntityTypeRepository.Product), UkSubset),
                    StartDate = new DateTime(2015, 1, 1, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Brand_Awareness_Other", UkSubset, TestEntityTypeRepository.Product), UkSubset),
                    LegacyPrimaryTrueValues = { Values =  new [] { 1 } },
                    LegacyBaseValues = { Values = new [] { 1, 2, 3 } },
                    VarCode = "Brand Awareness Other"
                },
                new Measure
                {
                    UrlSafeName = "brand-product-other",
                    Name = "Brand Product Other",
                    Field = AddDataAccessModelForSubset(rfm.Add("Brand_Product_Other", TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand), UkSubset),
                    StartDate = new DateTime(2015, 1, 1, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Brand_Product_Other", TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand), UkSubset),
                    LegacyPrimaryTrueValues = { Values =  new [] { 1 } },
                    LegacyBaseValues = { Values = new [] { 1, 2, 3 } },
                    VarCode = "Brand Product Other"
                },
                new Measure
                {
                    UrlSafeName = "customer-tv-platforms",
                    Name = "Customer TV platforms",
                    Field = AddDataAccessModelForSubset(rfm.Add("TV_platforms", UkSubset, TestEntityTypeRepository.Brand, tvPlatformEntityType), UkSubset),
                    StartDate = new DateTime(2015, 1, 1, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("Consumer_Segment", UkSubset, TestEntityTypeRepository.Brand), UkSubset),
                    LegacyPrimaryTrueValues = { Values =  new [] { 1 } },
                    LegacyBaseValues = { Values = new [] { 4, 5, 6 } },
                    VarCode = "Customer TV platforms"
                }
            };
        }

        public static IEnumerable<Measure> VariableTestingMeasures(Variable<int?> variable)
        {
            var rfm = new ResponseFieldManager(EntityTypeRepository.GetDefaultEntityTypeRepository());
            return new List<Measure>() {
                new Measure
                {
                    UrlSafeName = "genericQuestion",
                    Name = "genericQuestion",
                    Field = AddDataAccessModelForSubset(rfm.Add("genericQuestion", UkSubset, TestEntityTypeRepository.GenericQuestion), UkSubset, "", null, false),
                    StartDate = new DateTime(2015, 1, 1, 0, 0, 0),
                    BaseField = AddDataAccessModelForSubset(rfm.Add("genericQuestion", UkSubset, TestEntityTypeRepository.GenericQuestion), UkSubset, "", null, false),
                    LegacyPrimaryTrueValues = { Values = new[] { 1, 2, 3, 4, 5 } },
                    LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5 } },
                },
                new Measure
                {
                    UrlSafeName = "nettingVariable",
                    Name = "nettingVariable",
                    BaseExpressionString = "((len(response.region()) or len(response.city())))",
                    PrimaryVariable = variable,
                    HasCustomFieldExpression = true,
                    FilterValueMapping = "1:north|2:east|3:south|4:west|5:middle",
                    LegacyBaseValues =
                    {
                        Minimum = 1,
                        Maximum = 5,
                    },
                    NumberFormatString = "0%",
                    NumberFormat = "0%",
                    VariableConfigurationId = 42,
                }};
        }

        public static ResponseFieldDescriptor AddDataAccessModelForSubset(ResponseFieldDescriptor responseFieldDescriptor, Subset subset, string questionText = "", ChoiceSet choiceSet = null, bool isText = false)
        {
            responseFieldDescriptor.AddDataAccessModelForSubset(subset.Id, CreateFieldDefinitionModel(questionText, choiceSet, isText));
            return responseFieldDescriptor;
        }
        private static ChoiceSet CreateChoiceSet(IList<Choice> choices) =>
            new() { Choices = choices };

        private static FieldDefinitionModel CreateFieldDefinitionModel(string questionText, ChoiceSet questionChoiceSet, bool isText)
            {
            return new("", "", "", DbLocation.QuestionEntity.UnquotedColumnName, "", 1f, "", EntityInstanceColumnLocation.Unknown, "", isText, null, Enumerable.Empty<EntityFieldDefinitionModel>(), null)
            {
                QuestionModel = new Question
                {
                    QuestionText = questionText,
                    QuestionChoiceSet = questionChoiceSet
                }
            };
        }

        public static readonly List<ClassDescriptor> ClassDescriptorList = CreateSampleClassDescriptions().ToList();
        public static ClassDescriptor ProductClassDescriptor => ClassDescriptorList[1];
        public static ClassDescriptor BrandClassDescriptor => ClassDescriptorList[0];
        public static ClassDescriptor ProfileClassDescriptor => new ClassDescriptor(TestEntityTypeRepository.Profile, Array.Empty<string>());

        public static IEnumerable<ClassDescriptor> CreateSampleClassDescriptions()
        {
            return new List<ClassDescriptor>
            {
                new ClassDescriptor(TestEntityTypeRepository.Brand, Array.Empty<string>()),
                new ClassDescriptor(TestEntityTypeRepository.Product, Array.Empty<string>())
            };
        }

        public static List<AverageDescriptor> MockAverageRepositorySource() =>
            new List<AverageDescriptor>
            {
                new AverageDescriptor
                {
                    AverageId = "14Days",
                    DisplayName = "14 days",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    MakeUpTo = MakeUpTo.Day,
                    NumberOfPeriodsInAverage = 14
                },
                new AverageDescriptor
                {
                    AverageId = "DisabledAverage",
                    DisplayName = "Disabled Average",
                    Disabled = true,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    MakeUpTo = MakeUpTo.Day,
                    NumberOfPeriodsInAverage = 1
                },
                new AverageDescriptor
                {
                    AverageId = "28Days",
                    DisplayName = "28 days",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    MakeUpTo = MakeUpTo.Day,
                    NumberOfPeriodsInAverage = 28
                },
                new AverageDescriptor
                {
                    AverageId = "Weekly",
                    DisplayName = "Weekly",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    MakeUpTo = MakeUpTo.WeekEnd,
                    NumberOfPeriodsInAverage = 7
                },
                new AverageDescriptor
                {
                    AverageId = "Monthly",
                    DisplayName = "Monthly",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    MakeUpTo = MakeUpTo.MonthEnd,
                    NumberOfPeriodsInAverage = 1
                },
                new AverageDescriptor
                {
                    AverageId = "MonthlyOver3Months",
                    DisplayName = "Monthly (over 3 months)",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    MakeUpTo = MakeUpTo.MonthEnd,
                    NumberOfPeriodsInAverage = 3
                },
                new AverageDescriptor
                {
                    AverageId = "Quarterly",
                    DisplayName = "Quarterly",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    MakeUpTo = MakeUpTo.QuarterEnd,
                    NumberOfPeriodsInAverage = 3
                },
                new AverageDescriptor
                {
                    AverageId = "HalfYearly",
                    DisplayName = "HalfYearly",
                    Disabled = false,
                    Subset = new[] { UkSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    MakeUpTo = MakeUpTo.HalfYearEnd,
                    NumberOfPeriodsInAverage = 6
                },
                new AverageDescriptor
                {
                    AverageId = "Annual",
                    DisplayName = "Annual",
                    Disabled = false,
                    Subset = new[] { USSubset },
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    MakeUpTo = MakeUpTo.CalendarYearEnd,
                    NumberOfPeriodsInAverage = 12
                }
            };

        public static IProfileResponseAccessorFactory SubstituteDailyQuotaCellRespondentsSource(
            IProfileResponseAccessor profileResponseAccessor)
        {
            var substituteDailyQuotaCellRespondentsSource = Substitute.For<IProfileResponseAccessorFactory>();

            substituteDailyQuotaCellRespondentsSource.GetOrCreate(UkSubset).Returns(profileResponseAccessor);
            return substituteDailyQuotaCellRespondentsSource;
        }

        public static IQuotaCellReferenceWeightingRepository SubstituteQuotaCellReferenceWeightingRepository(IGroupedQuotaCells quotaCells)
        {
            var quotaCellReferenceWeightingRepository = Substitute.For<IQuotaCellReferenceWeightingRepository>();
            quotaCellReferenceWeightingRepository.Get(UkSubset).Returns(c =>
            {
                double totalCells = quotaCells.Cells.Count;
                var quotaWeights = quotaCells.Cells.ToDictionary(q => q.ToString(), q => WeightingValue.StandardWeighting((q.Id + 1) / (totalCells + 1)));
                return new QuotaCellReferenceWeightings(quotaWeights);
            });
            return quotaCellReferenceWeightingRepository;
        }

        public static IProfileResponseAccessor SubstituteProfileResponseAccessor(IResponseFieldManager responseFieldManager, IGroupedQuotaCells quotaCells)
        {
            var testResponseFactory = new TestResponseFactory(responseFieldManager);
            var profileResponseAccessorMock = Substitute.For<IProfileResponseAccessor>();
            var startDate = new DateTimeOffset(2017, 5, 1, 0, 0, 0, TimeSpan.Zero);
            var endDate = new DateTimeOffset(2019, 7, 31, 0, 0, 0, TimeSpan.Zero);
            profileResponseAccessorMock.StartDate.Returns(startDate);
            profileResponseAccessorMock.EndDate.Returns(endDate);
            profileResponseAccessorMock.GetResponses(Arg.Any<IGroupedQuotaCells>()).Returns(c => quotaCells.Cells.Select(
                q =>
                {
                    var singleProfilePerDay = startDate
                        .SpanByDayTo(endDate)
                        .Select(d =>
                        {
                            var (profileResponse, _) = testResponseFactory.CreateResponse(d, 0, Array.Empty<TestAnswer>());
                            return (IProfileResponseEntity)profileResponse;
                        });

                    return PopulatedQuotaCell.CreateIndexed(q, singleProfilePerDay.ToArray());
                }));

            return profileResponseAccessorMock;
        }

        public static IResponseFieldDescriptorLoader GetResponseFieldDescriptorLoader()
        {
            var fieldDescriptorLoader = Substitute.For<IResponseFieldDescriptorLoader>();
            fieldDescriptorLoader.GetFieldDescriptors(Arg.Any<Subset>())
                .Returns(c => ResponseFieldDescriptors(c.ArgAt<Subset>(0)));
            fieldDescriptorLoader.GetFieldDescriptors(Arg.Any<Subset>(), Arg.Any<IEnumerable<EntityType>>())
                .Returns(c => GetFieldDescriptors(c.ArgAt<Subset>(0), c.ArgAt<IEnumerable<EntityType>>(1)));
            return fieldDescriptorLoader;
        }

        public static IResponseFieldManager GetResponseFieldManager(Subset subset, IEnumerable<ResponseFieldDescriptor> additionalFields = null)
        {
            var responseFieldManager = Substitute.For<IResponseFieldManager>();
            responseFieldManager
                .GetOrAddFieldsForEntityType(Arg.Any<IEnumerable<EntityType>>(), Arg.Any<string>())
                .Returns(args => GetFieldDescriptors(subset, args.Arg<IEnumerable<EntityType>>(), additionalFields));
            return responseFieldManager;
        }

        private static List<ResponseFieldDescriptor> GetFieldDescriptors(Subset subset, IEnumerable<EntityType> responseEntityTypes, IEnumerable<ResponseFieldDescriptor> additionalFields = null)
        {
            return ResponseFieldDescriptors(subset, additionalFields).Where(m => m.EntityCombination.IsEquivalent(responseEntityTypes)).ToList();
        }

        public static IReadOnlyCollection<ClassInstanceDescriptor> ClassInstanceDescriptors()
        {
            return new List<ClassInstanceDescriptor>
            {
                new ClassInstanceDescriptor(1 , "Test Company 1"),
                new ClassInstanceDescriptor(2 , "Test Company 2"),
                new ClassInstanceDescriptor(3 , "Test Company 3"),
            };
        }

        public static IReadOnlyCollection<ClassInstanceDescriptor> ProductClassInstanceDescriptors()
        {
            return new List<ClassInstanceDescriptor>
            {
                new ClassInstanceDescriptor(1 , "Product 1"),
                new ClassInstanceDescriptor(2 , "Product 2"),
            };
        }

        public static IQuotaCellDescriptionProvider QuotaCellDescriptionProvider(IGroupedQuotaCells quotaCells)
        {
            var quotaCellDescriptionProvider = Substitute.For<IQuotaCellDescriptionProvider>();
            foreach (var quotaCell in quotaCells.Cells)
            {
                quotaCellDescriptionProvider.GetIdentifiersToKeyPartDescriptions(quotaCell)
                    .Returns(quotaCell.FieldGroupToKeyPart.ToDictionary( x=> MapFileQuotaCellDescriptionProvider.FieldToHumanName[x.Key], x=>x.Value));
            }
            return quotaCellDescriptionProvider;
        }

        public static IReadOnlyCollection<ResponseFieldDescriptor> ResponseFieldDescriptors(Subset subset, IEnumerable<ResponseFieldDescriptor> additionalFields = null)
        {
            var rfm = new ResponseFieldManager(EntityTypeRepository.GetDefaultEntityTypeRepository());
            var choice1 = new Choice { SurveyChoiceId = 1, Name = "Category 1" };
            var choice2 = new Choice { SurveyChoiceId = 2, Name = "Category 2" };
            var choice3 = new Choice { SurveyChoiceId = 3, Name = "Category 3" };

            var twoChoices = new List<Choice> { choice1, choice2 };
            var threeChoices = twoChoices.Concat(choice3.Yield()).ToList();

            var fieldMetaData = new List<(string FieldName, string QuestionName, bool IsText, ChoiceSet ChoiceSet)>
            {
                ("Category_Type_Question", "Choose from one of 2 categories?", false, CreateChoiceSet(twoChoices)),
                ("Category_Type_Question_But_With_Value_Type_Specified", "Choose from one of three categories?", false, CreateChoiceSet(threeChoices)),
                ("Text_Type_Question", "What is your first name?", true, null),
                ("OpenText_Type_Question", "What is your opentext first name?", true, null),
                ("Unknown_Type_Question", "What is the answer to life, the universe and everything?", false, null),
                ("Value_Type_Question", "What is the value of something?", false, null)
            };

            if (subset.Equals(USSubset)) return new List<ResponseFieldDescriptor>();

            var responseFieldDescriptors = new List<ResponseFieldDescriptor>();
            responseFieldDescriptors.AddRange(fieldMetaData.Select(meta => AddDataAccessModelForSubset(rfm.Add($"{EntityType.Profile}_{meta.FieldName}", UkSubset), subset, meta.QuestionName, meta.ChoiceSet, meta.IsText)));
            responseFieldDescriptors.AddRange(fieldMetaData.Select(meta => AddDataAccessModelForSubset(rfm.Add($"{EntityType.Brand}_{meta.FieldName}", UkSubset, TestEntityTypeRepository.Brand), subset, meta.QuestionName, meta.ChoiceSet, meta.IsText)));
            responseFieldDescriptors.AddRange(fieldMetaData.Select(meta => AddDataAccessModelForSubset(rfm.Add($"{EntityType.Product}_{meta.FieldName}", UkSubset, TestEntityTypeRepository.Product), subset, meta.QuestionName, meta.ChoiceSet, meta.IsText)));
            responseFieldDescriptors.AddRange(fieldMetaData.Select(meta => AddDataAccessModelForSubset(rfm.Add($"{EntityType.Product}_{EntityType.Brand}_{meta.FieldName}", UkSubset, TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand), subset, meta.QuestionName, meta.ChoiceSet, meta.IsText)));
            if (additionalFields != null) {
                foreach (var field in additionalFields) {
                    responseFieldDescriptors.Add(AddDataAccessModelForSubset(field, subset, "", null, false));
                }
            }
            return responseFieldDescriptors;
        }

        public static IBrandVueDataLoader SubstituteBrandVueDataLoader(Subset subset = null, int surveyId = -1)
        {
            subset = subset ?? UkSubset;

            var respondentRepositoryDict = GetRespondentRepositoryValues(subset, surveyId);

            var substituteBrandVueDataLoader = Substitute.For<IBrandVueDataLoader, ISubProductSecurityRestrictionsProvider>();
            substituteBrandVueDataLoader.RespondentRepositorySource.GetForSubset(Arg.Any<Subset>())
                .Returns(c => respondentRepositoryDict[c.ArgAt<Subset>(0)]);

            return substituteBrandVueDataLoader;
        }

        public static Dictionary<Subset, RespondentRepository> GetRespondentRepositoryValues(Subset subset, int surveyId)
        {
            var respondentRepositoryDict = new Dictionary<Subset, RespondentRepository>();

            var cell1 = QuotaCell.DefaultCellDefinition("L", "0", "0", "0");
            var cell2 = QuotaCell.DefaultCellDefinition("N", "0", "0", "0");
            var quotaCell1 = new QuotaCell(0, subset, cell1);
            var quotaCell2 = new QuotaCell(1, subset, cell2);

            respondentRepositoryDict[UkSubset] = new RespondentRepository(subset)
            {
                { new(1000, new DateTime(2017, 5, 1), surveyId), quotaCell1 },
                { new(1001, new DateTime(2018, 12, 31), surveyId), quotaCell1 },
                { new(1002, new DateTime(2019, 06, 15), surveyId), quotaCell1 },
                { new(1003, new DateTime(2017, 5, 1), surveyId), quotaCell2 },
                { new(1004, new DateTime(2018, 12, 31), surveyId), quotaCell2 },
                { new(1005, new DateTime(2019, 06, 15), surveyId), quotaCell2 }
            };

            respondentRepositoryDict[USSubset] = new RespondentRepository(subset)
            {
                { new(1001, new DateTime(2019, 06, 15), surveyId), quotaCell1 },
                { new(1002, new DateTime(2019, 06, 15), surveyId), quotaCell2 },
            };
            return respondentRepositoryDict;
        }

        public static EntityMetricData[] ResponseData(Subset subset, ResponseFieldDescriptor[] fields, (DateTime startTime, DateTime endDate)? timeRange, TargetInstances[] targetInstances)
        {
            var possibleEntityCombinations = targetInstances.Select(t => t.OrderedInstances.Select(i => new EntityValue(t.EntityType, i.Id))).CartesianProduct().ToList();
            if (!possibleEntityCombinations.Any()) possibleEntityCombinations.Add(Array.Empty<EntityValue>());
            var numberOfCombinations = possibleEntityCombinations.Count;
            var entityMeasureData = new List<EntityMetricData>()
            {
                new EntityMetricData()
                {
                    EntityIds = EntityIds.From(possibleEntityCombinations[0 % numberOfCombinations].ToList()),
                    ResponseId = 1000,
                    Measures = new List<(ResponseFieldDescriptor Field, int Value)>(),
                    Timestamp = new DateTime(2017, 5, 1).ToUtcDateOffset(),
                    SurveyId = 0
                },
                new EntityMetricData()
                {
                    EntityIds = EntityIds.From(possibleEntityCombinations[1 % numberOfCombinations].ToList()),
                    ResponseId = 1001,
                    Measures = new List<(ResponseFieldDescriptor Field, int Value)>(),
                    Timestamp = new DateTime(2018, 12, 31).ToUtcDateOffset(),
                    SurveyId = 0
                },
                new EntityMetricData()
                {
                    EntityIds = EntityIds.From(possibleEntityCombinations[2 % numberOfCombinations].ToList()),
                    ResponseId = 1002,
                    Measures = new List<(ResponseFieldDescriptor Field, int Value)>(),
                    Timestamp = new DateTime(2019, 06, 15).ToUtcDateOffset(),
                    SurveyId = 0
                },
                new EntityMetricData()
                {
                    EntityIds = EntityIds.From(Array.Empty<EntityValue>()),
                    ResponseId = 100000,
                    Measures = new List<(ResponseFieldDescriptor Field, int Value)>(),
                    Timestamp = new DateTime(2019, 06, 15).ToUtcDateOffset(),
                    SurveyId = 0
                }
            };

            return entityMeasureData.Where(m => m.Timestamp == timeRange?.startTime.ToUtcDateOffset()).ToArray();
        }


        public static ILazyDataLoader SubstituteLazyDataLoader()
        {
            var substituteLazyDataLoader = Substitute.For<ILazyDataLoader>();
            substituteLazyDataLoader.GetDataForFields(Arg.Any<Subset>(), Arg.Any<IReadOnlyCollection<ResponseFieldDescriptor>>(), Arg.Any<(DateTime startTime, DateTime endDate)>(), Arg.Any<TargetInstances[]>(), Arg.Any<CancellationToken>())
                .Returns(callinfo => ResponseData(
                    callinfo.ArgAt<Subset>(0),
                    callinfo.ArgAt<ResponseFieldDescriptor[]>(1),
                callinfo.ArgAt<(DateTime, DateTime)>(2),
                    callinfo.ArgAt<TargetInstances[]>(3)));

            return substituteLazyDataLoader;
        }

        public static void MockCalculationProxyForParameters(
            this IMetricResultCalculationProxy substituteCalculationProxy,
            string startDate, string endDate, string averageId, int expectedDailyResults)
        {
            var startDateTime = new DateTimeOffset(DateTime.Parse(startDate), TimeSpan.Zero);
            var endDateTime = new DateTimeOffset(DateTime.Parse(endDate), TimeSpan.Zero);

            var entityWeightedDailyResults = new[]
            {
                new EntityWeightedDailyResults(new EntityInstance {Id = 1}, Enumerable.Repeat(new WeightedDailyResult(endDateTime), expectedDailyResults).ToList())
            };

            var metricCalculationResults = new[]
            {
                new MetricCalculationResult(new TargetInstances(TestEntityTypeRepository.Brand,
                    new[] {new EntityInstance() {Id = 1}}), Array.Empty<TargetInstances>(), entityWeightedDailyResults)
            };

            substituteCalculationProxy.Calculate(Arg.Is<MetricCalculationRequestInternal>(r =>
                    r.StartDate.Equals(startDateTime) &&
                    r.EndDate.Equals(endDateTime) &&
                    r.Average.AverageId.Equals(averageId)), Arg.Any<CancellationToken>())
                .Returns(metricCalculationResults);
        }

        public static IApiAverageProvider MockApiAverageProvider()
        {
            var substituteAverageProvider = Substitute.For<IApiAverageProvider>();
            var weightingAverages = MockAverageRepositorySource().Where(sourceAverage =>
                    sourceAverage.TotalisationPeriodUnit == TotalisationPeriodUnit.Month &&
                    sourceAverage.NumberOfPeriodsInAverage == 1 ||
                    sourceAverage.TotalisationPeriodUnit == TotalisationPeriodUnit.Day)
                .Select(a => new global::BrandVue.PublicApi.Models.AverageDescriptor(a));

            substituteAverageProvider.GetSupportedAverageDescriptorsForWeightings(UkSubset).Returns(weightingAverages);
            substituteAverageProvider.GetAllAvailableAverageDescriptors(UkSubset).Returns(DefaultAverageRepositoryData.GetFallbackAverages()
                .Where(a => !a.Disabled).Select(a => new global::BrandVue.PublicApi.Models.AverageDescriptor(a)));
            return substituteAverageProvider;
        }

        public static EntitySet EntitySet()
        {
            var davidInstances = new[] {new EntityInstance {Id = 1, Name = "Attenborough"}};
            var davidSet = new EntitySet(null, "All Davids", davidInstances, "david", false, false);
            return davidSet;
        }

        public static IEntitySetRepository EntitySetRepository()
        {
            IEntitySetRepository entitySetRepository = Substitute.For<IEntitySetRepository>();
            var davidSet = MockRepositoryData.EntitySet();
            entitySetRepository.GetDefaultSetForOrganisation(Arg.Any<string>(), Arg.Any<Subset>(), Arg.Any<string>())
                .Returns(davidSet);
            return entitySetRepository;
        }
    }
}
