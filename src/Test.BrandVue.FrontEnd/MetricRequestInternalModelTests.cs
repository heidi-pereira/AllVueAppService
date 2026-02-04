using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.QuotaCells;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;

namespace Test.BrandVue.FrontEnd
{
    [TestFixture]
    public class MetricRequestInternalModelTests
    {
        private IEntityRepository _entityRepository;

        private static T[] ArrayFor<T>(params T[] args) => args;
        private static EntityInstance[] EntityInstancesFor(params int[] ids) =>
            ids.Select(i => new EntityInstance {Id = i}).ToArray();
        private static TargetInstances TargetInstanceFor(string entityName, params int[] ids) =>
            new TargetInstances(new EntityType { Identifier = entityName }, EntityInstancesFor(ids));
        private static TargetInstances[] TargetInstancesFor(IEnumerable<(string EntityName, int[] Ids)> entitiesAndIds) =>
            entitiesAndIds.Select(t => TargetInstanceFor(t.EntityName, t.Ids)).ToArray();

        [OneTimeSetUp]
        public void Init()
        {
            _entityRepository = Substitute.For<IEntityRepository>();
            _entityRepository.GetInstancesOf("brand", ExpectedOutputs.UkSurveysetDescriptor).Returns(EntityInstancesFor(1, 2, 3));
            _entityRepository.GetInstancesOf("product", ExpectedOutputs.UkSurveysetDescriptor).Returns(EntityInstancesFor(11, 12, 13, 14));
            _entityRepository.GetInstancesOf("image", ExpectedOutputs.UkSurveysetDescriptor).Returns(EntityInstancesFor(101, 102, 103, 104, 105));
            _entityRepository.GetInstancesOf("brand", ExpectedOutputs.UsSurveysetDescriptor).Returns(EntityInstancesFor(1, 2, 4, 5, 6));
            _entityRepository.GetInstancesOf("product", ExpectedOutputs.UsSurveysetDescriptor).Returns(EntityInstancesFor(15, 16, 17, 18));
        }

        private static IEnumerable<TestCaseData> InvalidCalculationRequestsData()
        {
            yield return new TestCaseData("2020-01-01", "2019-05-31", new Dictionary<string, int[]>(), "UK", "Monthly",
                    "gender", "2020-06-01", ArrayFor("End Date 2019-05-31 is greater than the Start Date 2020-01-01"))
                .SetName("Gender end date before start date");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]>(), "UK", "Monthly",
                    "net-buzz", "2020-06-01", ArrayFor("ClassInstances key combination: [] is invalid for 'Net Buzz' metric. Keys should be [brand]"))
                .SetName("Net Buzz with no class instances");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]>  { {"brand", Array.Empty<int>() }}, "UK", "Monthly",
                    "net-buzz", "2020-06-01", ArrayFor($"{nameof(MetricCalculationRequest.ClassInstances)} {nameof(ClassInstanceDescriptor.ClassInstanceId)} must be specified."))
                .SetName("Net Buzz with brand entity in request, but no instances");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]> { {"brand", ArrayFor(1, 2, 3, 4) }}, "UK", "Monthly",
                    "net-buzz", "2020-06-01", ArrayFor($"Invalid {nameof(ClassInstanceDescriptor.ClassInstanceId)}s have been found in the request. They are brand: [4]."))
                .SetName("Net Buzz request, but brand id not in subset");
            yield return new TestCaseData("2018-01-01", "2020-05-31", new Dictionary<string, int[]> { {"brand", ArrayFor(1, 2, 3) }}, "UK", "14Days",
                    "net-buzz", "2020-06-01", ArrayFor($"Request has exceeded the 500 result limit. Request would yield 2646 results."))
                .SetName("Net Buzz request, but result limit has been exceeded");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]> { {"brand", ArrayFor(1, 2, 3) }}, "UK", "Monthly",
                    "brand-product-other", "2020-06-01", ArrayFor("ClassInstances key combination: [brand] is invalid for 'Brand Product Other' metric. Keys should be [brand, product]"))
                .SetName("Brand Product Other with no product class instances");
        }

        [Test, TestCaseSource(nameof(InvalidCalculationRequestsData))]
        public void InvalidCalculationRequests(string requestStartDate, string requestEndDate, Dictionary<string, int[]> requestedEntityInstances,
            string subsetId, string averageId, string metricUrlSafeName, string subsetResponsesEndDate, string[] expectedErrorStrings)
        {
            var metricRequestInternal = CreateMetricCalculationRequestInternalFromParameters(requestStartDate, requestEndDate, requestedEntityInstances, subsetId, averageId, metricUrlSafeName, subsetResponsesEndDate);

            Assert.That(metricRequestInternal.IsValid, Is.False);
            Assert.That(expectedErrorStrings, Is.EquivalentTo(metricRequestInternal.Errors));
        }

        private static IEnumerable<TestCaseData> ValidCalculationRequestsData()
        {
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]>(), "UK", "Monthly",
                    "gender", "2020-06-01", TargetInstanceFor(EntityType.Profile), TargetInstancesFor(Array.Empty<(string, int[])>()))
                .SetName("Valid Profile metric gender for UK");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]> { { "brand", ArrayFor(1, 2, 3) } }, "UK", "Monthly",
                    "net-buzz", "2020-06-01", TargetInstanceFor(EntityType.Brand, 1, 2, 3), TargetInstancesFor(Array.Empty<(string, int[])>()))
                .SetName("Valid Brand metric Net Buzz for UK");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]> { { "brand", ArrayFor(1, 2, 3) }, { "product", ArrayFor(11, 12, 13, 14) }}, "UK", "Monthly",
                    "brand-product-other", "2020-06-01", TargetInstanceFor(EntityType.Product, 11, 12, 13, 14), TargetInstancesFor(ArrayFor((EntityType.Brand, ArrayFor(1,2,3)))))
                .SetName("Valid Brand Product metric Brand Product Other request for UK");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]>(), "US", "Monthly",
                    "gender", "2020-06-01", TargetInstanceFor(EntityType.Profile), TargetInstancesFor(Array.Empty<(string, int[])>()))
                .SetName("Valid Profile metric gender for US");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]> { { "brand", ArrayFor(1, 2, 4, 5, 6) } }, "US", "Monthly",
                    "net-buzz", "2020-06-01", TargetInstanceFor(EntityType.Brand, 1, 2, 4, 5, 6), TargetInstancesFor(Array.Empty<(string, int[])>()))
                .SetName("Valid Brand metric Net Buzz for US");
            yield return new TestCaseData("2020-01-01", "2020-05-31", new Dictionary<string, int[]> { { "brand", ArrayFor(1, 2, 4, 5, 6) }, { "product", ArrayFor(15, 16, 17, 18) }}, "US", "Monthly",
                    "brand-product-other", "2020-06-01", TargetInstanceFor(EntityType.Brand, 1, 2, 4, 5, 6), TargetInstancesFor(ArrayFor((EntityType.Product, ArrayFor(15, 16, 17, 18)))))
                .SetName("Valid Brand Product metric Brand Product Other request for US");
        }

        [Test, TestCaseSource(nameof(ValidCalculationRequestsData))]
        public void ValidCalculationRequests(string requestStartDate, string requestEndDate, Dictionary<string, int[]> requestedEntityInstances,
            string subsetId, string averageId, string metricUrlSafeName, string subsetResponsesEndDate, TargetInstances expectedPrimaryInstances, TargetInstances[] expectedFilterInstances)
        {
            var metricRequestInternal = CreateMetricCalculationRequestInternalFromParameters(requestStartDate, requestEndDate, requestedEntityInstances, subsetId, averageId, metricUrlSafeName, subsetResponsesEndDate);

            Assert.That(metricRequestInternal.IsValid, Is.True);
            Assert.That(metricRequestInternal.Errors, Is.Empty);
            Assert.That(expectedPrimaryInstances, Is.EqualTo(metricRequestInternal.PrimaryEntityInstances));
            Assert.That(expectedFilterInstances, Is.EqualTo(metricRequestInternal.FilterEntityInstancesCollection));
        }

        private MetricCalculationRequestInternal CreateMetricCalculationRequestInternalFromParameters(string requestStartDate,
            string requestEndDate, Dictionary<string, int[]> requestedEntityInstances, string subsetId, string averageId,
            string metricUrlSafeName, string subsetResponsesEndDate)
        {
            var startDate = DateTimeOffset.Parse(requestStartDate);
            var endDate = DateTimeOffset.Parse(requestEndDate);
            var surveySet = ExpectedOutputs.SurveysetDescriptors().First(s => s.SurveysetId == subsetId);
            var averageDescriptor =
                new AverageDescriptor(MockRepositoryData.MockAverageRepositorySource().First(a => a.AverageId == averageId));
            var metricDescriptor =
                new MetricDescriptor(MockRepositoryData.CreateSampleMeasures().First(m => m.UrlSafeName == metricUrlSafeName));
            var subsetEndDate = DateTimeOffset.Parse(subsetResponsesEndDate);
            var profileResponseAccessor = Substitute.For<IProfileResponseAccessor>();
            profileResponseAccessor.EndDate.Returns(subsetEndDate);
            var metricRequest = new MetricCalculationRequest(startDate, endDate, requestedEntityInstances);
            var metricRequestInternal = new MetricCalculationRequestInternal(metricRequest, surveySet, averageDescriptor,
                metricDescriptor, _entityRepository, profileResponseAccessor, 500);
            return metricRequestInternal;
        }
    }
}
