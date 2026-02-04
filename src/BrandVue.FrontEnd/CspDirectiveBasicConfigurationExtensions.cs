using Microsoft.AspNetCore.Builder;
using NWebsec.Core.Common.HttpHeaders.Configuration;

namespace BrandVue
{
    internal static class CspDirectiveBasicConfigurationExtensions
    {
        public static void CustomSourcesIfAny(this ICspDirectiveBasicConfiguration s, string[] policySources)
        {
            if (policySources != null && policySources.Any())
            {
                s.CustomSources(policySources);
            }
        }
    }
}