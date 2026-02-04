using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    internal class SingleEntityOptimisationTests
    {
        [Test]
        [TestCase(new string[0], "AnswerValue", null, ExpectedResult = false)]
        [TestCase(new[] { "AnswerChoiceId" }, "AnswerValue", null, ExpectedResult = false)]
        [TestCase(new[] { "AnswerChoiceId" }, "AnswerChoiceId", null, ExpectedResult = true)]
        [TestCase(new[] { "AnswerChoiceId" }, "QuestionChoiceId", null, ExpectedResult = false)]
        [TestCase(new[] { "AnswerChoiceId, QuestionChoiceId" }, "AnswerChoiceId", null, ExpectedResult = false)]
        [TestCase(new[] { "AnswerChoiceId" }, "AnswerValue", "AnswerEntityTypeName", ExpectedResult = false)]
        [TestCase(new[] { "AnswerChoiceId" }, "AnswerChoiceId", "AnswerEntityTypeName", ExpectedResult = true)]
        [TestCase(new[] { "AnswerChoiceId" }, "QuestionChoiceId", "AnswerEntityTypeName", ExpectedResult = false)]
        [TestCase(new[] { "AnswerChoiceId, QuestionChoiceId" }, "AnswerChoiceId", "AnswerEntityTypeName", ExpectedResult = false)]
        public bool FieldDefinitionModelHasSingleDimension(IEnumerable<string> entityColumnNames, string valueColumnName, string valueEntityIdentifier)
        {
            var entityDefinitionModels = entityColumnNames.Select(name =>
            {
                var entityTypeName = new string(name.Skip(1).TakeWhile(c => char.IsLower(c)).Prepend(name[0]).Concat("EntityTypeName").ToArray());
                var entityType = new EntityType(entityTypeName, entityTypeName, entityTypeName);
                return new EntityFieldDefinitionModel(name, entityType, entityType.Identifier);
            }).ToArray();
            var fieldDefinitionModel = new FieldDefinitionModel(
                "testfield",
                "dbo",
                "table1",
                valueColumnName,
                "question",
                null,
                "varcode",
                EntityInstanceColumnLocation.CH1,
                valueEntityIdentifier,
                false,
                null,
                entityDefinitionModels,
                null);
            var field = new ResponseFieldDescriptor(fieldDefinitionModel.Name, entityDefinitionModels.Select(e => e.EntityType).ToArray());
            field.AddDataAccessModelForSubset(TestResponseFactory.AllSubset.Id, fieldDefinitionModel);

            var definitionIsSingleDimension = fieldDefinitionModel.OnlyDimensionIsEntityType();
            var fieldIsSingleDimension = field.OnlyDimensionIsEntityType();
            Assert.That(fieldIsSingleDimension, Is.EqualTo(definitionIsSingleDimension));
            return definitionIsSingleDimension;
        }
    }
}
