using BrandVue.EntityFramework;
using CustomerPortal.Models;
using System;
using System.Linq;
using Vue.Common.Auth;

namespace CustomerPortal.Services
{
    public class VueContextService : IVueContextService
    {
        private const string SurveyVueProduct = SavantaConstants.AllVueShortCode;
        private readonly AppSettings _appSettings;
        private readonly IRequestContext _requestContext;
        private readonly IUserContext _userContext;
        private readonly IAllVueProductConfigurationService _allVueProductConfigurationService;


        public VueContextService(AppSettings appSettings, IRequestContext requestContext, IUserContext userContext, IAllVueProductConfigurationService allVueProductConfigurationService)
        {
            _appSettings = appSettings;
            _requestContext = requestContext;
            _userContext = userContext;
            _allVueProductConfigurationService= allVueProductConfigurationService;
        }

        public VueContext GetVueContext()
        {
            if (_appSettings.RunningEnvironment == RunningEnvironment.Development)
            {
                return new VueContext(GetDevelopmentUrl(), true, true,true, true, true, true,_allVueProductConfigurationService);
            }

            if (!_userContext.Products.Contains(SurveyVueProduct, StringComparer.OrdinalIgnoreCase))
            {
                return new VueContext(string.Empty, false, false, false, false, false,false, _allVueProductConfigurationService);
            }
            return new VueContext(GetVueUrl(), true, true, true, true, _userContext.IsAdministrator,true, _allVueProductConfigurationService);
        }

        private string GetVueUrl()
        {
            return @$"https:\\{_requestContext.PortalGroup}.{_appSettings.VueHost}";
        }

        private string GetDevelopmentUrl()
        {
            if (_appSettings.VueHost.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return @$"http:\\{_appSettings.VueHost}";
            }
            return GetVueUrl();
        }
    }
}
