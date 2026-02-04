using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.QuotaCells;
using Microsoft.AspNetCore.Http;
using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    /// <remarks>
    /// The intent is to refactor in the direction of a single or small number of request models:
    /// The idea is to remove the difference between filtering/splitting by entities vs arbitrary other filters
    ///
    /// public class TheRequestModel : ISubsetIdProvider
    /// {
    ///     public string MeasureName { get; }
    ///     public string SubsetId { get; }
    ///     public Period Period { get; }
    ///     public ResponseProperty SplitBy { get; } //i.e. SQL GroupBy, a separate result is returned for each distinct value for responseproperty
    ///     public MeasureFilter FilterBy { get; } // i.e. SQL Where, only matching responses considered
    /// }
    ///
    /// public class MeasureFilter
    /// {
    ///     public ResponseProperty Property { get; }
    ///     public ValuePredicate Predicate { get; } // e.g. x == 0 || x &gt; 3
    /// }
    /// </remarks>
    public record MultiEntityRequestModel : ISubsetIdProvider, IEntityRequestModel
    {
        public MultiEntityRequestModel(string measureName,
            string subsetId,
            Period period,
            EntityInstanceRequest dataRequest,
            EntityInstanceRequest[] filterBy,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            MeasureFilterRequestModel[] additionalMeasureFilters,
            BaseExpressionDefinition[] baseExpressionOverrides,
            bool includeSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            int? focusEntityInstanceId = null)
        {
            MeasureName = measureName;
            SubsetId = subsetId;
            Period = period;
            DataRequest = dataRequest ?? EntityInstanceRequest.DefaultPrimaryEntityInstanceRequest;
            FilterBy = filterBy;
            DemographicFilter = demographicFilter;
            FilterModel = filterModel;
            AdditionalMeasureFilters = additionalMeasureFilters;
            BaseExpressionOverrides = baseExpressionOverrides;
            IncludeSignificance = includeSignificance;
            SigConfidenceLevel = sigConfidenceLevel;
            FocusEntityInstanceId = focusEntityInstanceId;
        }

        public static MultiEntityRequestModel TemporaryConstructor(CuratedResultsModel model, string entityType)
        {
            return new MultiEntityRequestModel(
            model.MeasureName[0],
            model.SubsetId,
            model.Period,
            new EntityInstanceRequest(entityType, model.EntityInstanceIds),
            new EntityInstanceRequest[]{},
            model.DemographicFilter,
            model.FilterModel,
            model.AdditionalMeasureFilters,
            new [] {model.BaseExpressionOverride},
            model.IncludeSignificance,
            model.SigConfidenceLevel,
            model.ActiveBrandId);

        }
        [Required,NotNull]
        public string MeasureName { get; }
        public string SubsetId { get; }
        public Period Period { get; set; }
        [CanBeNull]
        public EntityInstanceRequest DataRequest { get; }
        public EntityInstanceRequest[] FilterBy { get; }
        public DemographicFilter DemographicFilter { get; }
        public CompositeFilterModel FilterModel { get; }
        public MeasureFilterRequestModel[] AdditionalMeasureFilters { get; }
        public BaseExpressionDefinition[] BaseExpressionOverrides { get; }
        public bool IncludeSignificance { get; }
        public SigConfidenceLevel SigConfidenceLevel { get; }

        [CanBeNull]
        public int? FocusEntityInstanceId { get; set; }

        public int[] GetEntityInstanceIds()
        {
            return DataRequest.EntityInstanceIds;
        }
    }


    public record StackedMultiEntityRequestModel(string MeasureName,
        string SubsetId,
        Period Period,
        EntityInstanceRequest SplitBy,
        EntityInstanceRequest FilterBy,
        DemographicFilter DemographicFilter,
        CompositeFilterModel FilterModel,
        MeasureFilterRequestModel[] AdditionalMeasureFilters, 
        [property: CanBeNull] BaseExpressionDefinition BaseExpressionOverride = null) : ISubsetIdProvider;

    public class EntityInstanceRequest
    {
        public string Type { get; }
        public int[] EntityInstanceIds { get; }

        public EntityInstanceRequest(string type, int[] entityInstanceIds)
        {
            Type = type;
            EntityInstanceIds = entityInstanceIds;
        }

        public static EntityInstanceRequest DefaultPrimaryEntityInstanceRequest => new(ViewTypeEnum.Profile.ToString().ToLower(), new int[] { 0 });
    }
}
