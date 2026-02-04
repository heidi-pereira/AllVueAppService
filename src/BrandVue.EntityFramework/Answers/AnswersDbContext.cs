using System.Linq;
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.Answers
{
    public class AnswersDbContext : DbContext
    {
        public DbSet<ChoiceSet> ChoiceSets { get; set; }
        public DbSet<Choice> Choices { get; set; }
        public virtual DbSet<Question> Questions { get; set; }
        public DbSet<SurveySegment> SurveySegments { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public virtual DbSet<Answer> Answers { get; set; }
        public DbSet<AnswerStat> AnswerStats { get; set; }
        public virtual DbSet<Surveys> Surveys { get; set; }
        public DbSet<KimbleProposals> KimbleProposals { get; set; }

        public virtual DbSet<SurveyGroup> SurveyGroups { get; set; }
        public virtual DbSet<SurveySharedOwner> SurveySharedOwners { get; set; }
        public DbSet<SurveyGroupSurveys> SurveyGroupSurveys { get; set; }

        public AnswersDbContext() { }
        public AnswersDbContext(DbContextOptions builderOptions) : base(builderOptions) { }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("vue");

            modelBuilder.Entity<ChoiceSet>(b =>
            {
                b.HasIndex(i => new {i.SurveyId, i.Name}).IsUnique();
                b.HasOne(c => c.ParentChoiceSet1).WithMany(c => c.DirectDescendants)
                    .HasForeignKey(c => c.ParentChoiceSet1Id);
                b.HasOne(c => c.ParentChoiceSet2).WithMany(c => c.AddedDescendants)
                    .HasForeignKey(c => c.ParentChoiceSet2Id);
            });

            modelBuilder.Entity<Choice>().HasIndex(i => new {i.ChoiceSetId, i.SurveyChoiceId}).IsUnique();

            modelBuilder.Entity<Question>().HasIndex(i => new {i.SurveyId, i.VarCode}).IsUnique();
            modelBuilder.Entity<Question>().Property(b => b.OptionalData).HasJsonConversion();
            modelBuilder.Entity<AnswerStat>().HasNoKey();

            modelBuilder.Entity<SurveyGroupSurveys>().HasKey(s => new {s.SurveyGroupId, s.SurveyId});

            //TODO Make this whole thing readonly: e.g. create an IReadableAnswerDbContext which doesn't have save changes
            //TODO Always use credentials that can only have read permissions on this database

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                const string connectionString = "Server=(localdb)\\mssqllocaldb;Database=ExtractionDb;TrustServerCertificate=True;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;Encrypt=True;";
                optionsBuilder.UseSqlServer(connectionString);
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
            }
        }

        public AnswerStat[] GetAnswerStats(IReadOnlyCollection<int> segmentIds, IReadOnlyCollection<int> surveyIds)
        {
            return SurveyResponses
                .Where(sr =>
                    sr.Status == SurveyCompletionStatus.Completed && sr.Archived == false &&
                    surveyIds.Contains(sr.SurveyId) && segmentIds.Contains(sr.SegmentId))
                .GroupBy(sr => sr.SegmentId)
                .Select(a => new AnswerStat() { SegmentId = a.Key, ResponseCount = a.Count() })
                .ToArray();
        }
    }
}
