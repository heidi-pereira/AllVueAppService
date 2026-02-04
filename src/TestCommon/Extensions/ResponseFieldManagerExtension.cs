using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using TestCommon.DataPopulation;

namespace TestCommon.Extensions
{
    public static class ResponseFieldManagerExtension
    {
        public static ResponseFieldDescriptor[] AddAllWithTypes(this ResponseFieldManager manager, EntityType[] types, params string[] fieldNames)
        {
            return fieldNames.Select(fieldName => Add(manager, fieldName, types)).ToArray();
        }

        public static ResponseFieldDescriptor Add(this ResponseFieldManager manager, string fieldName, params EntityType[] types)
        {
            var field = manager.Add(fieldName, TestResponseFactory.AllSubset, types);
            Assert.That(types, Is.EquivalentTo(field.EntityCombination), "Setup issue: Attempted to add an existing field with different entities");
            return field;
        }

        public static ResponseFieldDescriptor Add(this ResponseFieldManager manager, string fieldName, Subset subset, params EntityType[] types)
        {
            return manager.Add(fieldName, subset.Id, false, types);
        }

        public static ResponseFieldDescriptor Add(this ResponseFieldManager manager, string fieldName, string subsetId, params EntityType[] types)
        {
            return manager.Add(fieldName, subsetId, false, types);
        }

        public static ResponseFieldDescriptor Add(this ResponseFieldManager manager, string fieldName, string subsetId, bool isRadio, params EntityType[] types)
        {
            var questionType = isRadio ? "RADIO" : "VALUE";
            return Add(manager, fieldName, subsetId, questionType, types);
        }

        public static ResponseFieldDescriptor Add(this ResponseFieldManager manager, string fieldName, string subsetId, string questionType, params EntityType[] types)
        {
            var question = new Question() { MasterType = questionType };
            return Add(manager, fieldName, subsetId, question, types);
        }

        public static ResponseFieldDescriptor Add(this ResponseFieldManager manager, string fieldName, string subsetId, Question question, params EntityType[] types)
        {
            var entityModels = types.Select(type => new EntityFieldDefinitionModel("col", type, type.Identifier));
            var fieldDefinitionModel = new FieldDefinitionModel(fieldName, "dbo", "table1", fieldName, "", null, fieldName + "VarCode", EntityInstanceColumnLocation.optValue, "", false, null, entityModels, null)
            {
                QuestionModel = question
            };

            return manager.LazyLoad((SubsetId: subsetId, Model: fieldDefinitionModel)).Single();
        }
    }
}