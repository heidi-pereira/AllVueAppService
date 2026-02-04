using System.Text.RegularExpressions;
using BrandVue.EntityFramework.Answers.Model;
using Humanizer;

namespace BrandVue.SourceData.Import
{
    public class NameGenerator
    {
        private static readonly Regex FieldNameRegex = new Regex("^[a-z]{0,3}[0-9]+[a-z]?$", RegexOptions.IgnoreCase);
        private static readonly Regex RemoveNonSentenceCharactersRegex = new Regex(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex RemoveNonWordCharactersRegex = new Regex(@"\W", RegexOptions.Compiled);
        private static readonly Regex RemoveTagsRegex = new Regex(@"#.+#", RegexOptions.Compiled);
        private readonly Dictionary<string, int> _questionWordOccurrences;

        public NameGenerator(IEnumerable<Question> questions)
        {
            _questionWordOccurrences = questions
                .SelectMany(q => GetNonTagWords(q.QuestionText)).GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        }

        public string GenerateFieldName(Question question)
        {
            string fieldName = GenerateFieldNameInternal(question);
            return EnsureValidPythonIdentifier(fieldName);
        }

        /// <summary>
        /// Creates a python identifier following https://docs.python.org/3/reference/lexical_analysis.html#identifiers
        /// to be used in IronPython
        /// "Within the ASCII range (U+0001..U+007F), the valid characters for identifiers are the same as in Python 2.x:
        /// the uppercase and lowercase letters A through Z, the underscore _ and, except for the first character, the digits 0 through 9."
        /// </summary>
        public static string EnsureValidPythonIdentifier(string input)
        {
            string name = RemoveNonWordCharactersRegex.Replace(input, "");
            if (char.IsDigit(name[0]))
            {
                // python identifiers cannot start with a digit
                return $"_{name}";
            }

            return name;
        }

        private string GenerateFieldNameInternal(Question question)
        {
            if (!FieldNameRegex.Match(question.VarCode).Success)
            {
                return question.VarCode;
            }

            var threeRarestWords = GetThreeRarestWords(question.QuestionText);
            return $"{string.Join("_", threeRarestWords.Select(w => w.ToLowerInvariant()))}_{question.VarCode}";
        }

        public string GenerateMeasureName(Question question, HashSet<string> usedMeasureNames)
        {
            if (!usedMeasureNames.Comparer.Equals(StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(usedMeasureNames), "Must use OrdinalIgnoreCase comparer");
            }

            string generatedName;
            if (!FieldNameRegex.Match(question.VarCode).Success)
            {
                generatedName = question.VarCode.Humanize(LetterCasing.Sentence);
            }
            else
            {
                var threeRarestWords = GetThreeRarestWords(question.QuestionText);
                generatedName = string.Join(" ", threeRarestWords.Select(w => w.ToLowerInvariant())) + $" ({question.VarCode})".Humanize(LetterCasing.Sentence);
            }

            // Handle duplicate names
            var suggestedName = generatedName;
            var suffixNumber = 2;
            while (!usedMeasureNames.Add(suggestedName))
            {
                suggestedName = $"{generatedName}_{suffixNumber}";
                suffixNumber++;
            }

            return suggestedName;
        }

        private static IEnumerable<string> GetNonTagWords(string questionText)
        {
            var cleanQuestionText = RemoveTagsRegex.Replace(questionText, " ");
            cleanQuestionText = RemoveNonSentenceCharactersRegex.Replace(questionText, "");
            return cleanQuestionText.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 2);
        }

        private IEnumerable<string> GetThreeRarestWords(string questionText)
        {
            var words = GetNonTagWords(questionText);
            var threeRarestWords = words.Select((word, originalIndex) => (word, originalIndex))
                .OrderBy(w => _questionWordOccurrences[w.word]).Take(3)
                .OrderBy(w => w.originalIndex) // Use original word order to minimize chances of sounding like Yoda
                .Select(w => w.word);
            return threeRarestWords;
        }
    }
}
