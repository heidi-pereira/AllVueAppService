using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.QuotaCells;
using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public record CuratedResultsModel : ISubsetIdProvider, IEntityRequestModel
    {
        public CuratedResultsModel(DemographicFilter demographicFilter,
            int[] entityInstanceIds,
            string subsetId,
            string[] measureName,
            Period period,
            int activeBrandId,
            CompositeFilterModel filterModel,
            SigDiffOptions sigDiffOptions,
            string[] ordering = null,
            DataSortOrder orderingDirection = DataSortOrder.Ascending,
            MeasureFilterRequestModel[] additionalMeasureFilters = null,
            BaseExpressionDefinition baseExpressionOverride = null)
        {
            DemographicFilter = demographicFilter;
            EntityInstanceIds = entityInstanceIds;
            SubsetId = subsetId;
            MeasureName = measureName;
            Period = period;
            ActiveBrandId = activeBrandId;
            FilterModel = filterModel;
            AdditionalMeasureFilters = additionalMeasureFilters;
            Ordering = ordering;
            OrderingDirection = orderingDirection;
            BaseExpressionOverride = baseExpressionOverride;
            IncludeSignificance = sigDiffOptions.HighlightSignificance;
            SigConfidenceLevel = sigDiffOptions.SigConfidenceLevel;
            SigDiffOptions = sigDiffOptions;
        }

        [Required]
        public int[] EntityInstanceIds { get; }
        [Required]
        public string[] MeasureName { get; }
        [Required]
        public string SubsetId { get; }
        [Required]
        public Period Period { get; init; }

        [Required]
        public DemographicFilter DemographicFilter { get; }
        public int ActiveBrandId { get; }

        public CompositeFilterModel FilterModel { get; }
        public MeasureFilterRequestModel[] AdditionalMeasureFilters { get; }
        public string[] Ordering { get; }
        public DataSortOrder OrderingDirection { get; }
        [CanBeNull]
        public BaseExpressionDefinition BaseExpressionOverride { get; }
        public bool IncludeSignificance { get; }
        public SigConfidenceLevel SigConfidenceLevel { get; }
        public SigDiffOptions SigDiffOptions { get;  }
        public int[] GetEntityInstanceIds() => EntityInstanceIds;
    }
}