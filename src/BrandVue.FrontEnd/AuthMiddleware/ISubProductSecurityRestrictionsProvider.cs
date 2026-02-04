using System.Threading;

namespace Vue.AuthMiddleware
{
    public interface ISubProductSecurityRestrictionsProvider
    {
        Task<ISubProductSecurityRestrictions> GetSecurityRestrictions(CancellationToken cancellationToken);
    }
}
