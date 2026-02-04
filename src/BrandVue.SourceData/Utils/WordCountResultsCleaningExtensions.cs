using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BrandVue.EntityFramework.ResponseRepository;

namespace BrandVue.SourceData.Utils
{
    public static class WordCountResultsCleaningExtensions
    {
        // We support only characters within Basic Multilingual Plane: https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction#unicode-code-points.
        // This includes (probably) all letters from languages we are interested in and lets us remove unwanted characters like emojis.
        // This Regex defines unsupported characters, that is the ones which are outside of BMP.
        // .NET Regex didn't support defining code point ranges outside of BMP at the time of writing, but is now possible by using Runes.
        // To get around this limitation, we have to query for two ranges of 'surrogate pairs' - building blocks of special characters OR the special zero length joiner 'character'.
        // https://stackoverflow.com/questions/364009/c-sharp-regular-expressions-with-uxxxxxxxx-characters-in-the-pattern
        private static readonly Regex DetectUnsupportedCharsRegex = new Regex(@"[\uD800-\uDBFF][\uDC00-\uDFFF]|\u200D|[\uFE00-\uFE0F]");

        public static IEnumerable<WeightedWordCount> CleanTextAndRegroup(this IEnumerable<WeightedWordCount> results)
        {
            var cleanResults = results
                .Select(r => new WeightedWordCount
                {
                    Text = CleanText(r.Text),
                    Result = r.Result,
                    UnweightedResult = r.UnweightedResult
                })
                .Where(r => !string.IsNullOrEmpty(r.Text));

            return cleanResults.Regroup();
        }

        public static IEnumerable<WeightedWordCount> ApplyExclusionList(this IEnumerable<WeightedWordCount> results, string[] exclusionList)
        {
            if (exclusionList == null)
                return results;

            return results
                .Where(r => !exclusionList.Any(bannedWord => 
                    bannedWord.StartsWith("=") 
                        ? r.Text.Equals(bannedWord.Substring(1), StringComparison.CurrentCultureIgnoreCase)
                        : r.Text.IndexOf(bannedWord, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        private static IEnumerable<WeightedWordCount> Regroup(this IEnumerable<WeightedWordCount> results)
        {
            return results
                .GroupBy(r => r.Text, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => new WeightedWordCount
                    {Text = g.Key, Result = g.Sum(r => r.Result), UnweightedResult = g.Sum(r => r.UnweightedResult)});
        }
        
        private static string CleanText(string input)
        {
            var supportedCharsString = DetectUnsupportedCharsRegex.Replace(input, "");
            var sb = new StringBuilder();
            bool lastCharWasSpace = true;

            for (int i = 0; i < supportedCharsString.Length; i++)
            {
                var character = supportedCharsString[i];
                bool isExtraSpace = lastCharWasSpace && char.IsWhiteSpace(character);
                if (isExtraSpace || IsPunctuationOrSymbol(character))
                    continue;

                sb.Append(character);

                lastCharWasSpace = char.IsWhiteSpace(character);
            }

            if (sb.Length > 0 && char.IsWhiteSpace(sb[sb.Length - 1]))
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private static bool IsPunctuationOrSymbol(char c)
            => char.IsSymbol(c) || char.IsPunctuation(c);
    }
}
