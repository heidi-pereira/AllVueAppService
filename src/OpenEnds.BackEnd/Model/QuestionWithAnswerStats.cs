namespace OpenEnds.BackEnd.Model
{
    public record QuestionWithAnswerStats(
    VueQuestion Question,
    int MaxLength,
    int AnswerCount
);
}
