namespace UserManagement.Tests.Infrastructure.Repositories.UserFeaturePermissions;

[TestFixture]
public class RoleRepositoryTests
{
    private const string AdminRoleName = "Admin";
    private const string Organisation1 = "Org1";
    private const string OrganisationName2 = "Org2";
    private const string UpdatedByUserId = "admin_123";
    private MetaDataContext? _context;
    private RoleRepository? _repository;

    [SetUp]
    public async Task SetUp()
    {
        var dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new MetaDataContext(dbContextOptions);
        _repository = new RoleRepository(_context);

        var mockData = new List<Role>
        {
            new Role { Id = 1, RoleName = AdminRoleName, OrganisationId = Organisation1, UpdatedByUserId = UpdatedByUserId },
            new Role { Id = 2, RoleName = "User", OrganisationId = Organisation1, UpdatedByUserId = UpdatedByUserId },
            new Role { Id = 3, RoleName = "Manager", OrganisationId = OrganisationName2, UpdatedByUserId = UpdatedByUserId }
        };

        _context.Roles.AddRange(mockData);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _context!.Database.EnsureDeleted();
        _context.Dispose();
        _context = null;
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnRole_WhenRoleExists()
    {
        // Act
        var result = await _repository!.GetByIdAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.RoleName, Is.EqualTo(AdminRoleName));
    }

    [Test]
    public void GetByIdAsync_ShouldThrowInvalidOperationException_WhenRoleDoesNotExist()
    {
        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _repository!.GetByIdAsync(99));
    }

    [Test]
    public async Task GetByOrganisationAsync_ShouldReturnRolesForOrganisation()
    {
        // Act
        var result = await _repository!.GetByOrganisationIdAsync(Organisation1);
        var numberOfDefaultRoles = 4;
        // Assert
        Assert.That(result.Count(), Is.EqualTo(2 + numberOfDefaultRoles));
        Assert.That(result.All(r => r.OrganisationId == Organisation1 || r.OrganisationId == "savanta"));
    }

    [Test]
    public async Task AddAsync_ShouldAddRole()
    {
        // Arrange
        const string Expected = "New Manager";

        // Map EF Role to Domain Role
        var newDomainRole = new UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role(
            Expected,
            OrganisationName2,
            UpdatedByUserId
        );
        
        // Act
        var role = await _repository!.AddAsync(newDomainRole);

        // Assert
        Assert.That(role, Is.Not.Null);
        Assert.That(role.Id, Is.GreaterThan(0));
        Assert.That(role!.RoleName, Is.EqualTo(Expected));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateRole()
    {
        // Arrange
        var existingRole = await _repository!.GetByIdAsync(1);
        const string newName = "Super Admin";
        existingRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
            existingRole.Id,
            newName,
            existingRole.OrganisationId,
            existingRole.UpdatedByUserId
        );

        // Act
        await _repository.UpdateAsync(existingRole);

        // Assert
        var updatedRole = await _context!.Roles.FindAsync(1);
        Assert.That(updatedRole, Is.Not.Null);
        Assert.That(updatedRole!.RoleName, Is.EqualTo(newName));
    }
}