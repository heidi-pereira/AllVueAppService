using BrandVue.EntityFramework;
using Vue.Common.Auth.Ui;

namespace CustomerPortal.Models
{
    public enum RunningEnvironment
    {
        Development,
        Live,
        Unknown,
    }

    public class ProductConfigurationResult
    {
        public string[] GoogleTags { get; set; }
        public UserContext User { get; set; }
        public VueContext VueContext { get; set; }
        public string SubdomainOrganisation => User.AuthCompany;
        public string ProductName => SavantaConstants.AllVueShortCode;
        public string HelpLink => "https://docs.savanta.com/allvue/Default.html";
        public string MixPanelToken { get; set; }
        public string RunningEnvironmentDescription { get; set; }
        public RunningEnvironment RunningEnvironment { get; set; }
    }
}
