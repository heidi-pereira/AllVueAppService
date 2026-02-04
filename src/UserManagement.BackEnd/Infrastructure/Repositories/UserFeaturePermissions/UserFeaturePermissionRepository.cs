using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions;
using Microsoft.EntityFrameworkCore;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserFeaturePermissions;
using Role = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.Role;

namespace UserManagement.BackEnd.Infrastructure.Repositories.UserFeaturePermissions;

public class UserFeaturePermissionRepository : IUserFeaturePermissionRepository
{
    private readonly MetaDataContext _context;

    public UserFeaturePermissionRepository(MetaDataContext context)
    {
        _context = context;
    }

    public async Task<Domain.UserFeaturePermissions.Entities.UserFeaturePermission?> GetByUserIdAsync(string userId)
    {
        var repoResult = await _context.Set<UserFeaturePermission>()
                             .Include(p => p.UserRole)
                                 .ThenInclude(r => r.Options)
                                     .ThenInclude(o => o.Feature)
                             .FirstOrDefaultAsync(p => p.UserId == userId);
        if (repoResult == null)
            return null;
        return UserFeaturePermissionMapper.MapFromInfrastructure(repoResult);
    }

    public async Task DeleteAllPermissionsForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var items = await _context.Set<UserFeaturePermission>()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.Set<UserFeaturePermission>().RemoveRange(items);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Domain.UserFeaturePermissions.Entities.UserFeaturePermission>> GetAllAsync()
    {
        var repoResult = await _context.Set<UserFeaturePermission>()
                             .Include(p => p.UserRole)
                                 .ThenInclude(r => r.Options)
                                     .ThenInclude(o => o.Feature)
                             .ToListAsync();
        return repoResult.Select(UserFeaturePermissionMapper.MapFromInfrastructure);
    }

    public async Task<bool> HasRoleAssignments(int roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFeaturePermissions
            .AnyAsync(p => p.UserRoleId == roleId, cancellationToken);
    }

    public async Task<Domain.UserFeaturePermissions.Entities.UserFeaturePermission> AddAsync(Domain.UserFeaturePermissions.Entities.UserFeaturePermission permission)
    {
        // First, check if a permission already exists for this user and remove it
        var existingPermissions = await _context.Set<UserFeaturePermission>()
            .Where(p => p.UserId == permission.UserId)
            .ToListAsync();
        
        if (existingPermissions.Any())
        {
            _context.Set<UserFeaturePermission>().RemoveRange(existingPermissions);
        }

        // Get the role from the database to ensure we're using a tracked entity
        var existingRole = await _context.Set<Role>()
            .Include(r => r.Options)
                .ThenInclude(o => o.Feature)
            .FirstOrDefaultAsync(r => r.Id == permission.UserRoleId);
        
        if (existingRole == null)
        {
            throw new InvalidOperationException($"Role with ID {permission.UserRoleId} not found");
        }

        // Create a new UserFeaturePermission with the tracked role
        var infraPermission = new UserFeaturePermission
        {
            UserId = permission.UserId,
            UserRoleId = permission.UserRoleId,
            UserRole = existingRole,
            UpdatedByUserId = permission.UpdatedByUserId,
            UpdatedDate = permission.UpdatedDate
        };

        await _context.Set<UserFeaturePermission>().AddAsync(infraPermission);
        await _context.SaveChangesAsync();
        
        // Return the domain entity with the assigned ID
        return UserFeaturePermissionMapper.MapFromInfrastructure(infraPermission);
    }

    public async Task UpdateAsync(Domain.UserFeaturePermissions.Entities.UserFeaturePermission permission)
    {
        var existingPermission = await _context.Set<UserFeaturePermission>()
            .FirstOrDefaultAsync(p => p.Id == permission.Id);
        
        if (existingPermission != null)
        {
            existingPermission.UserId = permission.UserId;
            existingPermission.UserRoleId = permission.UserRoleId;
            existingPermission.UpdatedByUserId = permission.UpdatedByUserId;
            existingPermission.UpdatedDate = permission.UpdatedDate;
            
            _context.Set<UserFeaturePermission>().Update(existingPermission);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Domain.UserFeaturePermissions.Entities.UserFeaturePermission> UpsertAsync(Domain.UserFeaturePermissions.Entities.UserFeaturePermission permission)
    {
        var existingPermission = await _context.Set<UserFeaturePermission>()
            .Include(p => p.UserRole)
                .ThenInclude(r => r.Options)
                    .ThenInclude(o => o.Feature)
            .FirstOrDefaultAsync(p => p.UserId == permission.UserId);

        if (existingPermission != null)
        {
            // Update existing permission
            existingPermission.UserRoleId = permission.UserRoleId;
            existingPermission.UpdatedByUserId = permission.UpdatedByUserId;
            existingPermission.UpdatedDate = permission.UpdatedDate;

            // Get the new role to ensure we're using a tracked entity
            var existingRole = await _context.Set<Role>()
                .Include(r => r.Options)
                    .ThenInclude(o => o.Feature)
                .FirstOrDefaultAsync(r => r.Id == permission.UserRoleId);
            
            if (existingRole == null)
            {
                throw new InvalidOperationException($"Role with ID {permission.UserRoleId} not found");
            }

            existingPermission.UserRole = existingRole;
            _context.Set<UserFeaturePermission>().Update(existingPermission);
            await _context.SaveChangesAsync();
            
            return UserFeaturePermissionMapper.MapFromInfrastructure(existingPermission);
        }
        else
        {
            // Add new permission - reuse existing AddAsync logic
            return await AddAsync(permission);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var permission = await _context.Set<UserFeaturePermission>().FindAsync(id);
        if (permission != null)
            _context.Set<UserFeaturePermission>().Remove(permission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByUserIdAsync(string userId)
    {
        var permissions = await _context.Set<UserFeaturePermission>()
            .Where(p => p.UserId == userId)
            .ToListAsync();
        
        if (permissions.Any())
        {
            _context.Set<UserFeaturePermission>().RemoveRange(permissions);
            await _context.SaveChangesAsync();
        }
    }
}