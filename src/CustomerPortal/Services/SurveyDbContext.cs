using System;
using System.Collections.Generic;
using System.Linq;
using CustomerPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerPortal.Services
{
    public class SurveyDbContext : DbContext
    {
        public SurveyDbContext(DbContextOptions<SurveyDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("customerportal");

            modelBuilder.Entity<SurveyGroupSurvey>()
                .HasKey(x => new { x.SurveyGroupId, x.SurveyId });
            modelBuilder.Entity<SurveySharedOwner>()
                .HasKey(x => new { x.Id});
        }

        public override int SaveChanges()
        {
            if (!Database.IsInMemory())
            {
                throw new InvalidOperationException("This context is read-only.");
            }

            return base.SaveChanges();
        }

        public void Initialise(AppSettings appSettings)
        {
            var random = new Random();
            if (!Surveys.Any())
            {
                var next = 5 + random.Next(30);
                for (var i = 1; i < next; i++)
                {
                    var totalQuota = GetSurveyQuotaCell("Total");

                    var startDate = i == 1 ? DateTime.Today : RandomDate();
                    var name = i == 1 ? "TestSurvey" : SurveyName();
                    var status = i == 1 ? 1 : IsOpen();
                    var surveyModel = new Survey
                    {
                        Id = i,
                        InternalName = name,
                        Name = name,
                        LaunchDate = startDate,
                        CompleteDate = startDate.AddDays(random.Next(365)),
                        Status = status,
                        Complete = totalQuota[0].Complete,
                        Target = totalQuota[0].Target,
                        FileDownloadGuid = Guid.NewGuid(),
                        AuthCompanyId = null,
                        Quota = new List<Quota>(new[]
                        {
                                new Quota
                                {
                                    Name = "Total",
                                    QuotaCells = totalQuota
                                },
                                new Quota
                                {
                                    Name = "Gender",
                                    QuotaCells = GetSurveyQuotaCell("Male","Female", "Other")
                                },
                                new Quota
                                {
                                    Name = "Age",
                                    QuotaCells = GetSurveyQuotaCell("16-24", "25-34", "35-59", "60+")
                                },
                                new Quota
                                {
                                    Name = "Region",
                                    QuotaCells = GetSurveyQuotaCell("North", "South", "East")
                                }
                            })
                    };

                    Surveys.Add(surveyModel);
                }
                SaveChanges();

            }

            DateTime RandomDate()
            {
                var start = new DateTime(2018, 1, 1);
                var range = (DateTime.Today - start).Days;
                return start.AddDays(random.Next(range));
            }

            string SurveyName()
            {
                var name = $"{Faker.Company.Industry()} {Faker.Company.Sector()} {Faker.Company.CatchPhraseMid()} Survey";
                return name;
            }

            int IsOpen()
            {
                return random.Next(2);
            }

            List<QuotaCell> GetSurveyQuotaCell(params string[] name)
            {
                var result = new List<QuotaCell>();
                foreach (var n in name)
                {
                    var target = random.Next(2000);
                    result.Add(new QuotaCell
                    {
                        Name = n,
                        Target = target,
                        Complete = random.Next(target)
                    });
                }

                return result;
            }
        }

        public DbSet<Survey> Surveys { get; set; }
        public DbSet<SurveyGroup> SurveyGroups { get; set;  }
        public DbSet<SurveyGroupSurvey> SurveyGroupSurveys { get; set; }
        public DbSet<SurveySharedOwner> SurveySharedOwners { get; set; }

        public DbSet<Quota> Quotas { get; set; }

    }
}
