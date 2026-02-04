using AuthServer.GeneratedAuthApi;
using System.Threading.Tasks;

namespace CustomerPortal.Services
{
    public interface IRequestContext
    {
        string PortalGroup { get; }
        Task<CompanyModel> GetAuthCompany();
    }
}