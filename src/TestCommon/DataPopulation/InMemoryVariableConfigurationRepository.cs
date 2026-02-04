using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Variable;

namespace TestCommon.DataPopulation
{
    public class InMemoryVariableConfigurationRepository : IVariableConfigurationRepository
    {
        private int _nextId = 1;
        private readonly Dictionary<int, VariableConfiguration> _variables = new();
        public IReadOnlyCollection<VariableConfiguration> GetAll() => _variables.Values.ToArray();

        public IReadOnlyCollection<VariableConfiguration> GetBaseVariables()
        {
            return GetAll().Where(v => v.Definition is BaseGroupedVariableDefinition || v.Definition is BaseFieldExpressionVariableDefinition).ToArray();
        }

        public VariableConfiguration Get(int variableConfigurationId) => _variables.TryGetValue(variableConfigurationId, out var v) ? v : null;
        public VariableConfiguration GetByIdentifier(string variableIdentifier) => _variables.Values.SingleOrDefault(v => v.Identifier.Equals(variableIdentifier, StringComparison.OrdinalIgnoreCase));

        public VariableConfiguration Create(VariableConfiguration variableConfiguration,
            IReadOnlyCollection<string> overrideVariableDependencyIdentifiers)
        {
            var newvariableConfiguration = variableConfiguration with { Id = _nextId++ };
            foreach (var v in _variables.Values.Where(v =>
                         overrideVariableDependencyIdentifiers.Contains(v.Identifier)))
            {
                newvariableConfiguration.VariableDependencies.Add(new VariableDependency { VariableId = variableConfiguration.Id, DependentUponVariableId = v.Id });
            }
            return newvariableConfiguration;
        }

        public void Delete(VariableConfiguration variableConfiguration) => _variables.Remove(variableConfiguration.Id);

        public void Update(VariableConfiguration variableConfiguration)
        {
            _variables[variableConfiguration.Id] = variableConfiguration;
        }

        public void UpdateMany(IEnumerable<VariableConfiguration> variableConfigurations)
        {
            foreach (var variableConfiguration in variableConfigurations)
            {
                Update(variableConfiguration);
            }
        }
    }
}
