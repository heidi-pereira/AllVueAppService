using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Variable;

namespace BrandVue.Services.Entity
{
    public class EntityFilterByPermissions 
    {
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;

        private Dictionary<string, IList<int>> _lookUpByFilteredEntityType;

        public EntityFilterByPermissions(IVariableConfigurationRepository variableConfigurationRepository, 
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
            _userDataPermissionsOrchestrator = userDataPermissionsOrchestrator;
            Initialize();
        }

        private void Initialize()
        {
            var permission = _userDataPermissionsOrchestrator.GetDataPermission();
            var filters = permission?.Filters;

            _lookUpByFilteredEntityType = new Dictionary<string, IList<int>>();
            if (filters == null || !filters.Any())
            {
                return;
            }
            foreach (var filter in filters)
            {

                var config = _variableConfigurationRepository.Get(filter.VariableConfigurationId);
                var definition = config.Definition as QuestionVariableDefinition;
                if (definition?.EntityTypeNames.Count() == 1)
                {
                    _lookUpByFilteredEntityType[definition.EntityTypeNames.Single().EntityTypeName] = filter.EntityInstanceId;
                }
            }
        }

        private bool IsEntityInstancesAvailable(string identifier, EntityInstance instance)
        {
            if (!_lookUpByFilteredEntityType.TryGetValue(identifier, out var item))
            {
                return true;
            }
            return item.Contains(instance.Id);
        }

        public IReadOnlyCollection<EntityInstance> Filter(string identifier,
            IReadOnlyCollection<EntityInstance> instances)
        {
            return instances.Where(x => IsEntityInstancesAvailable(identifier, x)).ToList();

        }
    }
}
