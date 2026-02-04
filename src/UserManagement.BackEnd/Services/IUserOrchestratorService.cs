using UserManagement.BackEnd.Models;

namespace UserManagement.BackEnd.Services
{
    public record UserToAdd(string OwnerCompanyId, 
        string FirstName, 
        string LastName, 
        string Email, 
        string Role, 
        int? RoleId, 
        List<ProductIdentifier> Products,
        bool SurveyVueEditingAvailable,
        bool SurveyVueFeedbackAvailable);

    public interface IUserOrchestratorService
    {
        Task DeleteUser(string userCompanyShortCode, string requesterEmail, string userIdOfUserToDelete, CancellationToken token);
        Task ForgotPassword(string userCompanyShortCode, string userEmail, CancellationToken token);
        Task<User> GetUserAsync(string userCompanyShortCode, string userId, CancellationToken token);
        Task UpdateUser(User user, string userCompanyShortCode, string updatedByUserId, CancellationToken token);
        Task AddUser(UserToAdd user, string userCompanyShortCode, string updatedByUserId, CancellationToken token);
    }
}
