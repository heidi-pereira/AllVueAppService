using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.Variable
{
    public class VariableConfigurationRepository : IVariableConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private List<VariableConfiguration> _allConfigs;

        public VariableConfigurationRepository(IDbContextFactory<MetaDataContext> dbContextFactory, IProductContext productContext)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
        }

        private IQueryable<VariableConfiguration> VariableConfigurationsInScope(MetaDataContext dbContext, bool tracking)
        {
            return dbContext.VariableConfigurations.For(_productContext, tracking);
        }

        public IReadOnlyCollection<VariableConfiguration> GetAll()
        {
            if (_allConfigs == null)
            {
                lock (this)
                {
                    if (_allConfigs == null)
                    {
                        using var dbContext = _dbContextFactory.CreateDbContext();
                        _allConfigs = VariableConfigurationsInScope(dbContext, false).ToList();
                    }
                }

                return _allConfigs.ToArray();
            }

            return _allConfigs.ToArray();
        }

        public IReadOnlyCollection<VariableConfiguration> GetBaseVariables()
        {
            return GetAll().Where(v => v.IsBaseVariable()).ToArray();
        }

        public VariableConfiguration Get(int id)
        {
            return GetAll().SingleOrDefault(v => v.Id == id);
        }

        public VariableConfiguration GetByIdentifier(string variableIdentifier)
        {
            //
            //This used to go to the database, and was hence case in-sensitive
            //
            return GetAll().SingleOrDefault(v => string.Equals(v.Identifier, variableIdentifier, StringComparison.OrdinalIgnoreCase));
        }

        public VariableConfiguration Create(VariableConfiguration variableConfiguration, IReadOnlyCollection<string> overrideVariableDependencyIdentifiers)
        {
            // Update cached Python expression before saving
            UpdateCachedPythonExpression(variableConfiguration);
            
            using var dbContext = _dbContextFactory.CreateDbContext();

            var newVariableConfiguration = Create(variableConfiguration, dbContext, overrideVariableDependencyIdentifiers);
            dbContext.SaveChanges();
            _allConfigs?.Add(newVariableConfiguration);
            return newVariableConfiguration;
        }

        internal VariableConfiguration Create(VariableConfiguration variableConfiguration,
            MetaDataContext dbContext,
            IReadOnlyCollection<string> overrideVariableDependencyIdentifiers)
        {
            if (overrideVariableDependencyIdentifiers != null)
            {
                var dependencies = VariableConfigurationsInScope(dbContext, true)
                    .Where(v => overrideVariableDependencyIdentifiers.Contains(v.Identifier));
                foreach (var v in dependencies)
                {
                    variableConfiguration.VariableDependencies.Add(new VariableDependency
                        { VariableId = variableConfiguration.Id, DependentUponVariableId = v.Id });
                }
            }

            var newVariableConfiguration = dbContext.Add(variableConfiguration);

            if (variableConfiguration.VariableDependencies != null)
            {
                dbContext.AddRange(variableConfiguration.VariableDependencies);
            }

            return newVariableConfiguration.Entity;
        }

        public void Update(VariableConfiguration variableConfiguration)
        {
            // Update cached Python expression before saving
            UpdateCachedPythonExpression(variableConfiguration);
            
            using var dbContext = _dbContextFactory.CreateDbContext();
            Update(variableConfiguration, dbContext);
            dbContext.SaveChanges();
        }

        public void UpdateMany(IEnumerable<VariableConfiguration> variableConfigurations)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            foreach (var variableConfiguration in variableConfigurations)
            {
                UpdateCachedPythonExpression(variableConfiguration);
                Update(variableConfiguration, dbContext);
            }
            dbContext.SaveChanges();
        }

        internal void Update(VariableConfiguration variableConfiguration, MetaDataContext dbContext)
        {
            var existingEntity = dbContext.VariableConfigurations
                .Include(v => v.VariableDependencies)
                .Single(v => v.Id == variableConfiguration.Id);

            // Apply changes to the existing entity
            // - stops EF fanning out updates across large number of linked entities (e.g. metrics / other variables)
            dbContext.Entry(existingEntity).CurrentValues.SetValues(variableConfiguration);
            UpdateDependencies(dbContext, existingEntity.VariableDependencies, variableConfiguration.VariableDependencies);

            _allConfigs.RemoveAll(m => m.Id == variableConfiguration.Id);
            _allConfigs.Add(variableConfiguration);

            static void UpdateDependencies(DbContext dbContext, ICollection<VariableDependency> existing, ICollection<VariableDependency> updated)
            {
                if (updated == null) return;

                var updatedIds = new HashSet<int>(updated.Select(u => u.DependentUponVariableId));

                var toRemove = existing.Where(e => !updatedIds.Contains(e.DependentUponVariableId)).ToList();
                dbContext.RemoveRange(toRemove);

                var existingIds = new HashSet<int>(existing.Select(e => e.DependentUponVariableId));

                var toAdd = updated.Where(u => !existingIds.Contains(u.DependentUponVariableId)).ToList();
                dbContext.AddRange(toAdd);
            }
        }

        public void Delete(VariableConfiguration variableConfiguration)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existing = VariableConfigurationsInScope(dbContext, true)
                .Include(v => v.VariableDependencies)
                .Include(v => v.VariablesDependingOnThis)
                .Single(v => v.Id == variableConfiguration.Id);

            if (existing.VariablesDependingOnThis.Any())
            {
                throw new BadRequestException($"Cannot delete variable referenced by: {existing.VariablesDependingOnThis.Select(v => v.Variable.DisplayName).JoinAsQuotedList()}");
            }

            dbContext.RemoveRange(existing.VariableDependencies);
            dbContext.VariableConfigurations.Remove(existing);
            dbContext.SaveChanges();
            _allConfigs.RemoveAll(m => m.Id == variableConfiguration.Id);
        }

        private void UpdateCachedPythonExpression(VariableConfiguration variableConfiguration)
        {
            if (variableConfiguration.Definition is EvaluatableVariableDefinition evaluatableDefinition)
            {
                try
                {
                    // Use the existing GetPythonExpression extension method to calculate the cached expression
                    evaluatableDefinition.CachedPythonExpression = variableConfiguration.Definition.GetPythonExpression();
                }
                catch (Exception)
                {
                    // If Python expression generation fails, set to null
                    // This allows the variable to be saved, and the expression can be updated later
                    evaluatableDefinition.CachedPythonExpression = null;
                }
            }
        }

        internal void ClearCache() => _allConfigs = null;
    }
}
