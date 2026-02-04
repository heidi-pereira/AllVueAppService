using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VueReporting.EgnyteHelper
{
    public class Configuration
    {
        public string BearerToken { get; set; }
        public string Subdomain { get; set; }
        public string StorageRootUrl { get; set; }
        public string ReportsMainFolder { get; set; }
    }
}
