using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MIG.SurveyPlatform.MapGeneration
{
    internal static class HumanStringExtensions
    {
        private static readonly Regex _usableCharacters = new Regex("[^a-zA-Z0-9_]");
        private static readonly Regex _htmlTag = new Regex("<[^>]*>");


        private static readonly string[] StringsToLowerCase = new[]
            {
                "A", "Ago", "An", "And", "As", "At", "But", "By", "For", "From", "In", "Into", "It", "Next", "No", "Nor", "Of", "Off", "On", "Onto", "Or", "Over", "Past", "So", "The",
                "Till", "To", "Up", "With", "Yet"
            }.Select(x => " " + x + " ")
            .Concat(new[] {"'T", "'S", "Etc.", "E.G.", "I.E."}).ToArray();

        public static string Humanize(this string programmingName)
        {
            if (string.IsNullOrWhiteSpace(programmingName)) return programmingName;
            var withReplacements = programmingName.Replace("_", " ");
            return string.Join(": ", withReplacements.Split(':').Select(s => ToTitleCase(s.Trim())));
        }

        private static string ToTitleCase(string withReplacements)
        {
            if (withReplacements == string.Empty) return withReplacements;
            var overTitleCased = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(withReplacements).Trim();
            var titleCase = StringsToLowerCase.Aggregate(overTitleCased, (current, toLowercase) => current.Replace(toLowercase, toLowercase.ToLower()));
            return titleCase.Substring(0, 1).ToUpper() + titleCase.Substring(1);
        }


        public static string CreateAcronym(this string str, int dedupeSeverity)
        {
            var words = _htmlTag.Replace(str, " ").Split(new[] {' ', '/','&'})
                .Select(w => _usableCharacters.Replace(w, ""))
                .Where(w => w.Length > 0).ToList();
            var firstLetters = new string(words.SelectMany((w, i) => GetFirstBit(w, i, dedupeSeverity)).Take(4).ToArray());
            return firstLetters;
        }

        private static IEnumerable<char> GetFirstBit(string w, int index, int dedupeSeverity)
        {
            return FirstUppercase(w.Take(1 + dedupeSeverity / (index + 1)));
        }

        private static IEnumerable<char> FirstUppercase(IEnumerable<char> word)
        {
            return word.Select((c, i) => i == 0 ? char.ToUpper(c) : c);
        }
    }
}