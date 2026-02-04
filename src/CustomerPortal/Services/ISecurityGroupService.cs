using System.Threading.Tasks;

namespace CustomerPortal.Services
{
    public interface ISecurityGroupService
    {
        Task<bool> UserHasSecurityGroupAccessFor(string authCompanyId);
    }
}
