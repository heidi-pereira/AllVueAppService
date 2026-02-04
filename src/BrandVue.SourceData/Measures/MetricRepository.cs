using Vue.Common.Auth;

namespace BrandVue.SourceData.Measures
{
    public class MetricRepository : BaseRepository<Measure, string>, ILoadableMetricRepository
    {
        private IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;

        public MetricRepository(IUserDataPermissionsOrchestrator userPermissionsOrchestrator)
        {
            _userDataPermissionsOrchestrator = userPermissionsOrchestrator ?? throw new ArgumentNullException(nameof(userPermissionsOrchestrator));
        }

        protected override void SetIdentity(Measure target, string identity)
        {
            target.Name = identity;
        }

        public IEnumerable<Measure> GetAll()
        {
            lock (_lock)
            {
                return _objectsById.Values.ToArray();
            }
        }

        public IEnumerable<Measure> GetAllForCurrentUser()
        {
            return GetAllMeasuresForUser().ToArray();
        }

        public IEnumerable<Measure> GetAllMeasuresWithDisabledPropertyFalseForSubset(Subset subset)
        {
            bool Included(Measure p) => !p.Disabled && (p.Subset == null || p.Subset.Any(s => s.Id == subset.Id)) && p.GetFieldDependencies().All(f => f.IsAvailableForSubset(subset.Id));
            
            return GetAllMeasuresForUser().Where(Included).ToArray();
        }

        public IEnumerable<Measure> GetAllMeasuresIncludingDisabledForSubset(Subset subset)
        {
            bool Included(Measure p) => (p.Subset == null || p.Subset.Any(s => s.Id == subset.Id)) && p.GetFieldDependencies().All(f => f.IsAvailableForSubset(subset.Id));

            return GetAllMeasuresForUser().Where(Included);
        }

        private IEnumerable<Measure> GetAllMeasuresForUser()
        {
            var permission = _userDataPermissionsOrchestrator.GetDataPermission();
            if (permission == null || permission.VariableIds.Count == 0)
            {
                lock (_lock)
                {
                    return _objectsById.Values;
                }
            }

            return GetMeasuresByVariableConfigurationId(permission.VariableIds);
        }

        private IEnumerable<Measure> GetMeasuresByVariableConfigurationId(ICollection<int> variableIds)
        {
            // We want to limit to the questions that the user has permissions to see plus any measures based on those questions
            // It is assumed that PrimaryFieldDependencies are always the base variables (questions) even when variables are created from other variables
            // therefore we don't need to do any recursive searching of dependencies
            // We also include any measures that do not have any field dependencies (e.g. waves) as these cannot be restricted by the permissions

            var variableIdHashSet = new HashSet<int>(variableIds);
            var questions = GetQuestionsByVariableConfigurationId(variableIdHashSet);
            var questionIdentifiers = new HashSet<string>(questions.Select(q => q.PrimaryVariableIdentifier));

            bool Included(Measure measure) => (measure.VariableConfigurationId.HasValue
                && variableIdHashSet.Contains(measure.VariableConfigurationId.Value)
                || !measure.PrimaryFieldDependencies.Any()
                || measure.PrimaryFieldDependencies.All(dependency => questionIdentifiers.Contains(dependency.Name)));

            lock (_lock)
            {
                return _objectsById.Values.Where(Included);
            }
        }

        private IEnumerable<Measure> GetQuestionsByVariableConfigurationId(HashSet<int> variableIdHashSet)
        {
            bool Included(Measure measure) => (measure.VariableConfigurationId.HasValue &&
                                               variableIdHashSet.Contains(measure.VariableConfigurationId.Value));

            lock (_lock)
            {
                return _objectsById.Values.Where(Included);
            }
        }

        public IEnumerable<Measure> GetMeasuresByVariableConfigurationIds(List<int> variableIds)
        {
            var variableIdHashSet = new HashSet<int>(variableIds);
            return GetQuestionsByVariableConfigurationId(variableIdHashSet);
        }

        public void RenameMeasure(Measure measure, string newName)
        {
            if (measure.Name != newName)
            {
                if (!TryAdd(newName, measure))
                {
                    throw new InvalidOperationException($"Measure already exists with name {newName}");
                }
                Remove(measure.Name);
                measure.Name = newName;
            }
        }
    }
}
