using BrandVue.EntityFramework.MetaData.FeatureToggle;
using CustomerPortal.Configurations;
using CustomerPortal.Models;
using CustomerPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vue.Common.Auth;
using UI = Vue.Common.Auth.Ui;
using Vue.Common.FeatureFlags;

namespace CustomerPortal.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    [ApiExplorerSettings(GroupName = "InternalApi")]
    public class VueController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IUserContext _userContext;
        private readonly IVueContextService _vueContextService;
        private readonly IOptions<MixPanelSettings> _options;
        private readonly IFeatureQueryService _featureQueryService;
        private readonly IUserFeaturePermissionsService _userFeaturePermissionsService;

        public VueController(
            AppSettings appSettings,
            IUserContext userContext,
            IVueContextService vueContextService,
            IOptions<MixPanelSettings> options,
            IFeatureQueryService featureQueryService,
            IUserFeaturePermissionsService userFeaturePermissionsService)
        {
            _options = options;
            _appSettings = appSettings;
            _userContext = userContext;
            _vueContextService = vueContextService;
            _featureQueryService = featureQueryService;
            _userFeaturePermissionsService = userFeaturePermissionsService;
        }

        [HttpGet]
        public async Task<ProductConfigurationResult> GetProductConfiguration()
        {
            var featurePermissions = (await _userFeaturePermissionsService.GetFeaturePermissionsAsync()).ToList();
            var applicationUser = UI.UserContext.FromUserContextAndPermissions(_userContext, featurePermissions);

            return new ProductConfigurationResult
            {
                GoogleTags = _appSettings.GoogleTags.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray(),
                User = applicationUser,
                VueContext = _vueContextService.GetVueContext(),
                MixPanelToken = _options.Value.AllVueToken,
                RunningEnvironment = _appSettings.RunningEnvironment,
                RunningEnvironmentDescription = _appSettings.RunningEnvironmentDescription,
            };
        }

        [HttpGet]
        [Route("availables")]
        public async Task<IEnumerable<Feature>> GetEnabledFeaturesForCurrentUser(CancellationToken token)
        {
            return await _featureQueryService.GetEnabledFeaturesForCurrentUserAsync(token);
        }
    }
}
