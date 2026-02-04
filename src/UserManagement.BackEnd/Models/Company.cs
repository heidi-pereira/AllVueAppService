namespace UserManagement.BackEnd.Models
{
    public record Company(
        string Id,
        string ShortCode,
        string DisplayName,
        string URL,
        bool HasExternalSSOProvider);
}