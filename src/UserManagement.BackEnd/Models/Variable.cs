namespace UserManagement.BackEnd.Models
{
    public record SurveySegment (string Id, string Name);
    public record Variable(int Id, string Name, string Description, List<VariableOption> Options, IList<SurveySegment> SurveySegments, string AnswerType, string CalculationType, bool IsHiddenInAllVue, int Count);
}
