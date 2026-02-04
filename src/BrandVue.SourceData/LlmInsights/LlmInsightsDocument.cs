using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;


#nullable enable

namespace BrandVue.SourceData.LlmInsights
{
    public class LlmInsightsDocument
    {

        public int SchemaVersion { get; init; } = 1;
        [JsonProperty("id")]
        public string Id { get; init; }
        public DateTime Created { get; init; } 
        public object Request { get; init; } 
        public object[] AverageRequests { get; set; }
        public LlmInsightsSegment[]? AiSummary { get; init; } 

        private readonly List<LlmInsightsUserFeedback> _userFeedback;
        public IReadOnlyList<LlmInsightsUserFeedback> UserFeedback => _userFeedback.AsReadOnly();


        public LlmInsightsDocument(object request, IReadOnlyCollection<object> averageRequests, string hash, LlmInsightsSegment[]? aiSummary)
        {
            var array = averageRequests.ToArray();
            Id = hash;
            Request = request;
            AverageRequests = array.ToArray();
            AiSummary = aiSummary;
            Created = DateTime.UtcNow;
            _userFeedback = new();
        }

        

        // Constructor for mapping document in CosmosDb to LlmInsightsDocument class
        [JsonConstructor]
        public LlmInsightsDocument(
            [JsonProperty("SchemaVersion")] int schemaVersion,
            [JsonProperty("id")] string id,
            [JsonProperty("Created")] DateTime created,
            [JsonProperty("Request")] object request,
            [JsonProperty("AiSummary")] LlmInsightsSegment[] aiSummary,
            [JsonProperty("UserFeedback")] List<LlmInsightsUserFeedback> userFeedback)
        {
            SchemaVersion = schemaVersion;
            Id = id;
            Request = request;
            AiSummary = aiSummary;
            Created = created;
            _userFeedback = userFeedback;
        }


        public void UpdateFeedbackUserComment(string userId, string userComment)
        {
            GetOrCreateUserFeedback(userId).UpdateUserComment(userComment);
        }
        public void UpdateFeedbackUserUsefulness(string userId, bool? isUseful)
        {
            GetOrCreateUserFeedback(userId).UpdateUserUsefulness(isUseful);
        }

        public void UpdateUserFeedbackSegmentCorrectness(string userId, int segmentId, bool? isCorrect)
        {
            GetOrCreateUserFeedback(userId).UpdateUserSegmentCorrectness(segmentId, isCorrect);
        }

        public LlmInsightsUserFeedback? GetUserFeedback(string userId)
        {
            return _userFeedback.FirstOrDefault(a => a.UserId == userId);
        }

        private LlmInsightsUserFeedback GetOrCreateUserFeedback(string userId)
        {
            var userFeedback = GetUserFeedback(userId);

            if (userFeedback is not null)
                return userFeedback;

            userFeedback = new LlmInsightsUserFeedback(
                userId,
                AiSummary?.ToDictionary(x => x.SegmentId, x => (bool?)null));

            _userFeedback.Add(userFeedback);

            return userFeedback;
        }

    }

}
