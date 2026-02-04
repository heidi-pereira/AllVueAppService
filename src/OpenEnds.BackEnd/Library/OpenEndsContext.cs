using Microsoft.EntityFrameworkCore;
using OpenEnds.BackEnd.Model;

namespace OpenEnds.BackEnd.Library;

public class OpenEndsContext(DbContextOptions<OpenEndsContext> options) : ReadOnlyBaseDbContext(options)
{
    public DbSet<VueQuestion> Questions { get; set; }
    public DbSet<VueAnswer> Answers { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<SurveyGroup> SurveyGroups { get; set; }
    public DbSet<SurveyGroupSurvey> SurveyGroupSurveys { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VueQuestion>().ToTable("Questions", "vue");

        modelBuilder.Entity<VueAnswer>().ToTable("Answers", "vue").HasNoKey();

        modelBuilder.Entity<SurveyResponse>().ToTable("SurveyResponse");

        modelBuilder.Entity<Survey>().ToTable("Surveys").HasNoKey();
        
        modelBuilder.Entity<SurveyGroup>().ToTable("SurveyGroups").HasNoKey();

        modelBuilder.Entity<SurveyGroupSurvey>().ToTable("SurveyGroupSurveys").HasNoKey();
    }
}