using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.SourceData.AnswersMetadata
{
    public interface IChoiceSetReader
    {
        int[] GetSegmentIds(Subset subset);
        IEnumerable<SurveySegment> GetSegments(IEnumerable<int> surveyIds);

        (IReadOnlyCollection<Question> questions, IReadOnlyCollection<ChoiceSetGroup> choiceSets) GetChoiceSetTuple(IReadOnlyCollection<int> surveyIds);

        IReadOnlyCollection<AnswerStat> GetAnswerStats(IReadOnlyCollection<int> surveyIds,
            IReadOnlyCollection<int> segmentIds);
        bool SurveyHasNonTestCompletes(IEnumerable<int> surveyIds);
        IReadOnlyList<int> GetSurveyChoiceIds(ChoiceSet choiceSet);

        void InvalidateCache(IEnumerable<int> surveyIds);
    }
}