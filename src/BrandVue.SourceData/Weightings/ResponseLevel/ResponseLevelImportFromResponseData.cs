using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.Weightings.ResponseLevel
{
    public class ResponseLevelImportFromResponseData
    {
        private readonly IResponseWeightingRepository _responseWeightingRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IAnswerDbContextFactory _answerDbContextFactory;

        public ResponseLevelImportFromResponseData( IResponseWeightingRepository responseWeightingRepository, ISubsetRepository subsetRepository, IAnswerDbContextFactory answerDbContextFactory)
        {
            _responseWeightingRepository = responseWeightingRepository;
            _subsetRepository = subsetRepository;
            _answerDbContextFactory = answerDbContextFactory;
        }

        private async Task<IList<ResponseWeightConfiguration>> GenerateWeights(string varCode, IEnumerable<int> surveyIds, decimal ?defaultWeight)
        {
            await using var dbContext = _answerDbContextFactory.CreateDbContext();

            var tuples = await dbContext.Answers.Where(a =>
                    a.Question.VarCode == varCode
                    && surveyIds.Contains(a.Response.SurveyId)
                    && a.Response.Status == SurveyCompletionStatus.Completed)
                    .Select(a => new {a.ResponseId, a.AnswerText}).ToListAsync();
            if (!tuples.Any())
            {
                throw new Exception($"Failed to generate response level weighting as there is no data for varCode='{varCode}'");
            }
            var result = tuples
                .Select(t =>
                {
                    if (!decimal.TryParse(t.AnswerText, out var weight) || (weight < 0))
                    {
                        if (defaultWeight.HasValue)
                        {
                            weight = defaultWeight.Value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    return new ResponseWeightConfiguration
                    {
                        RespondentId = t.ResponseId,
                        Weight = weight
                    };
                })
                .ToList();
            if (!defaultWeight.HasValue)
            {
                result = result.Where(r => r != null).ToList();
                if (!result.Any())
                {
                    throw new Exception($"Failed to generate response level weighting as there is no valid data for varCode='{varCode}'");
                }
            }
            return result;
        }

        public async Task<bool> InsertResponseWeights(string subsetId, string varCode, decimal ?defaultWeight)
        {
            if (!_subsetRepository.TryGet(subsetId, out var subset)) return false;
            
            var weights = await GenerateWeights(varCode, subset.SurveyIdToSegmentNames.Keys, defaultWeight);
            return _responseWeightingRepository.CreateResponseWeightsForRoot(subsetId, weights);
        }
    }
}
