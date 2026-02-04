using MIG.SurveyPlatform.Data.FieldMapping;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    internal static class FieldForOutputExtensions
    {
        public static string GetVarRoot(this ExportMaker.FieldForOutput fo)
        {
            return !string.IsNullOrEmpty(fo.Question.BaseVariableCode) ? fo.Question.BaseVariableCode : fo.Question.VariableCode;
        }
    }
}