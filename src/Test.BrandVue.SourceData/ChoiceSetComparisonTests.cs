using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AnswersMetadata;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class ChoiceSetComparisonTests
    {
        private int _id = 0;

        [Test]
        public void ChoiceSetsWithDifferentIdsShouldNotMatch()
        {
            var choiceSetOne = new ChoiceSet()
            {
                ChoiceSetId = 1,
                Name = "ChoicesOne",
                SurveyId = 1,
                Choices = Enumerable.Range(1, 7).Select(i => CreateChoice(i)).ToArray()
            };

            var choiceSetTwo = new ChoiceSet()
            {
                ChoiceSetId = 2,
                Name = "ChoicesTwo",
                SurveyId = 1,
                Choices = new List<Choice>
                {
                    new Choice() { ChoiceId = 8, SurveyChoiceId = 1, Name = "Television" },
                    new Choice() { ChoiceId = 9, SurveyChoiceId = 2, Name = "Radio" },
                    new Choice() { ChoiceId = 10, SurveyChoiceId = 3, Name = "Newspaper - print" },
                    new Choice() { ChoiceId = 11, SurveyChoiceId = 4, Name = "Newspaper - digital" },
                    new Choice() { ChoiceId = 12, SurveyChoiceId = 5, Name = "Social media (e.g. Facebook, YouTube)" },
                    new Choice() { ChoiceId = 13, SurveyChoiceId = 6, Name = "Online blogs / articles" },
                    new Choice() { ChoiceId = 14, SurveyChoiceId = 7, Name = "Podcasts" },
                    new Choice() { ChoiceId = 15, SurveyChoiceId = 8, Name = "News aggregation website" },
                    new Choice() { ChoiceId = 16, SurveyChoiceId = 9, Name = "Friends and family" },
                    new Choice() { ChoiceId = 17, SurveyChoiceId = 98, Name = "Other" }
                }
            };

            var groups = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() {choiceSetOne, choiceSetTwo});
            Assert.That(groups.Length, Is.EqualTo(2));
        }

        [Test]
        public void ChoiceSetsWithTheSameSurveyChoiceIdsButDifferentChoiceNamesShouldNotMatch()
        {
            var choiceSetOne = new ChoiceSet()
            {
                ChoiceSetId = 1,
                Name = "ChoicesOne",
                SurveyId = 1,
                Choices = Enumerable.Range(1, 7).Select(i => CreateChoice(i)).ToArray()
            };

            var choiceSetTwo = new ChoiceSet()
            {
                ChoiceSetId = 2,
                Name = "ChoicesTwo",
                SurveyId = 1,
                Choices = new List<Choice>
                {
                    new Choice() { ChoiceId = 8, SurveyChoiceId = 1, Name = "Television" },
                    new Choice() { ChoiceId = 9, SurveyChoiceId = 2, Name = "Radio" },
                    new Choice() { ChoiceId = 10, SurveyChoiceId = 3, Name = "Newspaper - print" },
                    new Choice() { ChoiceId = 11, SurveyChoiceId = 4, Name = "Newspaper - digital" },
                    new Choice() { ChoiceId = 12, SurveyChoiceId = 5, Name = "Social media (e.g. Facebook, YouTube)" },
                    new Choice() { ChoiceId = 13, SurveyChoiceId = 6, Name = "Online blogs / articles" },
                    new Choice() { ChoiceId = 14, SurveyChoiceId = 7, Name = "Podcasts" },
                }
            };

            var groups = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() {choiceSetOne, choiceSetTwo});
            Assert.That(groups.Length, Is.EqualTo(2));
        }

        [Test]
        public void ChoiceSetsWithSameRootAncestorsShouldBeGroupedTogether()
        {
            var ancestor = new ChoiceSet { Name = "Root", SurveyId = 1, Choices = Enumerable.Range(1, 3).Select(i => CreateChoice(i)).ToList() };
            var child1 = new ChoiceSet { Name = "Child1", SurveyId = 1, Choices = ancestor.Choices.ToList(), ParentChoiceSet1 = ancestor, ParentChoiceSet1Id = ancestor.ChoiceSetId };
            var child2 = new ChoiceSet { Name = "Child2", SurveyId = 1, Choices = ancestor.Choices.ToList(), ParentChoiceSet1 = ancestor, ParentChoiceSet1Id = ancestor.ChoiceSetId };
            var groups = ChoiceSetReader.GetChoiceSetGroups([ancestor, child1, child2]);
            Assert.That(groups.Length, Is.EqualTo(1));
        }

        [Test]
        public void ChoiceSetsWithDifferentRootAncestorsShouldNotBeGroupedTogether()
        {
            var ancestor1 = new ChoiceSet { ChoiceSetId = 1, Name = "Root1", SurveyId = 1, Choices = Enumerable.Range(1, 3).Select(i => CreateChoice(i)).ToList() };
            var ancestor2 = new ChoiceSet { ChoiceSetId = 2, Name = "Root2", SurveyId = 1, Choices = Enumerable.Range(4, 3).Select(i => CreateChoice(i)).ToList() };
            var child1 = new ChoiceSet { Name = "Child1", SurveyId = 1, Choices = ancestor1.Choices.ToList(), ParentChoiceSet1 = ancestor1, ParentChoiceSet1Id = ancestor1.ChoiceSetId };
            var child2 = new ChoiceSet { Name = "Child2", SurveyId = 1, Choices = ancestor2.Choices.ToList(), ParentChoiceSet1 = ancestor2, ParentChoiceSet1Id = ancestor2.ChoiceSetId };
            var groups = ChoiceSetReader.GetChoiceSetGroups([ancestor1, ancestor2, child1, child2]);
            Assert.That(groups.Length, Is.EqualTo(2));
        }

        [Test]
        public void RootChoiceSetsWithDifferentChoicesShouldNotBeGroupedTogether()
        {
            var cs1 = new ChoiceSet { ChoiceSetId = 1, Name = "Root1", SurveyId = 1, Choices = Enumerable.Range(1, 2).Select(i => CreateChoice(i)).ToList() };
            var cs2 = new ChoiceSet { ChoiceSetId = 2, Name = "Root2", SurveyId = 1, Choices = Enumerable.Range(3, 2).Select(i => CreateChoice(i)).ToList() };
            var groups = ChoiceSetReader.GetChoiceSetGroups([cs1, cs2]);
            Assert.That(groups.Length, Is.EqualTo(2));
        }

        [Test]
        public void ChoiceSetsWithDifferentAncestryShouldNotBeGroupedTogether()
        {
            var ancestor1 = new ChoiceSet { ChoiceSetId = 1, Name = "A1", SurveyId = 1, Choices = Enumerable.Range(1, 2).Select(i => CreateChoice(i)).ToList() };
            var ancestor2 = new ChoiceSet { ChoiceSetId = 2, Name = "A2", SurveyId = 1, Choices = Enumerable.Range(3, 2).Select(i => CreateChoice(i)).ToList() };
            var cs1 = new ChoiceSet { Name = "C1", SurveyId = 1, Choices = ancestor1.Choices.ToList(), ParentChoiceSet1 = ancestor1, ParentChoiceSet1Id = ancestor1.ChoiceSetId };
            var cs2 = new ChoiceSet { Name = "C2", SurveyId = 1, Choices = ancestor2.Choices.ToList(), ParentChoiceSet1 = ancestor2, ParentChoiceSet1Id = ancestor2.ChoiceSetId };
            var groups = ChoiceSetReader.GetChoiceSetGroups([cs1, cs2]);
            Assert.That(groups.Length, Is.EqualTo(2));
        }

        [Test]
        public void ChoiceSetsWithDifferentAncestryButSameChoicesShouldBeGroupedTogether()
        {
            var choices = Enumerable.Range(1, 3).Select(i => CreateChoice(i)).ToList();
            var cs1 = new ChoiceSet { Name = "Root", SurveyId = 1, Choices = choices };
            var cs2 = new ChoiceSet { Name = "A", SurveyId = 1, Choices = choices, ParentChoiceSet1 = cs1, ParentChoiceSet1Id = cs1.ChoiceSetId };
            var cs3 = new ChoiceSet { Name = "B", SurveyId = 1, Choices = choices };
            var groups = ChoiceSetReader.GetChoiceSetGroups([cs1, cs2, cs3]);
            Assert.That(groups.Length, Is.EqualTo(1));
        }

        [Test]
        public void IdenticalChoiceSetReferencesShouldBeGroupedTogether()
        {
            var cs = new ChoiceSet { Name = "Same", SurveyId = 1, Choices = Enumerable.Range(1, 2).Select(i => CreateChoice(i)).ToList() };
            var groups = ChoiceSetReader.GetChoiceSetGroups([cs, cs]);
            Assert.That(groups.Length, Is.EqualTo(1));
        }

        /// <summary>
        /// Note: If the hashcode was always 0 this would fail, but hash code clashes should be pretty rare and hard to chain...
        /// </summary>
        [Test]
        public void SimilarChoiceSetsShouldBeGroupedTheSameRegardlessOfOrder()
        {
            var choiceSetForwards = new ChoiceSet() { Name = "ChoicesForwards", SurveyId = 1, Choices = Enumerable.Range(1, 9).Select(i => CreateChoice(i)).ToArray() };
            var choiceSetBackwards = new ChoiceSet() { Name = "ChoicesBackwards", SurveyId = 1, Choices = Enumerable.Range(1, 9).Select(i => CreateChoice(i)).Reverse().ToArray() };

            var groupsForwards = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() { choiceSetForwards, choiceSetBackwards,});
            var groupsBackwards = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() { choiceSetBackwards, choiceSetForwards});

            Assert.Multiple(() =>
                {
                    Assert.That(groupsForwards.Length, Is.EqualTo(1));
                    Assert.That(groupsBackwards.Length, Is.EqualTo(1));
                }
            );
        }

        private Choice CreateChoice(int id, string name = null) =>
            new Choice() { ChoiceId = _id++, SurveyChoiceId = id, Name = name ?? id.ToString() };

        [Test]
        public void ChoiceSetsWithTheSameIdsAndSameNamesShouldMatch()
        {
            var choiceSetOne = new ChoiceSet()
            {
                Name = "ChoicesOne",
                SurveyId = 1,
                Choices = Enumerable.Range(1, 7).Select(i => CreateChoice(i)).ToArray()
            };

            var choiceSetTwo = new ChoiceSet()
            {
                Name = "ChoicesTwo",
                SurveyId = 1,
                Choices = Enumerable.Range(1, 7).Select(i => CreateChoice(i)).ToArray()
            };

            var groups = ChoiceSetReader.GetChoiceSetGroups(new List<ChoiceSet>() {choiceSetOne, choiceSetTwo});
            Assert.That(groups.Length, Is.EqualTo(1));
        }
    }
}
