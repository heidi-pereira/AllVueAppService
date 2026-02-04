using MediatR;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole
{
    public record AssignUserRoleCommand(
        string UserId,
        int UserRoleId,
        string UpdatedByUserId
    ) : IRequest<UserFeaturePermissionDto>;

    public record UserFeaturePermissionDto(
        int Id,
        string UserId,
        int UserRoleId,
        string UpdatedByUserId,
        DateTime UpdatedDate
    );
}
