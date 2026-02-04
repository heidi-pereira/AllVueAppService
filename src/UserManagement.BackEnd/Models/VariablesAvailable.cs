namespace UserManagement.BackEnd.Models
{
    public record VariablesAvailable(List<SurveySegment> SurveySegments, List<Variable> UnionOfQuestions);
}