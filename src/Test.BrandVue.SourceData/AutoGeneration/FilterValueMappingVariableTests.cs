using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace Test.BrandVue.SourceData.AutoGeneration
{
    public class FilterValueMappingVariableTests
    {
        private static readonly EntityType Gender = new("Gender", "Gender", "Genders");
        private static readonly EntityValue[] GenderEntities = Enumerable.Range(0, 4).Select(id => new EntityValue(Gender, id)).ToArray();
        private ResponseFieldDescriptor _genderField;
        private Measure _genderMeasure;

        [Test]
        public void ShouldCreateVariableForFilterValueMapping()
        {
            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(default, default).ReturnsForAnyArgs(args => args.Arg<Measure>());

            var testResponseEntityTypeRepository = new TestEntityTypeRepository(Gender);
            var fieldManager = new ResponseFieldManager(testResponseEntityTypeRepository);
            var responseFactory = new TestResponseFactory(fieldManager);
            _genderField = fieldManager.Add(Gender.Identifier, Gender);
            _genderMeasure = new Measure
            {
                Name = nameof(Gender),
                CalculationType = CalculationType.YesNo,
                BaseField = _genderField,
                LegacyBaseValues = { Values = new[] { 0,1,2,3 } },
                Field = _genderField,
                LegacyPrimaryTrueValues = { Values = GenderEntities.Select(e => e.Value).ToArray() },
                FilterValueMapping = "0:M|1:F|2:O"
            };
            var genderEntityToCount = new Dictionary<int, int>
            {
                [0] = 3,
                [1] = 4,
                [2] = 2,
                [3] = 1,
            };
            var responses = new List<ResponseAnswers>();
            foreach (var entity in genderEntityToCount.Keys)
            {
                var count = genderEntityToCount[entity];
                for (var i = 0; i < count; i++)
                {
                    var answer = new[] { TestAnswer.For(_genderField, entity, GenderEntities[entity]) };
                    responses.Add(new ResponseAnswers(answer));
                }
            }

            var calculator = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { _genderMeasure })
                .IncludeEntities(GenderEntities)
                .WithResponses(responses.ToArray())
                .BuildRealCalculatorWithInMemoryDb();
            var subset = calculator.DataLoader.SubsetRepository.Single();

            var loadedMeasure = calculator.DataLoader.MeasureRepository.Get(_genderMeasure.Name);
            Assert.That(loadedMeasure.FilterValueMappingVariable, Is.Not.Null);
            Assert.That(loadedMeasure.FilterValueMappingVariableConfiguration, Is.Not.Null);
            Assert.That(loadedMeasure.FilterValueMappingVariableConfiguration.Definition is GroupedVariableDefinition, Is.True);

            var response_M = responseFactory.CreateResponse(DateTime.Now, -1, responses.First(r => r.Answers.Single().FieldValue == 0).Answers).ProfileResponse;
            var response_F = responseFactory.CreateResponse(DateTime.Now, -1, responses.First(r => r.Answers.Single().FieldValue == 1).Answers).ProfileResponse;
            var response_OtherA = responseFactory.CreateResponse(DateTime.Now, -1, responses.First(r => r.Answers.Single().FieldValue == 2).Answers).ProfileResponse;
            var response_OtherB = responseFactory.CreateResponse(DateTime.Now, -1, responses.First(r => r.Answers.Single().FieldValue == 3).Answers).ProfileResponse;

            var variableFunc = loadedMeasure.FilterValueMappingVariable.CreateForSingleEntity(_ => true);
            calculator.DataLoader.ResponseFieldManager.Get(_genderField.Name).EnsureLoadOrderIndexInitialized_ThreadUnsafe();
            Assert.That(variableFunc(response_M).Span[0], Is.EqualTo(0));
            Assert.That(variableFunc(response_F).Span[0], Is.EqualTo(1));
            Assert.That(variableFunc(response_OtherA).Span[0], Is.EqualTo(2));
            Assert.That(variableFunc(response_OtherB).Span.Length, Is.EqualTo(0));
        }
    }
}
