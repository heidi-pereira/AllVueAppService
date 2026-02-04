using System.Collections.Generic;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Import
{
    public class ChoicesetReaderTests
    {
        public class MyChoiceSetReader : ChoiceSetReader
        {

            public MyChoiceSetReader(SurveySegment[] testSegments) : base((IAnswerDbContextFactory)null, null)
            {
                TestSegments = testSegments;
            }

            public SurveySegment[] TestSegments { get; }

            override public IEnumerable<SurveySegment> GetSegments(IEnumerable<int> surveyIds)
            {
                return TestSegments;
            }
        }

        [Test]
        public void SegmentNamesNotCaseSentive()
        {
            const int TestSurveyId = 12345;
            var segment1 = new SurveySegment
            {
                SurveySegmentId = 1,
                SurveyId = TestSurveyId,
                SegmentName = "...anything lowercased..."
            };

            var segment2 = new SurveySegment
            {
                SurveySegmentId = 1,
                SurveyId = TestSurveyId + 1,
                SegmentName = "Never Mind"
            };
           var testSegments = new[] { segment1, segment2, };
            var choiceSetReader = new MyChoiceSetReader(testSegments);

            var mySubset = new Subset();

            var items = new Dictionary<int, IReadOnlyCollection<string>>
            {
                [segment1.SurveyId] = new string[] { segment1.SegmentName.ToUpper() },
                [segment2.SurveyId] = new string[] { segment2.SegmentName }
            };

            mySubset.SurveyIdToSegmentNames = items;
            var result = choiceSetReader.GetSegmentIds(mySubset);

            Assert.That(result.Length, Is.EqualTo(testSegments.Length), "Failed to find all the matching items");
        }
    }
}
