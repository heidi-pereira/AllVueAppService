using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRolesByCompany;

public class GetRolesByCompanyQueryHandler : IRequestHandler<GetRolesByCompanyQuery, IEnumerable<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesByCompanyQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IEnumerable<RoleDto>> Handle(GetRolesByCompanyQuery request, CancellationToken cancellationToken)
    {      
        var allRoles = await _roleRepository.GetAllAsync();
        var filteredRoles = allRoles.Where(role => role.OrganisationId.Equals(request.CompanyId, StringComparison.OrdinalIgnoreCase) || role.OrganisationId.Equals("savanta", StringComparison.OrdinalIgnoreCase));
        return filteredRoles.Select(role => new RoleDto(
            role.Id,
            role.RoleName,
            role.OrganisationId,
            role.Options.Select(p => new PermissionFeatureOptionDto(p.Id, p.Name)).ToList()
        ));
    }
}
