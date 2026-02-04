using BrandVue.EntityFramework.Answers.Model;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.Averages
{
    public static class AverageHelper
    {
        public static AverageType[] VerifyAverageTypesForQuestionType(AverageType[] averages, MainQuestionType questionType)
        {
            if (questionType == MainQuestionType.SingleChoice)
            {
                return averages.Select(average => IsTypeOfMean(average) ? AverageType.EntityIdMean : average).ToArray();
            }

            return averages;
        }

        public static string GetAverageDisplayText(AverageType average)
        {
            return $"Average ({(IsTypeOfMean(average) ? "mean" : average.ToString().ToLower())})";
        }

        public static bool IsTypeOfMean(AverageType average)
        {
            return average == AverageType.Mean || average == AverageType.ResultMean || average == AverageType.EntityIdMean;
        }
    }
}
