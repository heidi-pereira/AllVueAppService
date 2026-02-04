using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.Services.Llm;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using TestCommon.Extensions;
using VerifyNUnit;

namespace Test.BrandVue.FrontEnd.LlmInsights
{
    public static class InsightTestDataFactory
    {
        public static readonly EntityInstance instance1 = new() { Id = 1, Name = "Foo" };
        public static readonly EntityInstance instance2 = new() { Id = 2, Name = "Bar" };
        public static DateTime resultDateStart = new(2000, 1, 1);
        public static
            IEnumerable<IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel,
                AbstractCommonResultsInformation>> GetSerialisedTypes()
        {
            yield return CreateRankingData();
            yield return CreateMultiEntityData();
            yield return CreateFunnelData();
        }

        private static FunnelRequestData CreateFunnelData()
        {
            SigDiffOptions sigOptions = new SigDiffOptions(
                true,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);
            return new FunnelRequestData(
                new FunnelResults {
                    Results =
                    [
                        new MetricResultsForEntity { 
                            EntityInstance = instance1, 
                            MetricResults = [new MetricWeightedDailyResult() { 
                                MetricName = "Popularity", 
                                WeightedDailyResult = new WeightedDailyResult(resultDateStart) { WeightedResult = 0.81 }
                            }, new MetricWeightedDailyResult() { 
                                MetricName = "LastMonthPurchase",
                                WeightedDailyResult = new WeightedDailyResult(resultDateStart) { WeightedResult = 0.8 }
                            }]
                        },
                        new MetricResultsForEntity
                        { 
                            EntityInstance = instance2, 
                            MetricResults = [new MetricWeightedDailyResult() { 
                                MetricName = "Popularity", 
                                WeightedDailyResult = new WeightedDailyResult(resultDateStart) { WeightedResult = 0.7 }
                            }, new MetricWeightedDailyResult() { 
                                MetricName = "LastMonthPurchase",
                                WeightedDailyResult = new WeightedDailyResult(resultDateStart) { WeightedResult = 0.6 }
                            }]
                        }
                    ],
                    MarketAveragePerMeasures =
                    [
                        new MetricWeightedDailyResult() { 
                            MetricName = "Popularity", 
                            WeightedDailyResult = new WeightedDailyResult(resultDateStart) { WeightedResult = 0.81 }
                        },
                        new MetricWeightedDailyResult() { 
                            MetricName = "LastMonthPurchase",
                            WeightedDailyResult = new WeightedDailyResult(resultDateStart) { WeightedResult = 0.8 }
                        }
                    ]
                },
                new CuratedResultsModel(new DemographicFilter(new FilterRepository()),
                    [1, 2], null, ["Popularity"],
                    new Period(),
                    -1,
                    new CompositeFilterModel(),
                    sigOptions),
                []);
        }

        private static MultiEntityRequestModel CreateMultiEntityRequestModel()
        {
            return new MultiEntityRequestModel("Popularity", "All", new Period(),
                new EntityInstanceRequest("Brand", [1, 2]),
                [],
                new DemographicFilter(),
                new CompositeFilterModel(),
                [],
                [],
                false,
                SigConfidenceLevel.NinetyFive);
        }

        private static OverTimeRequestData CreateMultiEntityData()
        {
            DateTime resultDateStart = new(2000, 1, 1);
            var overTime = new OverTimeResults()
            {
                EntityWeightedDailyResults =
                [
                    new(instance1,
                        Enumerable.Range(0, 10).Select(x => new WeightedDailyResult(resultDateStart.AddMonths(x))
                            { WeightedResult = 0.8 + x * 0.01, WeightedSampleSize = 100 }).ToList()
                    ),
                    new(instance2,
                        Enumerable.Range(0, 10).Select(x => new WeightedDailyResult(resultDateStart.AddMonths(x))
                            { WeightedResult = 0.7 + x * 0.01, WeightedSampleSize = 100 }).ToList()
                    )
                ]
            };
            var multiEntityRequest = CreateMultiEntityRequestModel();
            return new OverTimeRequestData(
                overTime,
                multiEntityRequest,
                []);
        }

        private static RankingRequestData CreateRankingData()
        {
            DateTime resultDateStart = new(2000, 1, 1);
            var multiEntityRequest = CreateMultiEntityRequestModel();
            return new RankingRequestData(
                new RankingTableResults(new[]
                {
                    new RankingTableResult(new EntityInstance { Id = 1, Name = "Foo" },
                        1,
                        2,
                        new WeightedDailyResult(resultDateStart) { WeightedResult = 0.8, WeightedSampleSize = 100 },
                        new WeightedDailyResult(resultDateStart) { WeightedResult = 0.81, WeightedSampleSize = 100 },
                        false),
                    new RankingTableResult(new EntityInstance { Id = 2, Name = "Bar" },
                        2,
                        1,
                        new WeightedDailyResult(resultDateStart) { WeightedResult = 0.8, WeightedSampleSize = 100 },
                        new WeightedDailyResult(resultDateStart) { WeightedResult = 0.8, WeightedSampleSize = 100 },
                        false)
                }),
                multiEntityRequest,
                []
            );
        }
    }

    [TestFixture]
    public class LlmInsightsSerialisationTests
    {
        [Test]
        [TestCaseSource(typeof(InsightTestDataFactory), nameof(InsightTestDataFactory.GetSerialisedTypes))]
        public void TestGenerateId_AsMd5Hash(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation>
                requestData)
        {
            // Act
            string result = requestData.ToHash();

            //Assert
            Assert.That(result != null);
            Verifier.Verify(result);
        }

        [Test]
        [TestCaseSource(typeof(InsightTestDataFactory), nameof(InsightTestDataFactory.GetSerialisedTypes))]
        public void TestGenerateId_MustBeLessThan256Length(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation>
                requestData)
        {
            // Act
            string result = requestData.ToHash();

            //Assert
            Assert.That(result.Length < 256);
        }

        [Test]
        [TestCaseSource(typeof(InsightTestDataFactory), nameof(InsightTestDataFactory.GetSerialisedTypes))]
        public async Task WillSerialiseCorrectlyToCsv(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation>
                requestData)
        {
            string serialised = Serialised(requestData);
            Assert.That(serialised, Does.Contain("Foo"));
            Assert.That(serialised, Does.Contain("Bar"));
            Assert.That(serialised, Does.Contain("0.8"));
            Assert.That(serialised, Does.Contain("0.81"));
        }

        private string Serialised(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation>
                requestData)
        {
            var rpp = new ResultsProviderParameters()
            {
                FocusEntityInstanceId = 1,
                RequestedInstances = new TargetInstances(TestEntityTypeRepository.Brand,
                    [InsightTestDataFactory.instance1, InsightTestDataFactory.instance2]),
                PrimaryMeasure = new Measure
                {
                    Name = "metric",
                    Minimum = 0,
                    Maximum = 1,
                    NumberFormat = "%",
                    HelpText = "QUESTION",
                    Field = new ResponseFieldDescriptor("BrandField", [ TestEntityTypeRepository.Brand ])
                }
            };
            var serialised = requestData.FormatDataPrompt(rpp);
            return serialised;
        }
    }
}