using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.PublicApi.Definitions;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using System.Globalization;
using System.Threading;

namespace BrandVue.PublicApi.Services
{
    public class ApiAnswerService : IApiAnswerService
    {
        private readonly IResponseFieldDescriptorLoader _responseFieldDescriptorLoader;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IEntityRepository _entityRepository;
        private readonly ILazyDataLoader _lazyDataLoader;
        private readonly IDataPresenceGuarantor _dataPresenceGuarantor;
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IVariableFactory _variableFactory;

        public ApiAnswerService(IResponseFieldDescriptorLoader responseFieldDescriptorLoader, IRespondentRepositorySource respondentRepositorySource, 
            IEntityRepository entityRepository, ILazyDataLoader lazyDataLoader, IDataPresenceGuarantor dataPresenceGuarantor, IVariableFactory variableFactory, 
            IReadableVariableConfigurationRepository variableConfigurationRepository)
        {
            _responseFieldDescriptorLoader = responseFieldDescriptorLoader ?? throw new ArgumentNullException(nameof(responseFieldDescriptorLoader));
            _respondentRepositorySource = respondentRepositorySource ?? throw new ArgumentNullException(nameof(respondentRepositorySource));
            _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
            _lazyDataLoader = lazyDataLoader ?? throw new ArgumentNullException(nameof(lazyDataLoader));
            _dataPresenceGuarantor = dataPresenceGuarantor ?? throw new ArgumentNullException(nameof(dataPresenceGuarantor));
            _variableConfigurationRepository = variableConfigurationRepository ?? throw new ArgumentNullException(nameof(variableConfigurationRepository));
            _variableFactory = variableFactory ?? throw new ArgumentNullException(nameof(variableFactory));
        }


        public async Task<ResponseDataWithHeaders> GetVariableResponseData(SurveysetDescriptor surveyset,
            VariableDescriptor variableDescriptor,
            DateTimeOffset singleDay, CancellationToken cancellationToken)
        {
            var variableConfiguration = _variableConfigurationRepository.GetByIdentifier(variableDescriptor.VariableIdentifier);
            var variable = _variableFactory.GetDeclaredVariable(variableConfiguration);
            var headers = new List<string>() { PublicApiConstants.EntityResponseFieldNames.ProfileId, "Value" }
                .Union(variable.UserEntityCombination.Select(x => x.Identifier + "_Id"));
            var respondentRepository = _respondentRepositorySource.GetForSubset(surveyset.Subset);
            var responseToValueFunctions = CreateResponseToValueFunction(surveyset, variable, respondentRepository);
            var targetInstances = variable.UserEntityCombination.Select(ec => 
                new DataTarget(ec, _entityRepository.GetInstancesOf(ec.Identifier, surveyset.Subset).Select(inst => inst.Id)));
            await _dataPresenceGuarantor.EnsureDataIsLoaded(respondentRepository, surveyset.Subset, variable, targetInstances.ToArray(), singleDay, singleDay.AddDays(1), cancellationToken); 
            return new(respondentRepository.GetRespondentsForDay(singleDay.Ticks)
                .SelectMany(cellResponse => responseToValueFunctions,
                    (cellResponse, tupl) =>
                    {
                        var responseEntity = tupl.Item2(cellResponse.ProfileResponseEntity);
                        if (responseEntity != null)
                        {
                            var result = tupl.Item1.ToDictionary(x=>x.EntityType.Identifier + "_Id", x=>x.Value.ToString());
                            result.Add(PublicApiConstants.EntityResponseFieldNames.ProfileId, cellResponse.ProfileResponseEntity.Id.ToString());
                            result.Add("Value", responseEntity.ToString());
                            return result;
                        }
                        return null;
                }).Where(x=>x != null), headers);
        }


        public async Task<ResponseDataWithHeaders> GetMappedClassResponseData(SurveysetDescriptor surveyset,
            ClassDescriptor classDescriptor,
            DateTimeOffset date, bool includeText, CancellationToken cancellationToken)
        {
            var requestEntityCombination = new[] { classDescriptor.EntityType };
            string classIdHeader = $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(classDescriptor.EntityType.Identifier)}_Id";
            var entityCombinationSpecificColumns = new[]
            {
                PublicApiConstants.EntityResponseFieldNames.ProfileId,
                classIdHeader
            };
            var responseFieldDescriptors = _responseFieldDescriptorLoader.GetFieldDescriptors(surveyset, requestEntityCombination, includeText).ToArray();
            var headers = CreateHeaders(entityCombinationSpecificColumns, responseFieldDescriptors);
            var entityMeasureDataWithQuotaCell = await GetMeasureDataWithQuotaCell(surveyset, date, requestEntityCombination, responseFieldDescriptors, cancellationToken);
            var orderedEntityCombination = requestEntityCombination.OrderBy(x => x).ToArray();
            return new(entityMeasureDataWithQuotaCell.Select(measureDataWithCell =>
            {
                var (entityMeasureData, _) = measureDataWithCell;
                var entityMeasures = EntityMeasureData(entityMeasureData);
                entityMeasures.Add(PublicApiConstants.EntityResponseFieldNames.ProfileId, entityMeasureData.ResponseId.ToString());
                entityMeasures.Add(classIdHeader, entityMeasureData.EntityIds.AsReadOnlyCollection(orderedEntityCombination).Single().Value.ToString());
                return entityMeasures;
            }), headers);
        }


        public async Task<ResponseDataWithHeaders> GetNestedClassResponseData(SurveysetDescriptor surveyset,
            ClassDescriptor parentClass, ClassDescriptor childClass,
            DateTimeOffset date, bool includeText, CancellationToken cancellationToken)
        {
            var requestEntityCombination = new[] { parentClass.EntityType, childClass.EntityType };
            var entityCombinationSpecificColumns = new[]
            {
                PublicApiConstants.EntityResponseFieldNames.ProfileId,
                $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parentClass.EntityType.Identifier)}_Id",
                $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(childClass.EntityType.Identifier)}_Id"
            };
            var responseFieldDescriptors = _responseFieldDescriptorLoader.GetFieldDescriptors(surveyset, requestEntityCombination, includeText).ToArray();
            var headers = CreateHeaders(entityCombinationSpecificColumns, responseFieldDescriptors);
            var entityMeasureDataWithQuotaCell = await GetMeasureDataWithQuotaCell(surveyset, date, requestEntityCombination, responseFieldDescriptors, cancellationToken);
            var orderedEntityCombination = requestEntityCombination.OrderBy(x => x).ToArray();
            var entityHeaders = requestEntityCombination.ToDictionary(t => t, t => $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.Identifier)}_Id");
            return new(entityMeasureDataWithQuotaCell.Select(measureDataWithCell =>
            {
                var (entityMeasureData, _) = measureDataWithCell;
                var entityMeasures = EntityMeasureData(entityMeasureData);
                entityMeasures.Add(PublicApiConstants.EntityResponseFieldNames.ProfileId, entityMeasureData.ResponseId.ToString());
                foreach ((var entityType, int value) in entityMeasureData.EntityIds.AsReadOnlyCollection(orderedEntityCombination))
                {
                    entityMeasures.Add(entityHeaders[entityType], $"{value}");
                }
                return entityMeasures;
            }), headers);
        }


        public async Task<ResponseDataWithHeaders> GetProfileResponseData(SurveysetDescriptor surveyset,
            DateTimeOffset date, string weightingColumnName, CancellationToken cancellationToken)
        {
            var requestEntityCombination = Array.Empty<EntityType>();
            var entityCombinationSpecificColumns = new[]
            {
                PublicApiConstants.EntityResponseFieldNames.ProfileId,
                weightingColumnName,
                PublicApiConstants.EntityResponseFieldNames.StartDate
            };
            var responseFieldDescriptors = _responseFieldDescriptorLoader.GetFieldDescriptors(surveyset, requestEntityCombination, true).ToArray();
            var headers = CreateHeaders(entityCombinationSpecificColumns, responseFieldDescriptors);
            var entityMeasureDataWithQuotaCell =
                await GetMeasureDataWithQuotaCell(surveyset, date, requestEntityCombination, responseFieldDescriptors, cancellationToken);
            return new (entityMeasureDataWithQuotaCell.Select(measureDataWithCell =>
            {
                var (entityMeasureData, quotaCell) = measureDataWithCell;
                var entityMeasures = EntityMeasureData(entityMeasureData);
                entityMeasures.Add(PublicApiConstants.EntityResponseFieldNames.ProfileId,
                    entityMeasureData.ResponseId.ToString());
                entityMeasures.Add(weightingColumnName, quotaCell.Id.ToString());
                entityMeasures.Add(PublicApiConstants.EntityResponseFieldNames.StartDate,
                    date.ToString("yyyy-MM-dd"));
                return entityMeasures;
            }), headers);
        }

        public IEnumerable<string> CreateHeaders(IEnumerable<string> entityCombinationSpecificColumns, IEnumerable<ResponseFieldDescriptor> questionDescriptors)
        {
            return entityCombinationSpecificColumns.Concat(questionDescriptors
                .OrderBy(fd => fd.Name)
                .Select(fd => fd.Name)
                .ToList());
        }

        private async Task<IEnumerable<(EntityMetricData EntityMeasureData, QuotaCell QuotaCell)>>
            GetMeasureDataWithQuotaCell(SurveysetDescriptor surveyset, DateTimeOffset date,
                EntityType[] requestEntityCombination, IEnumerable<ResponseFieldDescriptor> questionDescriptors,
                CancellationToken cancellationToken)
        {
            var requestTargets = requestEntityCombination
                .Select(c => new TargetInstances(c, _entityRepository.GetInstancesOf(c.Identifier, surveyset))).ToArray();
            var endDate = date.EndOfDay();

            var responseDataWithoutQuotaCells = await _lazyDataLoader.GetDataForFields(surveyset, questionDescriptors.ToArray(),
                (date.DateTime, endDate.DateTime), requestTargets, cancellationToken);

            var respondentRepository = _respondentRepositorySource.GetForSubset(surveyset);

            var allDataIsUnweighted = respondentRepository.AllCellsGroup.Cells.All(x=>x.IsUnweightedCell);
            var forceReturnOfAllQuotaData = allDataIsUnweighted;

            var entityMeasureDataWithQuotaCell = responseDataWithoutQuotaCells.Select(r => {
                respondentRepository.TryGet(r.ResponseId, out var cellResponse);
                return (EntityMeasureData: r, cellResponse.QuotaCell);
                })
                .Where(q => q.QuotaCell != null && (!q.QuotaCell.IsUnweightedCell || forceReturnOfAllQuotaData));
            return entityMeasureDataWithQuotaCell;
        }

        private IEnumerable<Tuple<IReadOnlyCollection<EntityValue>,Func<IProfileResponseEntity, TOut>>> CreateResponseToValueFunction<TOut>(Subset subset, 
            IVariable<TOut> variable, IRespondentRepository respondentRepository)
        {
                var userEntityCombination = variable.UserEntityCombination.ToArray();
                var cartesian = variable.UserEntityCombination
                    .Select(ec => _entityRepository.GetInstancesOf(ec.Identifier, subset))
                    .CartesianProduct()
                    .Select(x => new EntityValueCombination(x.Select((ei, i) => new EntityValue(userEntityCombination[i], ei.Id))));

                return cartesian.Select(vc=> Tuple.Create(vc.AsReadOnlyCollection() ,
                    variable.CreateForEntityValues(vc))
                );
        }

        private static Dictionary<string, string> EntityMeasureData(EntityMetricData entityMetricData)
        {
            return entityMetricData.Measures
                .Select(m => (m.Field, Value: m.Value.ToString()))
                .Concat(entityMetricData.TextFields.Select(m => (m.Field, m.Value)))
                .ToDictionary(kvp => kvp.Field.Name, kvp => kvp.Value);
        }
    }
}