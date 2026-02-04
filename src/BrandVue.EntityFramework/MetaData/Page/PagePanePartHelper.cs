using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData.Page
{
    public static class PagePanePartHelper
    {
        // This is based off getUrlSafePageName in UrlHelper.ts
        public static string SanitizeUrl(string url)
        {
            var update = url.ToLower();
            foreach (var val in " \t")
            {
                update = update.Replace(val, '-');
            }
            foreach (var val in "?#[]@!$'()*+,;=%\\")
            {
                update = update.Replace(val.ToString(), "");
            }
            update = update.Replace("&", "and");
            update = update.Replace("\\", "or");
            return update;
        }
    }
}
