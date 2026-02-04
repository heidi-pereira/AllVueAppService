using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using SurveyRecord = BrandVue.EntityFramework.SurveyRecord;

namespace Test.BrandVue.SourceData.Import
{
    public class SubsetRepositoryLoaderTests
    {
        private const int TestSurveyId = 12345;
        private const string TestSurveyName = "Test survey";

        [Test]
        public void ShouldAlwaysCreateAllSubset()
        {
            var loader = GetSubsetRepositoryLoader();

            var repository = loader.LoadSubsetConfiguration(new AllVueConfiguration());

            Assert.That(repository, Has.Exactly(1).Items);

            var firstSubset = repository.First();

            Assert.That(firstSubset.Id, Is.EqualTo(BrandVueDataLoader.All));
            Assert.That(firstSubset.DisplayName, Is.EqualTo(BrandVueDataLoader.All));
            Assert.That(firstSubset.DisplayNameShort, Is.EqualTo(BrandVueDataLoader.All));
            Assert.That(firstSubset.Alias, Is.EqualTo(BrandVueDataLoader.All));
            Assert.That(firstSubset.Disabled, Is.False);
            Assert.That(firstSubset.SegmentIds, Is.Empty);
            Assert.That(firstSubset.SurveyIdToSegmentNames, Contains.Key(TestSurveyId));
            Assert.That(firstSubset.SurveyIdToSegmentNames, Contains.Value(Enumerable.Empty<int>()));
            Assert.That(firstSubset.OverriddenStartDate, Is.Null);
            Assert.That(firstSubset.AlwaysShowDataUpToCurrentDate, Is.False);
        }

        [Test]
        public void ShouldLoadSubsetForEachSegmentWithData()
        {
            var segment1 = new SurveySegment
            {
                SurveySegmentId = 1,
                SurveyId = TestSurveyId,
                SegmentName = "Test segment 1"
            };

            var segment2 = new SurveySegment
            {
                SurveySegmentId = 2,
                SurveyId = TestSurveyId,
                SegmentName = "Test segment 2"
            };

            var loader = GetSubsetRepositoryLoader(segment1, segment2);

            var repository = loader.LoadSubsetConfiguration(new AllVueConfiguration());

            Assert.That(repository, Has.Exactly(3).Items);

            var allSubset = repository.First();
            var firstSubset = repository.ElementAt(1);
            var secondSubset = repository.Last();

            Assert.That(allSubset.SegmentIds, Contains.Item(segment1.SurveySegmentId));
            Assert.That(allSubset.SegmentIds, Contains.Item(segment2.SurveySegmentId));

            Assert.That(firstSubset.Id, Is.EqualTo(segment1.SegmentName));
            Assert.That(firstSubset.DisplayName, Is.EqualTo(segment1.SegmentName));
            Assert.That(firstSubset.DisplayNameShort, Is.EqualTo(segment1.SegmentName));
            Assert.That(firstSubset.Alias, Is.EqualTo(segment1.SegmentName));
            Assert.That(firstSubset.Disabled, Is.False);
            Assert.That(firstSubset.SegmentIds, Contains.Item(segment1.SurveySegmentId));
            Assert.That(firstSubset.SurveyIdToSegmentNames, Contains.Key(TestSurveyId));
            Assert.That(firstSubset.SurveyIdToSegmentNames, Contains.Value(new [] { segment1.SegmentName }));

            Assert.That(secondSubset.Id, Is.EqualTo(segment2.SegmentName));
            Assert.That(secondSubset.DisplayName, Is.EqualTo(segment2.SegmentName));
            Assert.That(secondSubset.DisplayNameShort, Is.EqualTo(segment2.SegmentName));
            Assert.That(secondSubset.Alias, Is.EqualTo(segment2.SegmentName));
            Assert.That(secondSubset.Disabled, Is.False);
            Assert.That(secondSubset.SegmentIds, Contains.Item(segment2.SurveySegmentId));
            Assert.That(secondSubset.SurveyIdToSegmentNames, Contains.Key(TestSurveyId));
            Assert.That(secondSubset.SurveyIdToSegmentNames, Contains.Value(new [] { segment2.SegmentName }));
        }

        private static SubsetRepositoryLoader GetSubsetRepositoryLoader(params SurveySegment[] testSegments)
        {
            var productContext = new ProductContext("survey", TestSurveyId.ToString(), true, TestSurveyName)
            {
                NonMapFileSurveys = new SurveyRecord[]
                {
                    new SurveyRecord
                    {
                        SurveyId = TestSurveyId,
                        SurveyName = TestSurveyName
                    }
                },
            };

            // Used for CSV reading which we're not testing
            var settings = new ConfigurationSourcedLoaderSettings(new AppSettings());
            var mockLoggerFactory = Substitute.For<ILoggerFactory>();

            var metadataContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);

            var mockChoiceSetReader = GetMockChoiceSetReader(testSegments);

            var loader = new SubsetRepositoryLoader(
                settings,
                mockLoggerFactory,
                mockChoiceSetReader,
                null,
                metadataContextFactory,
                productContext
            );
            return loader;
        }

        private static IChoiceSetReader GetMockChoiceSetReader(params SurveySegment[] testSegments)
        {
            var mockChoiceSetReader = Substitute.For<IChoiceSetReader>();

            mockChoiceSetReader.GetSegments(Arg.Any<IEnumerable<int>>()).Returns(testSegments);
            var answerSets = new List<AnswerStat>();
            foreach (var segment in testSegments)
            {
                mockChoiceSetReader.GetSegmentIds(Arg.Is<Subset>(s => s.Id == segment.SegmentName)).Returns(new[] {segment.SurveySegmentId});
                answerSets.Add(new AnswerStat { SegmentId = segment.SurveySegmentId, ResponseCount = 1 });
            }

            mockChoiceSetReader.GetSegmentIds(Arg.Is<Subset>(s => s.Id == BrandVueDataLoader.All)).Returns(testSegments.Select(s => s.SurveySegmentId).ToArray());
            mockChoiceSetReader.GetAnswerStats(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<IReadOnlyCollection<int>>()).Returns(answerSets.ToArray());
            mockChoiceSetReader.SurveyHasNonTestCompletes(Arg.Any<IEnumerable<int>>()).Returns(true);
            return mockChoiceSetReader;
        }
    }
}
