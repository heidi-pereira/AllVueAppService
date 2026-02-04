using Microsoft.EntityFrameworkCore;

namespace VueReporting.Models
{
    public class ReportRepository : DbContext
    {
        public ReportRepository()
        {
        }

        public ReportRepository(DbContextOptions<ReportRepository> options) : base(options)
        {
        }

        public DbSet<ReportTemplate> ReportTemplates { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PowerPointFileData>()
                .ToTable("ReportTemplates");

            modelBuilder.Entity<ReportTemplate>()
                .ToTable("ReportTemplates")
                .HasOne(r => r.PowerPointFileData)
                .WithOne(r => r.ReportTemplate)
                .HasForeignKey<PowerPointFileData>(r=>r.Id);
        }
    }
}