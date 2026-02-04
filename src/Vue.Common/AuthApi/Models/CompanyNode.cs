using AuthServer.GeneratedAuthApi;

namespace Vue.Common.AuthApi.Models
{
    public class CompanyNode
    {
        public string Id { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SecurityGroup { get; set; } = string.Empty;
        public List<string> ProductShortCodes { get; set; } = new List<string>();
        public IEnumerable<CompanyNode> Children { get; set; } = new List<CompanyNode>();
        public bool HasExternalSSOProvider { get; set; }

        public static CompanyNode? FromCompanyModel(CompanyModel? company)
        {
            if (company == null)
            {
                return null;
            }
            return new CompanyNode() { 
                Id = company.Id,
                ShortCode = company.ShortCode, 
                DisplayName = company.DisplayName,
                Url = company.Url,
                SecurityGroup = company.SecurityGroup,
                Children = new List<CompanyNode>(),
                ProductShortCodes = company.Products?.Select(x => x.ShortCode).Where(x=> !string.IsNullOrEmpty(x)).ToList() ?? new List<string>(),
                HasExternalSSOProvider = company.IsExternalLogin,
            };
        }
    }
}
