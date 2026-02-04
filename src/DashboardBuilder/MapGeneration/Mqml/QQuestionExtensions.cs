using System;
using System.Linq;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    internal static class QQuestionExtensions
    {
        public static bool TextsContain(this QQuestion question, string needle, StringComparison comparer = StringComparison.CurrentCulture)
        {
            return question.GetQuestionTexts().Any(text => text.IndexOf(needle, comparer) > -1);
        }

        public static string[] GetQuestionTexts(this QQuestion q)
        {
            return new[] { q.LongQuestion, q.Parent.MainTitle, q.Parent.SubTitle, q.Parent.PageQuestion };
        }
    }
}