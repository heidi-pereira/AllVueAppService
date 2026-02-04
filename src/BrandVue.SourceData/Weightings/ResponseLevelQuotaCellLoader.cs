using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Weightings;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Weightings
{

    public interface IResponseLevelQuotaCellLoader
    {
        public ResponseWeighting GetPossibleRootResponseWeightingsForSubset();
        public ResponseWeighting GetPossibleResponseWeightings(WeightingTargetConfiguration target);
        public ResponseWeighting GetPossibleResponseWeightings(WeightingTarget target);
    }

    public class ResponseLevelQuotaCellLoader : IResponseLevelQuotaCellLoader
    {
        private readonly ILogger<ResponseLevelQuotaCellLoader> _logger;
        private readonly IResponseWeightingRepository _responseWeightingRepository;
        private readonly Subset _subset;
        private Dictionary<int, ResponseWeighting> _weightings = new();
        private const int MaxAllowableResponseLevelDepthForPerformanceReasons = 2;
        private const int SynthesizedRoot_NonValidTargetId = -1;

        public ResponseLevelQuotaCellLoader(ILogger<ResponseLevelQuotaCellLoader> logger, IResponseWeightingRepository responseWeightingRepository, Subset subset)
        {
            _logger = logger;
            _responseWeightingRepository = responseWeightingRepository;
            _subset = subset;
        }
        public static string GetMagicFieldName => $"RespondentId";
        
        private bool CanResponseLevelWeightingBeAdded(WeightingTargetConfiguration target)
        {
            var responseLevelWeightingCanBeAdded = false;
            WeightingPlanConfiguration parentPlan = target.ParentWeightingPlan;

            for (int depthCount = 0; depthCount < MaxAllowableResponseLevelDepthForPerformanceReasons; depthCount++)
            {
                if (parentPlan is null)
                {
                    responseLevelWeightingCanBeAdded = true;
                    break;
                }
                parentPlan = parentPlan.ParentTarget?.ParentWeightingPlan;
            }

            return responseLevelWeightingCanBeAdded;
        }

        public ResponseWeighting GetPossibleRootResponseWeightingsForSubset()
        {
            var key = SynthesizedRoot_NonValidTargetId;

            if (!_weightings.ContainsKey(key))
            {
                _weightings[key] = GetRootWeightsForSubset();
            }
            return _weightings[key];
        }

        public ResponseWeighting GetPossibleResponseWeightings(WeightingTargetConfiguration target)
        {
            if (target is null)
            {
                return GetPossibleRootResponseWeightingsForSubset();
            }

            var key = target.Id;

            if (!_weightings.ContainsKey(key))
            {
                if (CanResponseLevelWeightingBeAdded(target))
                {
                    _weightings[key] = GetWeights(target);
                }
                else
                {
                    _weightings[key] = null;
                }
            }
            return _weightings[key];
        }

        public ResponseWeighting GetPossibleResponseWeightings(WeightingTarget target)
        {
            if (target is null)
            {
                return GetPossibleRootResponseWeightingsForSubset();
            }

            var key = target.ExistingDatabaseId.Value;

            if (!_weightings.ContainsKey(key))
            {
                _weightings[key] = GetWeights(target);
            }
            return _weightings[key];
        }

        private ResponseWeighting GetWeights(WeightingTarget target)
        {
            int counter = 0;
            try
            {
                if (target.ResponseWeightingContext == null) return null;
                
                var result = new ResponseWeighting(GetMagicFieldName, new Dictionary<int, int>(), new Dictionary<int, decimal>());

                var responsesGroupedByWeight = target.ResponseWeightingContext.ResponseWeights.GroupBy(responseWeight => responseWeight.Weight);
                foreach (var group in responsesGroupedByWeight)
                {
                    foreach (var respondentWeight in group)
                    {
                        result.ResponseIdToQuotaCellId[respondentWeight.RespondentId] = counter;
                    }
                    result.QuotaCellIdToWeight[counter] = group.Key;
                    counter++;
                }
                return result;
            }
            catch (Exception)
            {
                _logger.LogError($"Error getting weights for target {target.ExistingDatabaseId}");
                throw;
            }
        }

        private ResponseWeighting GetWeights(WeightingTargetConfiguration target)
        {
            int counter = 0;
            try
            {
                if (target.ResponseWeightingContext == null) return null;
                
                var result = new ResponseWeighting(GetMagicFieldName, new Dictionary<int, int>(), new Dictionary<int, decimal>());
                
                var responsesGroupedByWeight = target.ResponseWeightingContext.ResponseWeights.GroupBy(responseWeight => responseWeight.Weight);
                foreach (var group in responsesGroupedByWeight)
                {
                    foreach (var respondentWeight in group)
                    {
                        result.ResponseIdToQuotaCellId[respondentWeight.RespondentId] = counter;
                    }
                    result.QuotaCellIdToWeight[counter] = group.Key;
                    counter++;
                }
                return result;
            }
            catch (Exception)
            {
                _logger.LogError($"Error getting weights for target {target.Id}");
                throw;
            }
        }

        private ResponseWeighting GetRootWeightsForSubset()
        {
            int counter = 0;
            try
            {
                var result = new ResponseWeighting(GetMagicFieldName, new Dictionary<int, int>(), new Dictionary<int, decimal>());
                var responseWeightingContext = _responseWeightingRepository.GetRootResponseWeightingContextWithWeightsForSubset(_subset.Id);

                if (responseWeightingContext == null) return null;

                var responsesGroupedByWeight = responseWeightingContext.ResponseWeights.GroupBy(responseWeight => responseWeight.Weight);
                foreach (var group in responsesGroupedByWeight)
                {
                    foreach (var respondentWeight in group)
                    {
                        result.ResponseIdToQuotaCellId[respondentWeight.RespondentId] = counter;
                    }
                    result.QuotaCellIdToWeight[counter] = group.Key;
                    counter++;
                }
                return result;
            }
            catch (Exception)
            {
                _logger.LogError($"Error getting weights for subset {_subset.Id}");
                throw;
            }
        }
    }
}
