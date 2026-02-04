using Microsoft.EntityFrameworkCore;
using OpenEnds.BackEnd.Model;

namespace OpenEnds.BackEnd.Library;

public class MetadataContext(DbContextOptions<MetadataContext> options) : ReadOnlyBaseDbContext(options)
{
    public DbSet<AllVueConfiguration> AllVueConfigurations { get; set; }
    public DbSet<VariableConfiguration> VariableConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllVueConfiguration>().ToTable("AllVueConfigurations");
        modelBuilder.ApplyConfiguration(new VariableConfigurationConfiguration());
    }
}