using System.Configuration;
using System.IO;
using System.Xml;

namespace BrandVue.Services
{
    public class WebConfigService
    {
        public static int MaxFileUploadSize()
        {
            Configuration config1 =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var myWebConfigFileName = Path.Combine(Path.GetDirectoryName(config1.FilePath), "web.config");

            if (Path.Exists(myWebConfigFileName))
            {
                var webConfig = new XmlDocument();
                webConfig.Load(myWebConfigFileName);

                var myNode = webConfig.DocumentElement.SelectSingleNode(
                    "/configuration/system.webServer/security/requestFiltering/requestLimits");
                if (myNode != null)
                {
                    var maxAllowedContentLengthNode = myNode.Attributes.GetNamedItem("maxAllowedContentLength");
                    if (maxAllowedContentLengthNode != null && maxAllowedContentLengthNode.Value != null)
                    {
                        if (int.TryParse(maxAllowedContentLengthNode.Value, out var valResult))
                        {
                            return valResult;
                        }
                    }
                }
            }

            return 200 * 1024 * 1024;
        }
    }
}
