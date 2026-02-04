namespace UserManagement.Tests.Infrastructure.Repositories.UserFeaturePermissions;

public class DbContextFactoryStub : IDbContextFactory<MetaDataContext>
{
    private readonly MetaDataContext _context;

    public DbContextFactoryStub(MetaDataContext context)
    {
        _context = context;
    }

    public MetaDataContext CreateDbContext()
    {
        return _context;
    }
}