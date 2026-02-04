using System;
using System.Collections.Generic;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;

namespace TestCommon.DataPopulation
{
    public class TestAnswer
    {
        public ResponseFieldDescriptor Field { get; }
        public EntityValueCombination EntityValues { get; }
        public int FieldValue { get; }

        public TestAnswer(ResponseFieldDescriptor field, int fieldValue, params EntityValue[] entityValues)
        {
            Field = field;
            EntityValues = new EntityValueCombination(entityValues);
            FieldValue = fieldValue;
        }

        public static TestAnswer For(ResponseFieldDescriptor field, int fieldValue, params EntityValue[] entityValues)
        {
            return new TestAnswer(field, fieldValue, entityValues);
        }
    }

    public record ResponseAnswers(IReadOnlyCollection<TestAnswer> Answers, DateTimeOffset? Timestamp = null, int? SurveyId = null);
}