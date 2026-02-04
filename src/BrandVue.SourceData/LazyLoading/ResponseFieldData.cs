namespace BrandVue.SourceData.LazyLoading
{
    public class ResponseFieldData
    {
        public ResponseFieldData(int responseId, DateTimeOffset timestamp, int surveyId, IReadOnlyDictionary<ResponseFieldDescriptor, int> fieldValues)
        {
            ResponseId = responseId;
            Timestamp = timestamp;
            SurveyId = surveyId;
            FieldValues = fieldValues;
        }

        public int ResponseId { get; }
        public DateTimeOffset Timestamp { get; }
        public int SurveyId { get; }
        public IReadOnlyDictionary<ResponseFieldDescriptor, int> FieldValues { get; }
    }
}