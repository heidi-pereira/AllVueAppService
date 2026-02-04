using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    /// <summary>
    /// Defines how values will be mapped to entities of a new entity type.
    /// </summary>
    public class VariableGrouping
    {
        /// <summary>
        /// Defines the name of the new instance, which the values will be mapped to.
        /// </summary>
        [Required]
        public string ToEntityInstanceName { get; set; }

        /// <summary>
        /// Defines the id of the new instance, which the values will be mapped to.
        /// </summary>
        [Required]
        public int ToEntityInstanceId { get; set; }

        /// <summary>
        /// The condition that a respondent needs to match to be included in this group
        /// </summary>
        [Required]
        [VariableComponentIsValid]
        public VariableComponent Component { get; set; }
    }
}
