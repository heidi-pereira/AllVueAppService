using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.ResponseRepository
{
    [Keyless]
    public record RespondentWeight(int ResponseId, decimal Weight);

    public class ResponseDataContext : DbContext
    {
        public ResponseDataContext(DbContextOptions<ResponseDataContext> builderOptions): base(builderOptions) { }

        public DbSet<WeightedWordCount> WeightedWordCounts { get; set; }
        public DbSet<RawTextResponse> RawTextResponses { get; set; }
        public DbSet<TextResponse> TextResponses { get; set; }
        public DbSet<AnswerTextResponse> AnswerTextResponses { get; set; }
        public DbSet<HeatmapResponse> HeatmapResponses { get; set; }
        public DbSet<RespondentWeight> RespondentWeights { get; set; }
    }
}
