using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class QuestionVariableDefinition : VariableDefinition
    {
        [Required]
        public string QuestionVarCode { get; set; }
        [Required]
        public IReadOnlyCollection<(string DbLocationUnquotedColumnName, string EntityTypeName)> EntityTypeNames { get; set; }
        public SqlRoundingType RoundingType { get; set; } = SqlRoundingType.Round;
        /// <summary>
        /// Using this will always yield less accurate results than auto-scaling.
        /// It's only here for maintaining historical data from map file brandvues until we remove the scale factor entirely.
        /// </summary>
        public double? ForceScaleFactor { get; set; }
    }
}