using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VueReporting.Helpers
{
    public static class StringUlrExtension
    {
        public static string UrlPathCombine(this string url1, string url2)
        {
            if (url1.Length == 0)
            {
                return url2;
            }

            if (url2.Length == 0)
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return string.Format("{0}/{1}", url1, url2);
        }
    }
}
