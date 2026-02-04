using UserManagement.BackEnd.Models;
using Vue.Common.AuthApi;
using Vue.Common.AuthApi.Models;

namespace UserManagement.BackEnd.Services
{
    public class CompaniesService : ICompaniesService
    {
        private readonly IExtendedAuthApiClient _extendedAuthApiClient;
        private readonly IProductsService _productsService;

        public CompaniesService(IExtendedAuthApiClient extendedAuthApiClient, IProductsService productsService)
        {
            _extendedAuthApiClient = extendedAuthApiClient;
            _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
        }

        public async Task<List<string>> GetCompanyAncestorNames(string companyId, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token)
        {
            var dontIncludeSavanta = false;
            var companies = await _extendedAuthApiClient.GetCompanyAncestorsById(companyId, dontIncludeSavanta, token);

            return companies.Where(company => filterByCompanySecurityGroup(company.SecurityGroup)).Select(company => company.DisplayName).ToList();
        }

        public async Task<CompanyWithProducts> GetCompanyWithProductsByShortCode(string companyShortCode, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token)
        {
            var company = await _extendedAuthApiClient.GetCompanyByShortCode(companyShortCode, token);
            if ((company == null) || !filterByCompanySecurityGroup(company.SecurityGroup))
            {
                throw new ArgumentException($"Company with short code {companyShortCode} not found");
            }
            return CompanyFromCompanyNode(company);
        }

        public async Task<CompanyWithProducts> GetCompanyWithProductsAndChildCompanies(string companyShortCode, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token)
        {
            var companyList = (await _extendedAuthApiClient.GetCompanyAndChildrenList(companyShortCode, token)).ToList();
            if (companyList == null || !companyList.Any())
            {
                throw new ArgumentException($"Company with short code {companyShortCode} not found");
            }
            return CompanyWithProductsFromCompanyNode(filterByCompanySecurityGroup, companyList.First(x=>x.ShortCode == companyShortCode));
        }

        public async Task<CompanyWithProducts> GetCompanyWithProductsById(string companyId, Func<string, bool> filterByCompanySecurityGroup, CancellationToken token)
        {
            var company = await _extendedAuthApiClient.GetCompanyById(companyId, token);
            if ((company == null) || !filterByCompanySecurityGroup(company.SecurityGroup))
            {
                throw new ArgumentException($"Company with id {companyId} not found");
            }
            return CompanyFromCompanyNode(company);
        }

        private CompanyWithProducts CompanyFromCompanyNode(CompanyNode company)
        {
            return new CompanyWithProducts(company.Id,
                company.ShortCode,
                company.DisplayName,
                company.Url,
                company.HasExternalSSOProvider, [], 
                _productsService.ToProducts(company.ProductShortCodes), 
                company.ProductShortCodes.Contains(ProductsService.AuthProductIdFor_SurveyVueEditor), 
                company.ProductShortCodes.Contains(ProductsService.AuthProductIdFor_SurveyVueFeedback));
        }

        private CompanyWithProducts CompanyWithProductsFromCompanyNode(Func<string, bool> filterByCompanySecurityGroup, CompanyNode company)
        {
            return new CompanyWithProducts(company.Id,
                company.ShortCode,
                company.DisplayName,
                company.Url,
                company.HasExternalSSOProvider, 
                company.Children.Where(c => filterByCompanySecurityGroup(c.SecurityGroup)).Select(c => CompanyWithProductsFromCompanyNode(filterByCompanySecurityGroup, c)).ToList(), 
                _productsService.ToProducts(company.ProductShortCodes), 
                company.ProductShortCodes.Contains(ProductsService.AuthProductIdFor_SurveyVueEditor), 
                company.ProductShortCodes.Contains(ProductsService.AuthProductIdFor_SurveyVueFeedback));
        }
    }
}