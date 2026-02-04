using UserManagement.BackEnd.Domain.UserFeaturePermissions;

namespace UserManagement.Tests.Infrastructure.Repositories.UserFeaturePermissions;

[TestFixture]
public class UserFeaturePermissionRepositoryTests
{
    private const string UserId1 = "User1";
    private const string UserId2 = "User2";
    private const string UserId3 = "User3";
    private const string UpdatedByUserId = "admin_123";
    private const string DefaultOrg = "DefaultOrg";

    private MetaDataContext? _context;
    private UserFeaturePermissionRepository? _repository;
    private List<UserFeaturePermission>? _mockData;
    private Role _role = new()
    {
        Id = 1,
        RoleName = "Admin",
        UpdatedByUserId = UpdatedByUserId,
        OrganisationId = DefaultOrg
    };

    [SetUp]
    public void SetUp()
    {
        var dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _context = new MetaDataContext(dbContextOptions);
        _repository = new UserFeaturePermissionRepository(_context);

        _mockData = new List<UserFeaturePermission>
        {
            new() { Id = 1, UserId = UserId1, UpdatedByUserId = UpdatedByUserId, UserRole = _role },
            new() { Id = 2, UserId = UserId2, UpdatedByUserId = UpdatedByUserId, UserRole = _role },
            new() { Id = 3, UserId = UserId3, UpdatedByUserId = UpdatedByUserId, UserRole = _role }
        };

        _context.Set<UserFeaturePermission>().AddRange(_mockData);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context!.Database.EnsureDeleted();
        _context.Dispose();
        _context = null;
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnPermission_WhenPermissionExists()
    {
        // Act
        var result = await _repository!.GetByUserIdAsync(UserId1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(UserId1));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenPermissionDoesNotExist()
    {
        // Act
        var result = await _repository!.GetByUserIdAsync("NonExistentUser");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllPermissions()
    {
        // Act
        var result = await _repository!.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(_mockData!.Count));
    }

    [Test]
    public async Task AddAsync_ShouldAddPermission()
    {
        // Arrange
        var newPermission = new UserFeaturePermission { Id = 4, UserId = "NewTestUser", UpdatedByUserId = UpdatedByUserId, UserRole = _role };
        // Act
        await _repository!.AddAsync(UserFeaturePermissionMapper.MapFromInfrastructure(newPermission));
        await _context!.SaveChangesAsync();

        // Assert
        var addedPermission = await _context.Set<UserFeaturePermission>()
            .FirstOrDefaultAsync(p => p.UserId == "NewTestUser");
        Assert.That(addedPermission, Is.Not.Null);
        Assert.That(addedPermission!.UserId, Is.EqualTo("NewTestUser"));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdatePermission()
    {
        // Arrange
        var existingPermission = _mockData!.First();
        existingPermission.UserId = "UpdatedUser";

        // Act
        await _repository!.UpdateAsync(UserFeaturePermissionMapper.MapFromInfrastructure(existingPermission));
        await _context!.SaveChangesAsync();

        // Assert
        var updatedPermission = await _context.Set<UserFeaturePermission>().FindAsync(existingPermission.Id);
        Assert.That(updatedPermission, Is.Not.Null);
        Assert.That(updatedPermission!.UserId, Is.EqualTo("UpdatedUser"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemovePermission_WhenPermissionExists()
    {
        // Arrange
        var permissionToDelete = _mockData!.First();

        // Act
        await _repository!.DeleteAsync(permissionToDelete.Id);
        await _context!.SaveChangesAsync();

        // Assert
        var deletedPermission = await _context.Set<UserFeaturePermission>().FindAsync(permissionToDelete.Id);
        Assert.That(deletedPermission, Is.Null);
    }

    [Test]
    public async Task DeleteByUserIdAsync_ShouldDeleteAllPermissionsForUser()
    {
        // Arrange
        var userIdToDelete = UserId1;
        var initialCount = _mockData!.Count();

        // Act
        await _repository!.DeleteByUserIdAsync(userIdToDelete);

        // Assert
        var remainingPermissions = await _context!.Set<UserFeaturePermission>()
            .Where(p => p.UserId == userIdToDelete)
            .ToListAsync();
        
        Assert.That(remainingPermissions, Is.Empty);
        
        var totalRemainingCount = await _context.Set<UserFeaturePermission>().CountAsync();
        Assert.That(totalRemainingCount, Is.LessThan(initialCount));
    }

    [Test]
    public async Task DeleteByUserIdAsync_WhenUserHasNoPermissions_ShouldNotThrow()
    {
        // Arrange
        var nonExistentUserId = "NonExistentUser";

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _repository!.DeleteByUserIdAsync(nonExistentUserId));
    }

    [Test]
    public async Task UpsertAsync_ShouldUpdatePermission_WhenPermissionExists()
    {
        // Arrange
        var existingUserId = UserId1;
        var newRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
            2, "Updated Role", DefaultOrg, UpdatedByUserId
        );
        
        // Ensure the role exists in context for the repository to find
        var newRoleInfra = new Role
        {
            Id = 2,
            RoleName = "Updated Role",
            OrganisationId = DefaultOrg,
            UpdatedByUserId = UpdatedByUserId,
            UpdatedDate = DateTime.UtcNow
        };
        _context!.Set<Role>().Add(newRoleInfra);
        await _context.SaveChangesAsync();

        var updatedPermission = new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
            existingUserId,
            newRole,
            "updater_123"
        );

        // Act
        var result = await _repository!.UpsertAsync(updatedPermission);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(existingUserId));
        Assert.That(result.UserRoleId, Is.EqualTo(2));
        Assert.That(result.UpdatedByUserId, Is.EqualTo("updater_123"));
        
        // Verify only one permission exists for this user
        var allPermissionsForUser = await _context.Set<UserFeaturePermission>()
            .Where(p => p.UserId == existingUserId)
            .ToListAsync();
        Assert.That(allPermissionsForUser.Count, Is.EqualTo(1));
        Assert.That(allPermissionsForUser.First().UserRoleId, Is.EqualTo(2));
    }

    [Test]
    public async Task UpsertAsync_ShouldAddPermission_WhenPermissionDoesNotExist()
    {
        // Arrange
        var newUserId = "NewUser123";
        var newRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
            3, "New Role", DefaultOrg, UpdatedByUserId
        );
        
        // Ensure the role exists in context for the repository to find
        var newRoleInfra = new Role
        {
            Id = 3,
            RoleName = "New Role",
            OrganisationId = DefaultOrg,
            UpdatedByUserId = UpdatedByUserId,
            UpdatedDate = DateTime.UtcNow
        };
        _context!.Set<Role>().Add(newRoleInfra);
        await _context.SaveChangesAsync();

        var newPermission = new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
            newUserId,
            newRole,
            "creator_456"
        );

        var initialCount = await _context.Set<UserFeaturePermission>().CountAsync();

        // Act
        var result = await _repository!.UpsertAsync(newPermission);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.UserId, Is.EqualTo(newUserId));
        Assert.That(result.UserRoleId, Is.EqualTo(3));
        Assert.That(result.UpdatedByUserId, Is.EqualTo("creator_456"));
        
        // Verify total count increased
        var finalCount = await _context.Set<UserFeaturePermission>().CountAsync();
        Assert.That(finalCount, Is.EqualTo(initialCount + 1));
        
        // Verify the permission was actually added to database
        var addedPermission = await _context.Set<UserFeaturePermission>()
            .FirstOrDefaultAsync(p => p.UserId == newUserId);
        Assert.That(addedPermission, Is.Not.Null);
        Assert.That(addedPermission!.UserRoleId, Is.EqualTo(3));
    }

    [Test]
    public void UpsertAsync_ShouldThrowException_WhenRoleDoesNotExist()
    {
        // Arrange
        var nonExistentRoleId = 999;
        var nonExistentRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
            nonExistentRoleId, "Non Existent Role", DefaultOrg, UpdatedByUserId
        );
        
        var permissionWithInvalidRole = new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
            "TestUser",
            nonExistentRole,
            UpdatedByUserId
        );

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await _repository!.UpsertAsync(permissionWithInvalidRole));
        
        Assert.That(ex!.Message, Does.Contain($"Role with ID {nonExistentRoleId} not found"));
    }

    [Test]
    public async Task UpsertAsync_ShouldUpdateTimestamp_WhenPermissionExists()
    {
        // Arrange
        var existingUserId = UserId2;
        var originalPermission = await _context!.Set<UserFeaturePermission>()
            .FirstAsync(p => p.UserId == existingUserId);
        var originalTimestamp = originalPermission.UpdatedDate;
        
        // Wait a bit to ensure timestamp difference
        await Task.Delay(50);
        
        var updatedPermission = new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
            existingUserId,
            new BackEnd.Domain.UserFeaturePermissions.Entities.Role(1, "Admin", DefaultOrg, UpdatedByUserId),
            "timestamp_tester"
        );

        // Act
        var result = await _repository!.UpsertAsync(updatedPermission);

        // Assert
        Assert.That(result.UpdatedDate, Is.GreaterThan(originalTimestamp));
        Assert.That(result.UpdatedByUserId, Is.EqualTo("timestamp_tester"));
    }

    [Test]
    public async Task UpsertAsync_ShouldHandleRoleWithOptions_WhenUpdatingPermission()
    {
        // Arrange
        var existingUserId = UserId3;
        
        // Create a role with options in the database
        var permissionFeature = new PermissionFeature
        {
            Id = 1,
            Name = "Test Feature",
            SystemKey = BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue
        };
        
        var permissionOption = new PermissionOption
        {
            Id = 1,
            Name = "Test Option",
            Feature = permissionFeature
        };
        
        var roleWithOptions = new Role
        {
            Id = 4,
            RoleName = "Role With Options",
            OrganisationId = DefaultOrg,
            UpdatedByUserId = UpdatedByUserId,
            UpdatedDate = DateTime.UtcNow,
            Options = new List<PermissionOption> { permissionOption }
        };
        
        _context!.Set<PermissionFeature>().Add(permissionFeature);
        _context.Set<PermissionOption>().Add(permissionOption);
        _context.Set<Role>().Add(roleWithOptions);
        await _context.SaveChangesAsync();

        var domainRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
            4, "Role With Options", DefaultOrg, UpdatedByUserId
        );
        
        var updatedPermission = new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
            existingUserId,
            domainRole,
            "options_tester"
        );

        // Act
        var result = await _repository!.UpsertAsync(updatedPermission);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserRoleId, Is.EqualTo(4));
        Assert.That(result.UpdatedByUserId, Is.EqualTo("options_tester"));
        
        // Verify the role with options was properly assigned
        var updatedDbPermission = await _context.Set<UserFeaturePermission>()
            .Include(p => p.UserRole)
                .ThenInclude(r => r.Options)
                    .ThenInclude(o => o.Feature)
            .FirstAsync(p => p.UserId == existingUserId);
        
        Assert.That(updatedDbPermission.UserRole.Options.Count, Is.EqualTo(1));
        Assert.That(updatedDbPermission.UserRole.Options.First().Name, Is.EqualTo("Test Option"));
    }
}