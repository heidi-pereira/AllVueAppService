using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Entity;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    public class SurveyIdVariableTests
    {
        private const string EntityTypeName = "SurveyCondition";
        private const int FirstGroupId = 1;
        private const int SecondGroupId = 2;

        [Test]
        public void ResponsesForThiSSurveyShouldBeIncluded()
        {
            var surveyId = 1;
            var surveyCondition = GetSurveyCondition(new[] { surveyId }, new[] { -1 });
            var surveyFunction = GetSurveyVariableFunction(surveyCondition, FirstGroupId);

            var result = surveyFunction(new ProfileResponseEntity(1, DateTimeOffset.Now, surveyId));
            Assert.That(result, Is.EqualTo(FirstGroupId));
        }

        [Test]
        public void ResponseForOtherSurveysShouldNotBeIncluded()
        {
            var surveyId = 1;
            var otherSurveyId = 2;
            var surveyCondition = GetSurveyCondition(new[] { surveyId }, new[] { -1 });
            var surveyFunction = GetSurveyVariableFunction(surveyCondition, FirstGroupId);

            var result = surveyFunction(new ProfileResponseEntity(1, DateTimeOffset.Now, otherSurveyId));
            Assert.That(result, Is.EqualTo(null));
        }

        [Test]
        public void AnyOfMultipleSurveyIdsShouldBeIncluded()
        {
            var surveyId = 1;
            var otherSurveyId = 2;
            var surveyCondition = GetSurveyCondition(new[] { surveyId, otherSurveyId }, new[] { -1 });
            var surveyFunction = GetSurveyVariableFunction(surveyCondition, FirstGroupId);

            var result = surveyFunction(new ProfileResponseEntity(1, DateTimeOffset.Now, surveyId));
            Assert.That(result, Is.EqualTo(FirstGroupId));
            result = surveyFunction(new ProfileResponseEntity(1, DateTimeOffset.Now, otherSurveyId));
            Assert.That(result, Is.EqualTo(FirstGroupId));
        }

        [Test]
        public void ResponsesShouldOnlyBeIncludedInTheirOwnGroup()
        {
            var surveyId = 1;
            var otherSurveyId = 2;
            var surveyCondition = GetSurveyCondition(new[] { surveyId, otherSurveyId }, new[] { otherSurveyId });
            var surveyFunctionGroupOne = GetSurveyVariableFunction(surveyCondition, FirstGroupId);
            var surveyFunctionGroupTwo = GetSurveyVariableFunction(surveyCondition, SecondGroupId);

            var result = surveyFunctionGroupOne(new ProfileResponseEntity(1, DateTimeOffset.Now, surveyId));
            Assert.That(result, Is.EqualTo(FirstGroupId));
            result = surveyFunctionGroupTwo(new ProfileResponseEntity(1, DateTimeOffset.Now, surveyId));
            Assert.That(result, Is.EqualTo(null));

            result = surveyFunctionGroupOne(new ProfileResponseEntity(1, DateTimeOffset.Now, otherSurveyId));
            Assert.That(result, Is.EqualTo(FirstGroupId));
            result = surveyFunctionGroupTwo(new ProfileResponseEntity(1, DateTimeOffset.Now, otherSurveyId));
            Assert.That(result, Is.EqualTo(SecondGroupId));
        }

        internal static GroupedVariableDefinition GetSurveyCondition(IEnumerable<int> surveyIdsGroupOne, IEnumerable<int> surveyIdsGroupTwo)
        {
            return new()
            {
                ToEntityTypeName = EntityTypeName,
                ToEntityTypeDisplayNamePlural = "SurveysCondition",
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceId = FirstGroupId,
                        ToEntityInstanceName = "FirstSurveysGroup",
                        Component = new SurveyIdVariableComponent
                        {
                            SurveyIds = surveyIdsGroupOne.ToArray()
                        },
                    },
                    new()
                    {
                        ToEntityInstanceId = SecondGroupId,
                        ToEntityInstanceName = "SecondSurveysGroup",
                        Component = new SurveyIdVariableComponent
                        {
                            SurveyIds = surveyIdsGroupTwo.ToArray()
                        },
                    }
                }
            };
        }

        private Func<IProfileResponseEntity, int?> GetSurveyVariableFunction(GroupedVariableDefinition surveyConditionDefinition, int groupId)
        {
            return new SurveyIdVariable(surveyConditionDefinition).CreateForEntityValues(
                new EntityValueCombination(
                new EntityValue(
                    new EntityType(EntityTypeName, EntityTypeName, EntityTypeName),
                    groupId
                )));
        }
    }
}
