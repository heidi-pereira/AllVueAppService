using MediatR;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteUserRole
{
    public record DeleteUserRoleCommand(string UserId) : IRequest<bool>;
}
