using System.Security.Claims;

namespace Vue.Common.Auth
{
    public interface IUserContext : IUserContextBase
    {
        public bool HasSecurityGroupAccess(string securityGroup);
        public IReadOnlyCollection<Claim> Claims { get; }
        DateTimeOffset GetTrialDataRestrictedDate(DateTimeOffset subsetEndDate);
        void FreezeClaims();
    }
}