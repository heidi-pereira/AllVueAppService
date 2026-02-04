using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    [Table("WeightingPlans")]
    public class WeightingPlanConfiguration
    {
        [Required, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string VariableIdentifier { get; set; } // Never null - potentially multiple plans can come back with no parent

        public int Id { get; set; }
        public int? ParentWeightingTargetId { get; set; }
        /// <summary>
        /// Everything within a weighting group is weighted together (i.e. the sample for the whole group affects the scale factors)
        /// The unweighted and weighted sample counts for the set of respondents will be the same.
        /// The UI should prevent IsWeightingGroupRoot being set to true at multiple points in the tree, the one closest to the root will be considered authoritative.
        /// </summary>
        public bool IsWeightingGroupRoot { get; set; }
        [JsonIgnore]
        public WeightingTargetConfiguration ParentTarget { get; set; }
        public List<WeightingTargetConfiguration> ChildTargets { get; set; }

        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [Required, MaxLength(50)]
        public string SubsetId { get; set; }
    }
}