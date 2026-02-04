using System;
using System.Text.RegularExpressions;

namespace Test.BrandVue.FrontEnd.SurveyApi.Services
{
    public static class StringExtensions
    {
        /// <summary>
        /// Extension method to count the number of line breaks in this string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int NewLineCount(this string str) => 
            Regex.Matches(str, Environment.NewLine).Count;
    }
}
