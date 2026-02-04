using BrandVue.Services;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace BrandVue
{
    public static class Extensions
    {
        public static IEnumerable<T> Clone<T>(this IEnumerable<T> items) where T: ICloneable
        {
            return items.Select(x => (T) x.Clone());
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static string Stringify<T>(this T value) => JsonConvert.SerializeObject(value, BrandVueJsonConvert.Settings);

        public static string ToTitleCaseString<T>(this T value) => value == null ? null: value.ToString().IsNullOrWhiteSpace() ? string.Empty : string.Join(" ", value.ToString().Split([' ', '_']).Select(x => $"{x[0].ToString().ToUpperInvariant()}{x.Substring(1).ToLowerInvariant()}"));

        public static string StripHtmlTags(this string input)
        {
            if (input == null) return input;
            var text = input.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ");
            return Regex.Replace(text, "<[^>]*>", "", RegexOptions.Multiline);
        }
    }
}