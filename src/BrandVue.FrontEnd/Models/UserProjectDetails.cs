using AuthServer.GeneratedAuthApi;

namespace BrandVue.Models
{
    public class UserProjectDetails
    {
        public CompanyModel ProjectCompany { get; set; }
        public IEnumerable<UserProjectsModel> Users { get; set; }
        public bool IsSharedToAllUsers { get; set; }
    }
}
