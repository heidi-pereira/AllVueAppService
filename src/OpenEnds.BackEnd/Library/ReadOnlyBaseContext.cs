using Microsoft.EntityFrameworkCore;

namespace OpenEnds.BackEnd.Library;

    public abstract class ReadOnlyBaseDbContext(DbContextOptions options) : DbContext(options)
{
    // Make whole context read only for now
    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted || entry.State == EntityState.Added)
            {
                entry.State = EntityState.Unchanged;
            }
        }

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted || entry.State == EntityState.Added)
            {
                entry.State = EntityState.Unchanged;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
