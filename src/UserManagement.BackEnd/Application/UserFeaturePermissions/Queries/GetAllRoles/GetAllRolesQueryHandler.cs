using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllRoles;

public class GetAllRolesQuery : IRequest<IEnumerable<RoleDto>>
{
}

public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, IEnumerable<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetAllRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IEnumerable<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(role => new RoleDto(
            role.Id,
            role.RoleName,
            role.OrganisationId,
            role.Options.Select(p => new PermissionFeatureOptionDto(p.Id, p.Name)).ToList()
        ));
    }
}
