using BrandVue.EntityFramework.Exceptions;
using Newtonsoft.Json;


namespace BrandVue.SourceData.LlmInsights
{
    public class LlmInsightsUserFeedback
    {

        [JsonProperty("CreatedDt")]
        public DateTime CreatedDt { get; init; }
        [JsonProperty("UserId")]
        public string UserId { get; init; }
        [JsonProperty("UserComment")]
        public string UserComment { get; protected set; }
        [JsonProperty("IsUseful")]
        public bool? IsUseful { get; protected set; }

        private readonly Dictionary<int, bool?> _segmentCorrectness;
        public IReadOnlyDictionary<int, bool?> SegmentCorrectness => _segmentCorrectness.AsReadOnly();


        public LlmInsightsUserFeedback(string userId, Dictionary<int, bool?> segmentCorrectness)
        {
            CreatedDt = DateTime.UtcNow;
            UserId = userId;
            _segmentCorrectness = segmentCorrectness;
        }

        [JsonConstructor]
        public LlmInsightsUserFeedback(
            [JsonProperty("CreatedDt")] DateTime createdDt,
            [JsonProperty("UserId")] string userId,
            [JsonProperty("UserComment")] string userComment,
            [JsonProperty("IsUseful")] bool? isUseful,
            [JsonProperty("SegmentCorrectness")] Dictionary<int, bool?> segmentCorrectness
            )
        {
            CreatedDt = createdDt;
            UserId = userId;
            UserComment = userComment;
            IsUseful = isUseful;
            _segmentCorrectness = segmentCorrectness;
        }


        public void UpdateUserComment(string userComment)
        {
            // TODO - Decide on the text limit
            if (!string.IsNullOrEmpty(userComment) && userComment.Length > 1024)
                throw new BadRequestException("User comment is too long. Max length is 1024.");

            UserComment = userComment;
        }

        public void UpdateUserUsefulness(bool? isUseful)
        {
            if (IsUseful.Equals(isUseful))
                return;

            IsUseful = isUseful;
        }

        public void UpdateUserSegmentCorrectness(int segmentId, bool? isCorrect)
        {
            if (!_segmentCorrectness.ContainsKey(segmentId))
                throw new BadRequestException($"SegmentId:{segmentId} is not valid for this AI Summary");

            _segmentCorrectness[segmentId] = isCorrect;
        }

 

    }
}
