using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    [Table("WeightingTargets")]
    public class WeightingTargetConfiguration
    {
        /// <summary>
        /// Defines which respondents belong to this scheme.
        /// Value corresponds to the filter metric defined for the parent weighting plan.
        /// </summary>
        public int EntityInstanceId { get; set; }
        /// <summary>
        /// Multiple layers in the tree can have targets.
        /// Null when no target at this layer.
        /// </summary>
        public decimal? Target { get; set; }
        /// <summary>
        /// Mutually exclusive with Target. Defines a target sample for this target in order to do expansion weighting.
        /// </summary>
        public int? TargetPopulation { get; set; }

        public int Id { get; set; }
        public int ParentWeightingPlanId { get; set; }
        [JsonIgnore]
        public WeightingPlanConfiguration ParentWeightingPlan { get; set; }
        public List<WeightingPlanConfiguration> ChildPlans { get; set; }
        [CanBeNull] public ResponseWeightingContext ResponseWeightingContext { get; set; }

        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [Required, MaxLength(50)]
        public string SubsetId { get; set; }
    }
}