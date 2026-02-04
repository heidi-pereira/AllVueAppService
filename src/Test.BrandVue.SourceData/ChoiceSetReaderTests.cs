using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AnswersMetadata;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class ChoiceSetReaderTests
    {
        [Test]
        public void ShouldAlwaysGetSameCanonicalChoiceSetIfAncestorsEqual()
        {
            var choiceSetOne = new ChoiceSet()
            {
                Name = "Choice set one",
                ChoiceSetId = 1,
                SurveyId = 1,
                ParentChoiceSet1 = null,
                ParentChoiceSet2 = null,
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        ChoiceId = 1,
                        ChoiceSetId = 1,
                        Name = "SameChoice",
                        SurveyChoiceId = 1,
                        SurveyId = 1,
                    }
                }
            };

            var choiceSetTwo = new ChoiceSet()
            {
                Name = "Choice set two",
                ChoiceSetId = 2,
                SurveyId = 2,
                ParentChoiceSet1 = null,
                ParentChoiceSet2 = null,
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        ChoiceId = 2,
                        ChoiceSetId = 2,
                        Name = "SameChoice",
                        SurveyChoiceId = 1,
                        SurveyId = 2,
                    }
                }
            };

            var firstGroup = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() {choiceSetOne, choiceSetTwo}).Single();
            var secondGroup = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() {choiceSetTwo, choiceSetOne}).Single();

            Assert.That(firstGroup.Canonical.Name, Is.EqualTo(secondGroup.Canonical.Name));
        }
    }
}
