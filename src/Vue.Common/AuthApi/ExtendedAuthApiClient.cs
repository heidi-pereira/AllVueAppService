using AuthServer.GeneratedAuthApi;
using System.Threading;
using Vue.Common.AuthApi.Models;
using Vue.Common.Constants;

namespace Vue.Common.AuthApi
{
    public class ExtendedAuthApiClient : IExtendedAuthApiClient
    {
        private readonly IAuthApiClient _authApiClient;

        public ExtendedAuthApiClient(IAuthApiClient authApiClient)
        {
            _authApiClient = authApiClient;
        }

        public async Task<CompanyNode?> GetCompanyTree(string shortCode, CancellationToken cancellationToken)
        {
            var allCompanies = await _authApiClient.GetAllCompanies(cancellationToken);
            var companyModel = allCompanies.FirstOrDefault(company => company.ShortCode == shortCode);
            if (companyModel == null)
            {
                return null;
            }
            var company = CompanyNode.FromCompanyModel(companyModel);
            var parentGroups = allCompanies.Where(company => company.ParentCompanyId != null)
                .GroupBy(c => c.ParentCompanyId)
                .ToDictionary(g => g.Key, g => g.ToList());

            IEnumerable<CompanyNode> GetChildCompanyNodesByParentId(string parentId)
            {
                parentGroups.TryGetValue(parentId, out var children);
                var childNodes = children?.Select(CompanyNode.FromCompanyModel).ToList() ?? new List<CompanyNode>();
                foreach (var child in childNodes)
                {
                    if (child != null)
                    {
                        child.Children = GetChildCompanyNodesByParentId(child.Id).ToList();
                    }
                }
                return childNodes;
            }

            company.Children = GetChildCompanyNodesByParentId(company.Id).ToList();

            return company;
        }

        private IEnumerable<CompanyNode> FlattenCompanyTree(CompanyNode company)
        {
            var children = company.Children;
            var companies = new List<CompanyNode> { company };
            foreach (var child in children)
            {
                companies.AddRange(FlattenCompanyTree(child));
            }

            return companies;
        }

        public async Task<IEnumerable<CompanyNode>> GetCompanyAndChildrenList(string shortCode, CancellationToken cancellationToken)
        {
            var companyTree = await GetCompanyTree(shortCode, cancellationToken);
            if (companyTree == null)
            {
                return Enumerable.Empty<CompanyNode>();
            }

            return FlattenCompanyTree(companyTree);
        }

        public async Task<IList<CompanyModel>> GetCompanyAncestorsById(string authCompanyId, bool includeSavanta, CancellationToken cancellationToken)
        {
            var allCompanies = await _authApiClient.GetAllCompanies(cancellationToken);
            if (!includeSavanta) {
                allCompanies = allCompanies.Where(company => company.ShortCode != AuthConstants.SavantaCompany).ToList();
            }
            var companyModel = allCompanies.FirstOrDefault(company => company.Id == authCompanyId);
            if (companyModel == null)
            {
                return new List<CompanyModel>();
            }

            var companyList = new List<CompanyModel>();
            var parentId = companyModel.ParentCompanyId;
            while (parentId != null)
            {
                var parentCompany = allCompanies.FirstOrDefault(company => company.Id == parentId);
                if (parentCompany != null)
                {
                    companyList.Add(parentCompany);
                    parentId = parentCompany.ParentCompanyId;
                }
                else
                {
                    break;
                }
            }

            return companyList;
        }

        public async Task<CompanyNode?> GetCompanyById(string id, CancellationToken token)
        {
            return CompanyNode.FromCompanyModel(await _authApiClient.GetCompanyById(id, token));
        }
        public async Task<CompanyNode?> GetCompanyByShortCode(string shortCode, CancellationToken token)
        {
            return CompanyNode.FromCompanyModel(await _authApiClient.GetCompanyByShortcode(shortCode, token));
        }
    }
}
