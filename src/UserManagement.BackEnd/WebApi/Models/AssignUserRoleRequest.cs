namespace UserManagement.BackEnd.WebApi.Models
{
    public record AssignUserRoleRequest(
        string UserId,
        int UserRoleId,
        string UpdatedByUserId
    );
}
