using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;

namespace BrandVue.Services
{
    public class NetManager : INetManager
    {
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IProductContext _productContext;
        private readonly IPartsRepository _partsRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IVariableFactory _variableFactory;
        private readonly IVariableConfigurationFactory _variableConfigurationFactory;
        private readonly IVariableManager _variableManager;
        private readonly object _lockVariableConfiguration = new();

        public NetManager(IVariableConfigurationRepository variableConfigurationRepository,
            IEntityRepository entityRepository,
            IProductContext productContext,
            IMetricConfigurationRepository metricConfigurationRepository,
            IPartsRepository partsRepository,
            IMeasureRepository measureRepository,
            ISubsetRepository subsetRepository,
            IVariableFactory variableFactory,
            IVariableConfigurationFactory variableConfigurationFactory,
            IVariableManager variableManager)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
            _entityRepository = entityRepository;
            _productContext = productContext;
            _metricConfigurationRepository = metricConfigurationRepository;
            _partsRepository = partsRepository;
            _measureRepository = measureRepository;
            _subsetRepository = subsetRepository;
            _variableFactory = variableFactory;
            _variableConfigurationFactory = variableConfigurationFactory;
            _variableManager = variableManager;
        }

        public void CreateNewNet(string selectedSubsetId, MetricConfiguration metric, int partId, string netName, int[] nettedEntityInstanceIds)
        {
            var subset = _subsetRepository.Get(selectedSubsetId);
            var part = _partsRepository.GetById(partId);
            string uniqueName = metric.Name + "NET" + Guid.NewGuid();

            IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers;
            var variableConfig = CreateVariableConfigForNet(subset, metric, netName, nettedEntityInstanceIds, part, uniqueName, out dependencyVariableInstanceIdentifiers);
            var createdVariable = _variableConfigurationRepository.Create(variableConfig, dependencyVariableInstanceIdentifiers);
            try
            {
                var createdMetric = CreateNewMetricForNetting(uniqueName, createdVariable, metric);
                UpdateReportPartWithNewVariable(partId, variableConfig, createdMetric, createdVariable.Definition as GroupedVariableDefinition, metric.Field);
            }
            catch (Exception)
            {
                _variableConfigurationRepository.Delete(createdVariable);
                throw;
            }
        }

        public void AddGroupToNet(MetricConfiguration metric, int partId, string netName, ICollection<int> nettedEntityInstanceIds)
        {
            if (!metric.VariableConfigurationId.HasValue)
            {
                throw new NotFoundException(
                    $"Unable to update net, variable identifier not found. (metric name: {metric.Name})");
            }

            var part = _partsRepository.GetById(partId);

            var originalMetric = _metricConfigurationRepository.Get(metric.OriginalMetricName);
            string fromVariableIdentifier = FromVariableIdentifier(originalMetric);
            lock (_lockVariableConfiguration)
            {
                var existingVariable = _variableConfigurationRepository.Get(metric.VariableConfigurationId.Value);
                var definition = existingVariable.Definition as GroupedVariableDefinition;
                var firstComponent = (InstanceListVariableComponent)definition.Groups.First(g => g.Component is InstanceListVariableComponent).Component;
                var nettedVariable = CreateNetVariableGroup(definition, netName, nettedEntityInstanceIds, fromVariableIdentifier, firstComponent.FromEntityTypeName, firstComponent.ResultEntityTypeNames);
                definition.Groups.Add(nettedVariable);

                _variableConfigurationRepository.Update(existingVariable);
                try
                {
                    metric.VariableConfiguration = existingVariable;
                    UpdateMetricFromVariable(metric, existingVariable);
                }
                catch (Exception)
                {
                    definition.Groups.Remove(nettedVariable);
                    _variableConfigurationRepository.Update(existingVariable);
                    throw;
                }

                AddCreatedInstanceIdToPart(part, definition);
                _partsRepository.UpdatePart(part);
            }
        }

        public void RemoveNet(string selectedSubsetId, int partId, string metricName, int netVariableId, int optionToRemove)
        {
            var metric = _metricConfigurationRepository.Get(metricName);
            var measure = _measureRepository.Get(metricName);

            var part = _partsRepository.GetById(partId);

            var originalMetric = _metricConfigurationRepository.Get(metric.OriginalMetricName);
            var originalMeasure = _measureRepository.Get(originalMetric.Name);

            var subset = _subsetRepository.Get(selectedSubsetId);

            var existingVariable = _variableConfigurationRepository.Get(netVariableId);
            var existingDefinition = existingVariable.Definition as GroupedVariableDefinition;

            var groupItemToRemove = existingDefinition.Groups.Find(group => group.ToEntityInstanceId == optionToRemove);
            if (groupItemToRemove == null)
            {
                throw new NotFoundException(
                    $"Unable to remove entityInstance id: {optionToRemove} from net id: {netVariableId} as entityInstance didn't exist in net");
            }

            existingDefinition.Groups.Remove(groupItemToRemove);

            var originalType = originalMeasure.EntityCombination.Except(measure.EntityCombination).Single();
            var originalInstances = _entityRepository.GetInstancesOf(originalType.Identifier, subset);

            /* We don't need to compare instances exactly as adding Nets is an additive operation */
            var hasNoNetsRemaining = originalInstances.Count == existingDefinition.Groups.Count;
            if (hasNoNetsRemaining)
            {
                RevertPartToOriginalMetric(existingVariable, metric, part, originalType, originalMetric, originalMeasure, subset);
            }
            else
            {
                _variableConfigurationRepository.Update(existingVariable);
                UpdateMetricFromVariable(metric, existingVariable);
            }

            RemoveSelectedInstanceIdFromPart(part, optionToRemove);
            _partsRepository.UpdatePart(part);
        }

        public void UpdateMetricFromVariable(MetricConfiguration metric, VariableConfiguration variable)
        {
            var groupedDefinition = (GroupedVariableDefinition)variable.Definition;
            _variableManager.UpdateVariableGroupValuesForMetric(metric, groupedDefinition);
            _metricConfigurationRepository.Update(metric);
        }

        private VariableConfiguration CreateVariableConfigForNet(Subset subset, MetricConfiguration metric, string netName, int[] nettedEntityInstanceIds, PartDescriptor part, string uniqueName, out IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers)
        {
            string fromVariableIdentifier = FromVariableIdentifier(metric);
            (string fromEntityTypeName, List<string> resultEntityTypeNames) = GetSplitByAndResultByTypes(part, metric, subset);

            var definition = CreateNetVariableDefinition(subset, fromVariableIdentifier, fromEntityTypeName, resultEntityTypeNames);
            var nettedVariable = CreateNetVariableGroup(definition, netName, nettedEntityInstanceIds, fromVariableIdentifier, fromEntityTypeName, resultEntityTypeNames);
            definition.Groups.Add(nettedVariable);

            var identifier = _variableConfigurationFactory.CreateIdentifierFromName(uniqueName);
            return _variableConfigurationFactory.CreateVariableConfigFromParameters(uniqueName, identifier, definition, out dependencyVariableInstanceIdentifiers, out _);
        }

        private string FromVariableIdentifier(MetricConfiguration metric)
        {
            if (metric.VariableConfigurationId.HasValue)
            {
                return _variableConfigurationRepository.Get(metric.VariableConfigurationId.Value).Identifier;
            }

            return metric.Field;
        }

        // This function is based off the frontend code `getSplitByAndFilterByEntityTypes` in `SurveyVueUtils.tsx`
        private string GetSplitByType(PartDescriptor part, Measure measure, MetricConfiguration metric, Subset subset)
        {
            var defaultSplitByEntityTypeName = metric.DefaultSplitByEntityType ?? "";
            if (!measure.EntityCombination.Any())
            {
                return null;
            }

            //single entity
            if (measure.EntityCombination.Count() == 1)
            {
                return measure.EntityCombination.First().Identifier;
            }

            //look on the part first
            if (part.MultipleEntitySplitByAndFilterBy is not null)
            {
                return part.MultipleEntitySplitByAndFilterBy.SplitByEntityType;
            }

            //multientity by default split by type
            if (!string.IsNullOrWhiteSpace(defaultSplitByEntityTypeName.Trim()))
            {
                string splitByType = measure.EntityCombination.ToList().Find(t => t.Identifier == defaultSplitByEntityTypeName)?.Identifier;
                if (string.IsNullOrWhiteSpace(splitByType))
                {
                    splitByType = measure.EntityCombination.First().Identifier;
                }

                return splitByType;
            }

            //multientity by best guess
            var orderedTypes = measure.EntityCombination.Select(type =>
            {
                var instances = _entityRepository.GetInstancesOf(type.Identifier, subset);
                return (Type: type.Identifier, NumInstances: instances.Count);
            }).OrderBy(a => a.NumInstances);
            return orderedTypes.First().Type;
        }

        private GroupedVariableDefinition CreateNetVariableDefinition(Subset subset,
            string fromVariableIdentifier, string fromEntityTypeName, List<string> resultEntityTypeNames)
        {
            var availableEntityInstances = _entityRepository.GetInstancesOf(fromEntityTypeName, subset);
            var groups = availableEntityInstances
                .Select(entity => new VariableGrouping
                {
                    ToEntityInstanceId = entity.Id,
                    ToEntityInstanceName = entity.Name,
                    Component = new InstanceListVariableComponent
                    {
                        FromVariableIdentifier = fromVariableIdentifier,
                        FromEntityTypeName = fromEntityTypeName,
                        InstanceIds = new List<int> { entity.Id },
                        ResultEntityTypeNames = resultEntityTypeNames
                    },
                }).ToList();

            var definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = $"netted: {fromEntityTypeName}",
                Groups = groups
            };
            return definition;
        }

        private VariableGrouping CreateNetVariableGroup(GroupedVariableDefinition existingDefinition, string netName,
            IEnumerable<int> entityInstanceIds, string fromVariableIdentifier, string fromEntityTypeName, List<string> resultEntityTypeNames)
        {
            return new VariableGrouping
            {
                ToEntityInstanceId = existingDefinition.Groups.Select(v => v.ToEntityInstanceId).Max() + 1,
                ToEntityInstanceName = netName,
                Component = new InstanceListVariableComponent
                {
                    FromVariableIdentifier = fromVariableIdentifier,
                    FromEntityTypeName = fromEntityTypeName,
                    InstanceIds = entityInstanceIds.ToList(),
                    ResultEntityTypeNames = resultEntityTypeNames
                },
            };
        }

        private (string SplitByTypeName, List<string> ResultEntityTypeNames) GetSplitByAndResultByTypes(PartDescriptor part, MetricConfiguration metric, Subset subset)
        {
            var measure = _measureRepository.Get(metric.Name);

            string splitByType = GetSplitByType(part, measure, metric, subset);
            var resultTypes = measure.EntityCombination.Select(t => t.Identifier).Where(t => !string.Equals(t, splitByType, StringComparison.CurrentCultureIgnoreCase)).ToList();

            return (splitByType, resultTypes);
        }

        private void UpdateReportPartWithNewVariable(int partId, VariableConfiguration variableConfig, MetricConfiguration metricConfig, GroupedVariableDefinition variableDefinition, string oldMetricField)
        {
            var part = _partsRepository.GetById(partId);

            var entityCombination = _variableFactory.ParseResultEntityTypeNames(variableConfig);
            var splitByAndFilterBy = _variableManager.GetSplitByAndFilterBy(entityCombination, variableDefinition.ToEntityTypeName);

            part.Spec1 = metricConfig.Name;
            part.DefaultSplitBy = splitByAndFilterBy.SplitByEntityType ?? "";
            part.MultipleEntitySplitByAndFilterBy = splitByAndFilterBy;
            part.DefaultAverageId = oldMetricField;

            AddCreatedInstanceIdToPart(part, variableDefinition);
            _partsRepository.UpdatePart(part);
        }

        private MetricConfiguration CreateNewMetricForNetting(string uniqueName, VariableConfiguration variable, MetricConfiguration originalMetric)
        {
            var groupedDefinition = (GroupedVariableDefinition)variable.Definition;

            var newMetric = new MetricConfiguration
            {
                Name = uniqueName,
                HelpText = originalMetric.HelpText,
                VariableConfigurationId = variable.Id,
                DisableMeasure = false,
                EligibleForCrosstabOrAllVue = true,
                CalcType = CalculationTypeParser.AsString(CalculationType.YesNo),
                BaseExpression = null,
                NumFormat = MetricNumberFormatter.PercentageInputNumberFormat,
                DisableFilter = false,
                Subset = null,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                DisplayName = originalMetric.DisplayName,
                VarCode = originalMetric.VarCode,
                OriginalMetricName = originalMetric.Name,
            };

            _variableManager.UpdateVariableGroupValuesForMetric(newMetric, groupedDefinition);
            _metricConfigurationRepository.Create(newMetric, false);

            return newMetric;
        }

        private static void AddCreatedInstanceIdToPart(PartDescriptor part, GroupedVariableDefinition definition)
        {
            if (part.SelectedEntityInstances != null)
            {
                int entityInstanceIdToAdd = definition.Groups.Select(g => g.ToEntityInstanceId).Max();
                var currentInstances = part.SelectedEntityInstances.SelectedInstances.ToList()
                    .Append(entityInstanceIdToAdd);
                part.SelectedEntityInstances.SelectedInstances = currentInstances.ToArray();
            }
            else
            {
                part.SelectedEntityInstances = new SelectedEntityInstances
                {
                    SelectedInstances = definition.Groups.Select(g => g.ToEntityInstanceId).ToArray()
                };
            }
        }

        private static void RemoveSelectedInstanceIdFromPart(PartDescriptor part, int instanceToRemove)
        {
            if (part.SelectedEntityInstances != null)
            {
                var currentInstances = part.SelectedEntityInstances.SelectedInstances.ToList();
                if (currentInstances.Contains(instanceToRemove))
                {
                    currentInstances.Remove(instanceToRemove);
                }
                part.SelectedEntityInstances.SelectedInstances = currentInstances.ToArray();
            }
        }

        private void RevertPartToOriginalMetric(VariableConfiguration existingVariable, MetricConfiguration metric, PartDescriptor part,
            EntityType originalType, MetricConfiguration originalMetric, Measure originalMeasure, Subset subset)
        {
            _metricConfigurationRepository.Delete(metric.Id);
            _variableConfigurationRepository.Delete(existingVariable);

            var isTablePart = part.PartType == PartType.ReportsTable;
            string defaultSplitByEntityType = originalType.Identifier;

            part.Spec1 = originalMetric.Name;
            part.DefaultSplitBy = defaultSplitByEntityType;

            if (part.BaseExpressionOverride != null)
            {
                part.BaseExpressionOverride.BaseMeasureName = originalMeasure.Name;
                if (part.BaseExpressionOverride.BaseVariableId == existingVariable.Id)
                {
                    part.BaseExpressionOverride.BaseVariableId = null;
                }
            }

            part.MultipleEntitySplitByAndFilterBy = new MultipleEntitySplitByAndFilterBy
            {
                SplitByEntityType = defaultSplitByEntityType,
                FilterByEntityTypes = originalMeasure.EntityCombination.Where(t => t.Identifier != defaultSplitByEntityType).Select(type =>
                {
                    var instances = _entityRepository.GetInstancesOf(type.Identifier, subset);
                    return new EntityTypeAndInstance
                    {
                        Type = type.Identifier,
                        Instance = isTablePart ? null : instances.FirstOrDefault()?.Id
                    };
                }).ToArray()
            };
        }
    }
}
