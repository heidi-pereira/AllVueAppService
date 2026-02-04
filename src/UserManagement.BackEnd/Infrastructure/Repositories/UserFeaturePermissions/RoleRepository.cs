using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using Vue.Common.Auth.Permissions;
using Vue.Common.Constants.Constants;

namespace UserManagement.BackEnd.Infrastructure.Repositories.UserFeaturePermissions;

public class RoleRepository : IRoleRepository
{
    private readonly MetaDataContext _context;

    public RoleRepository(MetaDataContext dbContext)
    {
        _context = dbContext;
    }

    public async Task<Role> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.Include(r => r.Options).ThenInclude(o => o.Feature).SingleAsync(r => r.Id == id, cancellationToken);
        return role  != null ? RoleMapper.MapFromInfrastructure(role) : throw new KeyNotFoundException($"Role with ID {id} not found.");
    }



    private static Role CreateDomainPermissionFeature(int id, string userRole)
    {
        var permissions = PermissionsHelper.DefaultPermissions(userRole);

        var role = new Role(id, userRole, "savanta", "system");

        foreach (var option in permissions)
        {
            var (featureName,optionName) = SplitCamelCasedNameIntoFeatureAndOption(option.Name);
            role.AssignPermission(new PermissionOption(
                option.Id,
                optionName,
                new PermissionFeature(featureName, SystemKey.AllVue)
            ));
        }
        return role;
    }

    private static (string,string) SplitCamelCasedNameIntoFeatureAndOption(string optionName)
    {
        if (string.IsNullOrEmpty(optionName))
        {
            return (optionName, optionName);
        }

        var words = System.Text.RegularExpressions.Regex.Split(optionName, @"(?<!^)(?=[A-Z])");

        if (words.Length <= 1)
        {
            return (optionName, optionName.ToLowerInvariant());
        }
        return (words[0], string.Join("/", words.Skip(1).Select(x => x.ToLowerInvariant())));
    }


    public async Task<IEnumerable<Role>> GetByOrganisationIdAsync(string organisationId)
    {
        var repoResult = await _context.Roles.Where(r => r.OrganisationId == organisationId).ToListAsync();
        var roles = repoResult.Select(RoleMapper.MapFromInfrastructure).ToList();
        AddSavantaRoles(roles);
        return roles;
    }

    public async Task<Role> AddAsync(Role role)
    {
        var infraRole = RoleMapper.MapToInfrastructure(role);
        infraRole.Id = 0; // Ensure Id is 0 for new entities

        // Replace infraRole.Options with tracked PermissionOptions from the database
        var optionIds = infraRole.Options.Select(o => o.Id).ToList();
        var trackedOptions = _context.PermissionOptions.Where(po => optionIds.Contains(po.Id)).ToList();

        infraRole.Options.Clear();
        foreach (var trackedOption in trackedOptions)
        {
            infraRole.Options.Add(trackedOption);
        }

        var newEntity = _context.Roles.Add(infraRole).Entity;
        await _context.SaveChangesAsync();
        role.SetId(newEntity.Id);
        
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        var infraRole = RoleMapper.MapToInfrastructure(role);
        var existingRole = await _context.Roles
            .Include(r => r.Options)
            .SingleAsync(r => r.Id == role.Id, cancellationToken);

        existingRole.RoleName = infraRole.RoleName;
        existingRole.OrganisationId = infraRole.OrganisationId;
        existingRole.UpdatedByUserId = infraRole.UpdatedByUserId;

        var optionIds = infraRole.Options.Select(o => o.Id).ToList();
        var trackedOptions = await _context.PermissionOptions
            .Where(po => optionIds.Contains(po.Id))
            .ToListAsync();

        existingRole.Options.Clear();
        foreach (var trackedOption in trackedOptions)
        {
            existingRole.Options.Add(trackedOption);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        var infraRoles = await _context.Roles
            .Include(r => r.Options)
            .ThenInclude(o => o.Feature)
            .ToListAsync();

        var roles = infraRoles.Select(RoleMapper.MapFromInfrastructure).ToList();
        AddSavantaRoles(roles);
        return roles;
    }

    private static void AddSavantaRoles(List<Role> roles)
    {
        roles.Add(CreateDomainPermissionFeature(-1, Roles.SystemAdministrator));
        roles.Add(CreateDomainPermissionFeature(-2, Roles.Administrator));
        roles.Add(CreateDomainPermissionFeature(-3, Roles.User));
        roles.Add(CreateDomainPermissionFeature(-4, Roles.ReportViewer));
    }

    public async Task DeleteAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .Include(r => r.Options)
            .SingleAsync(r => r.Id == roleId, cancellationToken);

        role.Options.Clear();
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
    }
}