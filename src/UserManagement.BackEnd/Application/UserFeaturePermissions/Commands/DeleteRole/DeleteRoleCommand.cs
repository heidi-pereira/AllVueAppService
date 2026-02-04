using MediatR;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteRole
{
    public record DeleteRoleCommand(int RoleId) : IRequest<bool>;
}
