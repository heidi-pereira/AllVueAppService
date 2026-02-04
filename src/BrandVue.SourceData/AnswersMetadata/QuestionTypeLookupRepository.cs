using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.AnswersMetadata
{
    public class QuestionTypeLookupRepository : ILoadableQuestionTypeLookupRepository
    {
        private readonly Dictionary<Subset, Dictionary<string, MainQuestionType>> _questionTypeLookupBySubset = new();
        private readonly Subset[] _allSubsets;

        public QuestionTypeLookupRepository(IMeasureRepository measureRepository, ISubsetRepository subsetRepository)
        {
            _allSubsets = subsetRepository.ToArray();
            foreach (var metric in measureRepository.GetAllForCurrentUser())
            {
                AddOrUpdate(metric);
            }
        }

        public void AddOrUpdate(Measure measure)
        {
            var subsetsToAdd = measure.Subset?.Any() == true ? measure.Subset : _allSubsets;
            foreach (var subset in subsetsToAdd)
            {
                AddOrUpdate(measure, subset);
            }

            foreach (var subset in _allSubsets.Except(subsetsToAdd))
            {
                Remove(measure, subset);
            }
        }

        public void Remove(Measure measure)
        {
            foreach (var subset in measure.Subset?.DefaultIfEmpty() ?? _allSubsets)
            {
                Remove(measure, subset);
            }
        }

        private void Remove(Measure measure, Subset subset)
        {
            if (_questionTypeLookupBySubset.TryGetValue(subset, out var lookup))
            {
                lookup.Remove(measure.Name);
            }
        }

        private void AddOrUpdate(Measure metric, Subset subset)
        {
            var accessModel = metric.GetFieldDependencies().FirstOrDefault()?.GetDataAccessModelOrNull(subset.Id);
            var subsetLookup = GetOrAddSubsetLookup(subset);
            var mainQuestionType = GetMainQuestionType(metric, accessModel);
            subsetLookup[metric.Name] = mainQuestionType;
        }

        private Dictionary<string, MainQuestionType> GetOrAddSubsetLookup(Subset subset)
        {
            if (!_questionTypeLookupBySubset.TryGetValue(subset, out var lookup))
            {
                _questionTypeLookupBySubset[subset] = lookup = new Dictionary<string, MainQuestionType>();
            }

            return lookup;
        }

        public IDictionary<string, MainQuestionType> GetForSubset(Subset subset) =>
            _questionTypeLookupBySubset.TryGetValue(subset, out var lookup) ? lookup : new Dictionary<string, MainQuestionType>();

        private static MainQuestionType GetMainQuestionType(Measure metric,
            FieldDefinitionModel accessModel)
        {
            if (metric.GenerationType == AutoGenerationType.CreatedFromNumeric)
            {
                return MainQuestionType.GeneratedNumeric;
            }

            if (metric.IsBasedOnCustomVariable)
            {
                if (!metric.EntityCombination.Any())
                {
                    return MainQuestionType.Value;
                }
                return accessModel?.QuestionModel?.QuestionType ?? MainQuestionType.CustomVariable;
            }

            return accessModel?.QuestionModel?.QuestionType ?? MainQuestionType.Unknown;
        }
    }
}