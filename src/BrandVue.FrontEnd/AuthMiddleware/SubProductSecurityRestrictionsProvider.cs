using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework;
using BrandVue.SourceData.CalculationPipeline;
using Microsoft.Extensions.Logging;
using System.Threading;
using Vue.Common.AuthApi;

namespace Vue.AuthMiddleware
{
    public class SubProductSecurityRestrictionsProvider : ISubProductSecurityRestrictionsProvider
    {
        private readonly AppSettings _settings;
        private readonly IAuthApiClient _authApiClient;
        private readonly IPermissionService _permissionService;
        private readonly IProductContext _productContext;
        private readonly IReadOnlyCollection<int> _restrictedSurveyIds;
        private readonly ILogger<SubProductSecurityRestrictionsProvider> _logger;
        private ISubProductSecurityRestrictions _securityRestrictions;
        private DateTimeOffset _lastSecurityRestrictionsTime;
        private readonly bool _skipAuthCheckingForBrandVue = false;

        public SubProductSecurityRestrictionsProvider(AppSettings settings,
            IProductContext productContext,
            IAuthApiClient authApiClient,
            IPermissionService permissionService,
            ILogger<SubProductSecurityRestrictionsProvider> logger)
        {
            _settings = settings;
            _authApiClient = authApiClient;
            _permissionService = permissionService;
            // Other products are open to everyone (with that product claim)
            _restrictedSurveyIds = productContext.NonMapFileSurveyIds ?? Array.Empty<int>();
            _productContext = productContext;
            _logger = logger;

            _skipAuthCheckingForBrandVue = SkipAuthCheckingForBrandVueWhenCompanyIdsDontMatchOurAuthServer(settings);
        }

        private static bool SkipAuthCheckingForBrandVueWhenCompanyIdsDontMatchOurAuthServer(AppSettings settings)
        {
            //Nb. if you have a local auth server configured, and it's restored from live then it will have all the live company IDs
            //and you should not skip the auth checking

            bool skipAuthCheckingForBrandVue = settings.IsDeployedEnvironmentOneOfThese(AppSettings.DevEnvironmentName) && settings.IsAuthServerConfigured();
            if (settings.IsDeployedEnvironmentOneOfThese("test", "beta"))
            {
                skipAuthCheckingForBrandVue = true;
            }
            return skipAuthCheckingForBrandVue;
        }

        public async Task<ISubProductSecurityRestrictions> GetSecurityRestrictions(CancellationToken cancellationToken)
        {
            if (_securityRestrictions == null || DateTimeOffset.UtcNow - _lastSecurityRestrictionsTime > TimeSpan.FromMinutes(5))
            {
                _securityRestrictions = await GenerateSecurityRestrictions(cancellationToken);
                _lastSecurityRestrictionsTime = DateTimeOffset.UtcNow;
            }
            return _securityRestrictions;
        }

        private async Task<ISubProductSecurityRestrictions> GenerateSecurityRestrictions(CancellationToken cancellationToken)
        {
            
            if (_productContext.IsAllVue)
            {
                try
                {
                    return await GenerateSurveyVueRestrictions(cancellationToken);
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "Failed to get SurveyVue Security Restrictions for survey(s) {surveyIds}", _restrictedSurveyIds);
                    throw;
                }
            }

            if (_restrictedSurveyIds.Any())
            {
                try
                {
                    return await GenerateBrandVueRestrictions(cancellationToken);
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "Failed to get {Product} Security Restrictions for survey(s) {surveyIds}", _productContext, _restrictedSurveyIds);
                    throw;
                }
            }
            return SubProductSecurityRestrictions.Unrestricted();
        }

        private bool AreAllCompaniesFoundInMasterList(IList<string> masterList, string[] companiesFound)
        {
            return companiesFound.All(s => masterList.Contains(s));
        }

        private record SurveyWithRelatedCompany (int SurveyId, string ShortCode, string AuthCompanyId);
        private async Task<ISubProductSecurityRestrictions> GenerateBrandVueRestrictions(
            CancellationToken cancellationToken)
        {
            var surveyDetails = GetOwnerSurveyDetails().ToArray();
            //
            //Nb. BrandVue surveys don't support multiple auth companies
            //
            var validCompanyShortCodesByBrandVueProduct = new Dictionary<string, string[]>()
            {
                { "barometer", new[] { "brandvue","savanta","wgsn" } },
                { "brandvue",new[] { "brandvue" }  },
                { "charities",new[] { "brandvue" }  },
                { "drinks",new[] { "brandvue" }  },
                { "eatingout",new[] { "brandvue","morar" }  },
                { "finance",new[] { "brandvue" }  },
                { "retail",new[] { "brandvue" }  },
                { "wealth",new[] { "brandvue" }  },
            };
            var masterListOfCompanies = MasterListOfAcceptableCompanies(validCompanyShortCodesByBrandVueProduct);

            if (_skipAuthCheckingForBrandVue)
            {
                var authIds = string.Join(",", surveyDetails.Select(x => x.AuthCompanyId).Distinct().ToList());
                _logger.LogError("SKIPPING CHECK for Product: {Product} Expected company(s):{CompaniesExpected} {AuthIds}", 
                        _productContext.ShortCode, 
                        string.Join(",",masterListOfCompanies),
                        authIds);
                return SubProductSecurityRestrictions.Unrestricted();
            }
            var authCompanies = surveyDetails.Select(s => s.AuthCompanyId).Distinct().ToList();
            var companies = await GetAuthCompanies(authCompanies, authCompanies, cancellationToken);
            var externalCompaniesRequired = companies.Select(c => c.ShortCode).ToArray();
            if (!AreAllCompaniesFoundInMasterList(masterListOfCompanies, externalCompaniesRequired))
            {
                var missingCompanies = companies.Where(c => !masterListOfCompanies.Contains(c.ShortCode)).ToDictionary(x => x.Id, x => x);
                var surveysWithIssues = surveyDetails.Where(x => missingCompanies.ContainsKey(x.AuthCompanyId)).ToList();
                var companySurveyList = surveysWithIssues.Select(s => new SurveyWithRelatedCompany(s.SurveyId, missingCompanies[s.AuthCompanyId].ShortCode, s.AuthCompanyId));
                throw new Exception($"{_productContext}: Invalid companies defined for survey {string.Join(" , ", companySurveyList.Select(x => $"Survey: {x.SurveyId}-{x.AuthCompanyId}{x.ShortCode}"))}");
            }
            return SubProductSecurityRestrictions.Unrestricted();

            IList<string> MasterListOfAcceptableCompanies(Dictionary<string, string[]> validCompanyShortCodesByBrandVueProduct)
            {
                var masterList = validCompanyShortCodesByBrandVueProduct[_productContext.ShortCode];
                return masterList;
            }
        }

        private async Task<ISubProductSecurityRestrictions> GenerateSurveyVueRestrictions(
            CancellationToken cancellationToken)
        {
            var surveyDetails = GetOwnerAndSharedSurveyDetails().ToArray();
            var companies = (await GetAuthCompanies(
                surveyDetails.Select(s => s.AuthCompany.OwnerCompanyId).Distinct().ToList(),
                surveyDetails.SelectMany(s => s.AuthCompany.OwnerAndSharedCompanyIds).Distinct(), 
                cancellationToken)).ToArray();

            var companyLookupByIdToShortCode = companies.ToDictionary(x => x.Id, x => x.ShortCode);

            var externalCompaniesRequired = surveyDetails.Select(
                x => x.AuthCompany.ConvertToShortCode(_logger, companyLookupByIdToShortCode));
            var ownerCompaniesRequired= surveyDetails.Select(x => x.AuthCompany.OwnerCompanyId).ToArray();

            //
            // For surveys that are shared between multiple companies, a savanta user will need to have access to the companies that own the survey
            //
            //
            var securityGroupsRequired = companies.Where(x => ownerCompaniesRequired.Contains(x.Id)).Select(c => c.SecurityGroup).Where(g => !string.IsNullOrWhiteSpace(g));
            var projectIdRequired = _productContext.SubProductId;

            return SubProductSecurityRestrictions.Restricted(securityGroupsRequired, externalCompaniesRequired, projectIdRequired, _authApiClient, _permissionService);
        }

        private IEnumerable<(int SurveyId, string AuthCompanyId)> GetOwnerSurveyDetails()
        {
            var sqlProvider = new SqlProvider(_settings.ConnectionString, _settings.ProductToLoadDataFor);
            var surveyDetails = new List<(int SurveyId, string AuthCompanyId)>();
            var sql = $@"
SELECT s.surveyId, s.authCompanyId
FROM dbo.surveys s
WHERE s.surveyId IN ({_restrictedSurveyIds.CommaList()})
";
            sqlProvider.ExecuteReader(sql, new Dictionary<string, object>(), reader =>
            {
                var surveyId = reader.GetInt32(0);
                var authCompanyId = reader.IsDBNull(1)
                    ? throw new InvalidOperationException($"Survey ${surveyId} was missing auth company ID")
                    : reader.GetString(1);

                surveyDetails.Add((surveyId, authCompanyId));
            });

            if (surveyDetails.Count != _restrictedSurveyIds.Count)
            {
                ThrowMissingDetailsError(surveyDetails.Select(s => s.SurveyId));
            }
            return surveyDetails;
        }

        private void ThrowMissingDetailsError(IEnumerable<int> retrievedSurveyIds)
        {
            var missingSurveys = _restrictedSurveyIds.Where(id => !retrievedSurveyIds.Contains(id));
            throw new InvalidOperationException(
                $"Failed to get survey details for survey(s): {string.Join(", ", missingSurveys)}");
        }

        private record CompanyAuthRequirement(int SurveyId)
        {
            public string OwnerCompanyId { get; set; }
            public List<string> SharedCompanyIds { get; } = new();
            public IEnumerable<string> OwnerAndSharedCompanyIds => SharedCompanyIds.Prepend(OwnerCompanyId).Distinct();

            public SurveyCompanyShortCodeRequirement ConvertToShortCode(ILogger logger, IDictionary<string, string> lookupAuthToCompanyShortCode)
            {
                var result = new List<string>();
                foreach (string authCompanyCode in OwnerAndSharedCompanyIds)
                {
                    if (lookupAuthToCompanyShortCode.TryGetValue(authCompanyCode, out var companyShortCode))
                    {
                        result.Add(companyShortCode);
                    }
                    else
                    {
                        logger.LogError("{SurveyId}: Company ID '{authCompanyCode}' could not be resolved to a short code for survey.", SurveyId, authCompanyCode);
                    }
                }
                return new SurveyCompanyShortCodeRequirement(SurveyId, result.ToArray());
            }
        }


        private IEnumerable<(int SurveyId, CompanyAuthRequirement AuthCompany)> GetOwnerAndSharedSurveyDetails()
        {
            var sqlProvider = new SqlProvider(_settings.ConnectionString, _settings.ProductToLoadDataFor);
            var surveyDetails = new Dictionary<int, CompanyAuthRequirement>();

            var sql = $@"
SELECT s.surveyId, s.authCompanyId,1
FROM dbo.surveys s
WHERE s.surveyId IN ({_restrictedSurveyIds.CommaList()})

UNION ALL

SELECT sso.surveyId, sso.authCompanyId,0
FROM dbo.surveySharedOwner sso
WHERE sso.surveyId IN ({_restrictedSurveyIds.CommaList()})
";
            sqlProvider.ExecuteReader(sql, new Dictionary<string, object>(), reader =>
            {
                var surveyId = reader.GetInt32(0);
                var isOwner = reader.GetInt32(2) == 1;
                if (reader.IsDBNull(1))
                {
                    throw new InvalidOperationException($"Survey ${surveyId} was missing auth company ID");
                }
                var authCompanyId = reader.GetString(1);
                if (!surveyDetails.ContainsKey(surveyId))
                {
                    surveyDetails[surveyId] = new CompanyAuthRequirement(surveyId);
                }

                if (isOwner)
                {
                    surveyDetails[surveyId].OwnerCompanyId = authCompanyId;
                }
                else
                {
                    surveyDetails[surveyId].SharedCompanyIds.Add(authCompanyId);
                }
            });

            if (surveyDetails.Count != _restrictedSurveyIds.Count)
            {
                ThrowMissingDetailsError(surveyDetails.Keys.ToArray());
            }
            return surveyDetails.Select(kvp => (SurveyId: kvp.Key, AuthCompany: kvp.Value));
        }

        private async Task<CompanyModel[]> GetAuthCompanies(IList<string> requiredAuthCompanyIds, IEnumerable<string> allAuthCompanyIds,
            CancellationToken cancellationToken)
        {
            var validatedCompanies = (await _authApiClient.GetCompanies(allAuthCompanyIds.Distinct(), cancellationToken)).ToArray();
            var validatedRequiredCompanies = validatedCompanies.Where(x => requiredAuthCompanyIds.Contains(x.Id));
            if (requiredAuthCompanyIds.Count != validatedRequiredCompanies.Count())
            {
                var retrievedCompanyIds = validatedCompanies.Select(c => c.Id);
                throw new InvalidOperationException($"Failed to get company details for survey in {_restrictedSurveyIds.CommaList()}, retrieved companies: {string.Join(", ", retrievedCompanyIds)}");
            }
            return validatedCompanies;
        }
    }
}
