using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.SourceData.QuotaCells;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public class CrosstabRequestModel : ISubsetIdProvider
    {
        public CrosstabRequestModel(
            string primaryMeasureName,
            string subsetId,
            EntityInstanceRequest primaryInstances,
            EntityInstanceRequest[] filterInstances,
            Period period,
            CrossMeasure[] crossMeasures,
            int activeBrandId,
            DemographicFilter demographicFilter,
            CompositeFilterModel filterModel,
            CrosstabRequestOptions options,
            int? pageNo = null,
            int? noOfCharts = null,
            BaseExpressionDefinition baseExpressionOverride = null)
        {
            PrimaryMeasureName = primaryMeasureName;
            SubsetId = subsetId;
            PrimaryInstances = primaryInstances ?? EntityInstanceRequest.DefaultPrimaryEntityInstanceRequest;
            FilterInstances = filterInstances;
            Period = period;
            CrossMeasures = crossMeasures;
            ActiveBrandId = activeBrandId;
            Options = options;
            PageNo = pageNo;
            NoOfCharts = noOfCharts;
            DemographicFilter = demographicFilter;
            FilterModel = filterModel;
            BaseExpressionOverride = baseExpressionOverride;
        }
        
        [Required]
        public string PrimaryMeasureName { get; }
        [Required]
        public string SubsetId { get; }
        [CanBeNull]
        public EntityInstanceRequest PrimaryInstances { get; }
        [Required]
        public EntityInstanceRequest[] FilterInstances { get; }
        [Required]
        public Period Period { get; }
        [Required]
        public CrossMeasure[] CrossMeasures { get; }
        [Required]
        public int ActiveBrandId { get; }
        [Required]
        public DemographicFilter DemographicFilter { get; }
        [Required]
        public CompositeFilterModel FilterModel { get; }
        [CanBeNull]
        public int? PageNo { get; }
        [CanBeNull]
        public int? NoOfCharts { get; }
        [CanBeNull]
        public CrosstabRequestOptions Options { get; }
        [CanBeNull]
        public BaseExpressionDefinition BaseExpressionOverride { get; }
    }
}
