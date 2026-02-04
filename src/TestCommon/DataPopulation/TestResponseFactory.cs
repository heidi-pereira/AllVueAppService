using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.SourceData;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace TestCommon.DataPopulation
{
    public class TestResponseFactory
    {
        private int _id;
        private readonly Subset _subset;

        public static Subset AllSubset { get; } = new Subset { Id = "All", Iso2LetterCountryCode = "GB"};

        public TestResponseFactory(IResponseFieldManager responseFieldManager, Subset subset = null)
        {
            ResponseFieldManager = responseFieldManager;
            _subset = subset ?? AllSubset;
        }

        public IResponseFieldManager ResponseFieldManager { get; }

        public IEnumerable<(ProfileResponseEntity ProfileResponse, List<EntityMetricData> EntityMeasureData)> CreateTestResponses(DateTimeOffset dateOfFirstResponse,
                DateTimeOffset dateOfLastResponse, TestAnswer[][] responsesWithAnswers, int surveyId = -1)
        {
            double minutesCount = (dateOfLastResponse - dateOfFirstResponse).TotalMinutes;
            int numberOfResponses = responsesWithAnswers.Length;
            double minutesBetweenResponses = minutesCount / numberOfResponses;

            return responsesWithAnswers.Select((answers, responseIndex) =>
            {
                int multiplier = 1 + responseIndex - numberOfResponses; //Start from the end to make sure the period is considered complete
                var minutesToAdd = multiplier * minutesBetweenResponses;
                var responseDate = dateOfLastResponse.AddMinutes(minutesToAdd).ToDateInstance();
                return CreateResponse(responseDate, surveyId, answers);

            });
        }

        public (ProfileResponseEntity ProfileResponse, List<EntityMetricData> EntityMeasureData) CreateResponse(DateTimeOffset timestamp, int surveyId, IReadOnlyCollection<TestAnswer> answers)
        {
            int responseId = _id++;
            var profileResponse = new ProfileResponseEntity(responseId, timestamp, surveyId);
            var emds = new List<EntityMetricData>();

            foreach (var answer in answers)
            {
                emds.Add(new EntityMetricData
                {
                    ResponseId = responseId, 
                    EntityIds = answer.EntityValues.EntityIds,
                    Measures = (answer.Field, answer.FieldValue).Yield().ToList(), 
                    Timestamp = timestamp,
                    SurveyId = 0
                });

                answer.Field.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
                profileResponse.AddFieldValue(answer.Field, answer.EntityValues.EntityIds, answer.FieldValue,
                    _subset);
            }

            return (profileResponse, emds);
        }

        public ProfileResponseEntity WithFieldValues(ProfileResponseEntity profileResponse, IEnumerable<(string FieldName, int Value, IEnumerable<EntityValue> EntityValues)> fieldValues, Subset subset = null)
        {
            var fakeSubset = subset ?? _subset;
            foreach (var fieldValue in fieldValues)
            {
                var field = ResponseFieldManager.Get(fieldValue.FieldName);
                field.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
                profileResponse.AddFieldValue(field, new EntityValueCombination(fieldValue.EntityValues).EntityIds,
                    fieldValue.Value, fakeSubset);
            }

            return profileResponse;
        }
    }
}