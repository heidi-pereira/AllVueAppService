using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRoleById
{
    public record GetRoleByIdQuery(int RoleId) : IRequest<RoleDto>;

    public class GetRoleByIdQueryHandler(IRoleRepository repository) : IRequestHandler<GetRoleByIdQuery, RoleDto>
    {
        private readonly IRoleRepository _repository = repository;

        public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
                return null;

            return new RoleDto(
                role.Id,
                role.RoleName,
                role.OrganisationId,
                [.. role.Options.Select(p => new PermissionFeatureOptionDto(
                    p.Id,
                    p.Name
                ))]
            );
        }
    }
}
