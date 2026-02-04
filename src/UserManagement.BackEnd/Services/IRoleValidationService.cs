namespace UserManagement.BackEnd.Services
{
    public interface IRoleValidationService
    {
        /// <summary>
        /// Validates the role name and permission option IDs together according to business rules.
        /// Throws ValidationException if invalid.
        /// </summary>
        /// <param name="roleName">The role name to validate.</param>
        /// <param name="permissionOptionIds">The permission option IDs to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ValidateRole(string roleName, IEnumerable<int> permissionOptionIds, int? existingRoleId = null, CancellationToken cancellationToken = default);
    }
}