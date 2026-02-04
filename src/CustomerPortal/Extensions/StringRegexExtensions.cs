using System.Text.RegularExpressions;

namespace CustomerPortal.Extensions
{
    public static class StringRegexExtensions
    {
        private const string ReplaceCharacter = "-";
        private static readonly Regex OneOrMoreUnwantedCharacters = new Regex(@"[^A-Za-z0-9]+", RegexOptions.Compiled);
        private static readonly Regex LeadingOrTrailingCharacterRegex = new Regex($@"^\{ReplaceCharacter}|\{ReplaceCharacter}$", RegexOptions.Compiled);

        public static string SanitizeUrlSegment(this string input)
        {
            return input
                .RemoveUnwantedCharacters()
                .RemoveLeadingOrTrailingCharacter()
                .ToLower();
        }

        private static string RemoveLeadingOrTrailingCharacter(this string input) =>
            LeadingOrTrailingCharacterRegex.Replace(input, "");

        private static string RemoveUnwantedCharacters(this string input) =>
            OneOrMoreUnwantedCharacters.Replace(input, ReplaceCharacter);
    }
}
