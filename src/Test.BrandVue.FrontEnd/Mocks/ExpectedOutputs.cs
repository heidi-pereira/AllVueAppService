using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Models.Filters;
using BrandVue.SourceData.Weightings;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.Mocks
{
    /// <summary>
    /// This shouldn't reference MockRepositoryData
    /// </summary>
    public static class ExpectedOutputs
    {
        public static IEnumerable<AverageDescriptor> Averages()
        {
            return new List<AverageDescriptor>
            {
                new AverageDescriptor("14Days", "14 days"),
                new AverageDescriptor("28Days", "28 days"),
                new AverageDescriptor("Weekly", "Weekly"),
                new AverageDescriptor("Monthly", "Monthly"),
                new AverageDescriptor("MonthlyOver3Months", "Monthly (over 3 months)"),
                new AverageDescriptor("Quarterly", "Quarterly"),
                new AverageDescriptor("HalfYearly", "HalfYearly"),
            };
        }

        public static IEnumerable<AverageDescriptor> WeightingsAverages()
        {
            return new List<AverageDescriptor>
            {
                new AverageDescriptor("14Days", "14 days"),
                new AverageDescriptor("28Days", "28 days"),
                new AverageDescriptor("Weekly", "Weekly"),
                new AverageDescriptor("Monthly", "Monthly"),
            };
        }
        [Obsolete("Should be using WeightingCell")]
        public static IEnumerable<DemographicCellWeighting> DemographicCellWeightings()
        {
            return new List<DemographicCellWeighting>
            {
                new(0, 0.6666666666666666),
                new(1, 1.3333333333333333)
            };
        }
        
        public static IEnumerable<Weight> CellWeightings()
        {
            return new List<Weight>
            {
                new(0, 0.6666666666666666),
                new(1, 1.3333333333333333)
            };
        }

        public static IEnumerable<MetricDescriptor> Metrics()
        {
            var profileClassIds = new string[0];
            var brandClassIds = new[] { "brand" };
            return new List<MetricDescriptor>
            {
                //Lists standard case
                new MetricDescriptor(
                    "net-buzz",
                    "Net Buzz",
                    "Help text for net buzz",
                    new FilterInfoList("Positive_Buzz", brandClassIds) { IncludeList = new [] { 1 } },
                    new FilterInfoList("Consumer_Segment", brandClassIds) { IncludeList = new [] { 1, 2, 3, 4} },
                    new DateTime(2017, 6, 30, 0, 0, 0),
                    "brand",
                    new []{ "brand" }
                ),
                //Base question does not contain vals.
                new MetricDescriptor(
                    "buzz-noise",
                    "Buzz Noise",
                    "",
                    new FilterInfoList("Positive_Buzz", brandClassIds) { IncludeList = new [] { 1 } },
                    new FilterInfoNotNull("Consumer_Segment", brandClassIds),
                    new DateTime(2017, 6, 30, 0, 0, 0),
                    "brand",
                    new []{ "brand" }
                ),
                //Main question and Base question are neither a list, range or not null.
                new MetricDescriptor(
                    "gender",
                    "Gender",
                    "",
                    new FilterInfoUnknown("Gender", profileClassIds),
                    new FilterInfoUnknown("Gender", profileClassIds),
                    new DateTime(2000, 1, 1, 0, 0, 0),
                    "profile",
                    Array.Empty<string>()
                ),
                //Base question name is the same as main question name and represent a range.
                new MetricDescriptor(
                    "age",
                    "Age",
                    "",
                    new FilterInfoRange("Age", profileClassIds) { Min = 16, Max = 74 },
                    new FilterInfoRange("Age", profileClassIds) { Min = 16, Max = 74 },
                    new DateTime(2000, 1, 1, 0, 0, 0),
                    "profile",
                    Array.Empty<string>()
                ),
                //Base question is null.
                new MetricDescriptor(
                    "products-used-insurance-breakdown-cover",
                    "Products Used: Insurance - Breakdown Cover",
                    "",
                    new FilterInfoRange("BreakdownCover", profileClassIds) { Min = 16, Max = 74 },
                    new FilterInfoUnknown("BreakdownCover", profileClassIds),
                    new DateTime(2000, 1, 1, 0, 0, 0),
                    "profile",
                    Array.Empty<string>()
                    ),
                //Product brand question
                new MetricDescriptor(
                    "brand-awareness-other",
                    "Brand Awareness Other",
                    "",
                    new FilterInfoList("Brand_Awareness_Other", profileClassIds) { IncludeList = new [] { 1 } },
                    new FilterInfoList("Brand_Awareness_Other", profileClassIds) { IncludeList = new [] { 1, 2, 3 } },
                    new DateTime(2015, 1, 1, 0, 0, 0),
                    "product",
                    new []{ "product" }
                ),
                //Product brand question
                new MetricDescriptor(
                    "brand-product-other",
                    "Brand Product Other",
                    "",
                    new FilterInfoList("Brand_Product_Other", profileClassIds) { IncludeList = new [] { 1 } },
                    new FilterInfoList("Brand_Product_Other", profileClassIds) { IncludeList = new [] { 1, 2, 3 } },
                    new DateTime(2015, 1, 1, 0, 0, 0),
                    "brand|product",
                    new []{ "brand", "product" }
                ),
                new MetricDescriptor(
                    "customer-tv-platforms",
                    "Customer TV platforms",
                    "",
                    new FilterInfoList("TV_platforms", new []{ "brand", "tvplatform" }) { IncludeList = new [] { 1 } },
                    new FilterInfoList("TV_platforms", brandClassIds) { IncludeList = new [] { 4, 5, 6 } },
                    new DateTime(2015, 1, 1, 0, 0, 0),
                    "brand|tvplatform",
                    new []{ "brand", "tvplatform" }
                )
            };
        }

        public static IEnumerable<QuestionDescriptor> Questions()
        {
            return Questions(TestEntityTypeRepository.Brand)
                .Concat(Questions(TestEntityTypeRepository.Product))
                .Concat(Questions(TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand))
                .Concat(Questions(TestEntityTypeRepository.Profile))
                .OrderBy(q => q.QuestionId);
        }

        public static IEnumerable<QuestionDescriptor> Questions(params EntityType[] responseEntityTypes)
        {
            var classes = responseEntityTypes.Where(r => !r.IsProfile).Select(e => e.Identifier).ToArray();
            string fieldNamePart = string.Join("_", responseEntityTypes.Select(e => e.Identifier));
            return new List<QuestionDescriptor>
            {
                new QuestionDescriptor { QuestionId = $"{fieldNamePart}_Category_Type_Question", QuestionText = "Choose from one of 2 categories?",
                    AnswerSpec = new QuestionMultipleChoiceAnswer
                    {
                        Choices = new List<QuestionChoice>
                        {
                            new QuestionChoice { Id = "1", Value = "Category 1" },
                            new QuestionChoice { Id = "2", Value = "Category 2" }
                        }
                    },
                    Classes = classes
                },
                new QuestionDescriptor { QuestionId = $"{fieldNamePart}_Category_Type_Question_But_With_Value_Type_Specified", QuestionText = "Choose from one of three categories?",
                    AnswerSpec = new QuestionMultipleChoiceAnswer
                    {
                        Choices = new List<QuestionChoice>
                        {
                            new QuestionChoice { Id = "1", Value = "Category 1" },
                            new QuestionChoice { Id = "2", Value = "Category 2" },
                            new QuestionChoice { Id = "3", Value = "Category 3" }
                        }
                    },
                    Classes = classes
                },
                new QuestionDescriptor { QuestionId = $"{fieldNamePart}_OpenText_Type_Question", QuestionText = "What is your opentext first name?",
                    AnswerSpec = new QuestionTextAnswer(),
                    Classes = classes
                },
                new QuestionDescriptor { QuestionId = $"{fieldNamePart}_Text_Type_Question", QuestionText = "What is your first name?",
                    AnswerSpec = new QuestionTextAnswer(),
                    Classes = classes
                },
                new QuestionDescriptor { QuestionId = $"{fieldNamePart}_Unknown_Type_Question", QuestionText = "What is the answer to life, the universe and everything?",
                    AnswerSpec = new QuestionValueAnswer
                    {
                        Multiplier = 1,
                        MinValue = 0,
                        MaxValue = 0
                    },
                    Classes = classes
                },
                new QuestionDescriptor { QuestionId = $"{fieldNamePart}_Value_Type_Question", QuestionText = "What is the value of something?",
                    AnswerSpec = new QuestionValueAnswer
                    {
                        Multiplier = 1,
                        MinValue = 0,
                        MaxValue = 0
                    },
                    Classes = classes
                }
            };
        }

        public static SurveysetInfo SurveysetInfo()
        {
            return new SurveysetInfo(DateTime.Parse("2017-05-01"), DateTime.Parse("2019-06-15"));
        }

        public static List<DemographicCellDescriptor> DemographicCellDescriptors()
        {
            return new List<DemographicCellDescriptor>
            {
                new(0, "L", "0", "0", "0"),
                new(1, "N", "0", "0", "0"),
            };
        }
        public static List<WeightingCellDescriptor> WeightingCellDescriptors()
        {
            return new List<WeightingCellDescriptor>
            {
                new(-1, new Dictionary<string, string>(), false),
                new(0,  new Dictionary<string, string>{ {"region", "L"}, {"gender","0"}, {"ageGroup","0"}, {"socioEconomicGroupIndicator","0"} }, true),
                new(1,  new Dictionary<string, string>{ {"region", "N"}, {"gender","0"}, {"ageGroup","0"}, {"socioEconomicGroupIndicator","0"} }, true),

            };
        }

        public static readonly SurveysetDescriptor UkSurveysetDescriptor = SurveysetDescriptors().ToArray()[0];
        public static readonly SurveysetDescriptor UsSurveysetDescriptor = SurveysetDescriptors().ToArray()[1];

        public static IReadOnlyCollection<SurveysetDescriptor> SurveysetDescriptors()
        {
            return new[]
            {
                new SurveysetDescriptor(MockRepositoryData.UkSubset),
                new SurveysetDescriptor(MockRepositoryData.USSubset),
            };
        }

        public static IReadOnlyCollection<ClassInstanceDescriptor> BrandInstanceDescriptors()
        {
            return MockRepositoryData.ClassInstanceDescriptors();
        }

        public static IReadOnlyCollection<ClassInstanceDescriptor> ProductInstanceDescriptors()
        {
            return MockRepositoryData.ProductClassInstanceDescriptors();
        }

        public static List<ClassDescriptor> ClassDescriptors()
        {
            return new List<ClassDescriptor>
                {
                    new ClassDescriptor("brand", "brand", Array.Empty<string>()),
                    new ClassDescriptor("product", "product", Array.Empty<string>())
                };
        }
    }
}
