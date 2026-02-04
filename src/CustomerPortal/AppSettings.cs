using System;
using CustomerPortal.Models;

namespace CustomerPortal
{
    public class AppSettings
    {
        public string AppDeploymentEnvironment { get; set; }
        public string AuthAuthority { get; set; }
        public string AuthClientId { get; set; }
        public string AuthClientSecret { get; set; }
        public string DefaultPortalGroup { get; set; }
        public string VueHost { get; set; }
        public string GoogleTags { get; set; }
        public string EgnyteDomain { get; set; }
        public string EgnyteClientId { get; set; }
        public string EgnyteUsername { get; set; }
        public string EgnytePassword { get; set; }
        public string EgnyteRootFolder { get; set; }
        public string EgnyteAccessToken { get; set; } // Strictly not necessary but Egnyte imposes a 10 OAuth requests per user per hour limit and then doesn't seem to time out the access tokens so lets reuse them where possible
        public string EmailUserName { get; set; }
        public string EmailPassword { get; set; }
        public string DataDownloadDomain { get; set; }
        public bool ShouldMigrateDb { get; set; }
        public bool WeightingConfigurationEnabled { get; set; }
        public RunningEnvironment RunningEnvironment
        {
            get
            {
                switch (RunningEnvironmentDescription.ToLowerInvariant())
                {
                    case "dev":
                        return RunningEnvironment.Development;
                    case "live":
                        return RunningEnvironment.Live;
                }
                return RunningEnvironment.Unknown;
            }
        }
        public string  RunningEnvironmentDescription { get; set; }
    }
}
