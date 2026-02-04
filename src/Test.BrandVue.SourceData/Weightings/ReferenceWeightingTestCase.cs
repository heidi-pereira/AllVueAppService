using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.SourceData.Weightings
{
    internal class QuotaCellQuestionAndInstances
    {
        public string QuestionName { get; init; }
        public int[] InstanceIds { get; private set; }
        public QuotaCellQuestionAndInstances(string questionName, int[] instances)
        {
            QuestionName = questionName;
            InstanceIds = instances;
        }

        public void AddInstanceId(int id)
        {
            if (!InstanceIds.Any(x => x == id))
            {
                var temp = InstanceIds.ToList();
                temp.Add(id);
                InstanceIds = temp.ToArray();
            }
        }
        public string TypeName => $"{QuestionName}Type";
        public string SingleName => $"{QuestionName}";
        public string PluralName => $"{QuestionName}s";
    }
    internal record NumberOfResponsesForQuotaCell(string QuotaCellAsString, int NumberOfResponses);

    internal record ReferenceWeightingTestCase(List<QuotaCellQuestionAndInstances> Descriptors, List<NumberOfResponsesForQuotaCell> DataDistribution)
    {
        public ReferenceWeightingTestCase AddQuestionForQuotaAndInstances(string name, int[] values)
        {
            Descriptors.Add(new QuotaCellQuestionAndInstances(name, values));
            return this;
        }
        public ReferenceWeightingTestCase AddNumberOfResponsesForQuotaCell(string quotaCellAsString, int numberOfResponses)
        {
            DataDistribution.Add(new NumberOfResponsesForQuotaCell(quotaCellAsString, numberOfResponses));
            return this;
        }

        public string SubsetName => "UK";
        public int SurveyId => 1;
    }
}
