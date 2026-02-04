namespace UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities
{
    public class UserFeaturePermission
    {
        public int Id { get; private set; }
        public string UserId { get; private set; }
        public int UserRoleId { get; private set; }
        public Role UserRole { get; private set; }
        public string UpdatedByUserId { get; private set; }
        public DateTime UpdatedDate { get; private set; } 
        
        public UserFeaturePermission(string userId, Role role, string updatedByUserId)
        {
            UserId = userId;
            UserRole = role;
            UserRoleId = role.Id;
            UpdatedByUserId = updatedByUserId;
            UpdatedDate = DateTime.UtcNow;
        }

        public UserFeaturePermission(int id, string userId, Role role, string updatedByUserId)
        {
            Id = id;
            UserId = userId;
            UserRole = role;
            UserRoleId = role.Id;
            UpdatedByUserId = updatedByUserId;
            UpdatedDate = DateTime.UtcNow;
        }
    }
}