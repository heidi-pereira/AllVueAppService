using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    [Table("WeightingSchemes")]
    public class WeightingScheme
    {
        public const int NoneId = -1;

        public int? Id { get; set; }
        
        /// <summary>
        /// This property is useful for the unique index used - see below.
        /// It doesn't need to be a part of the DTO for the front end.
        /// </summary>
        [JsonIgnore]
        public int WeightingStrategyId { get; set; }

        /// <summary>
        /// Defines which respondents belong to this scheme.
        /// Value corresponds to the filter metric defined for the weighting strategy.
        /// The value can only be null if there's no filter metric AND there's only one weighting scheme.
        /// In this case all respondents belong to this weighting scheme.
        /// </summary>
        public int? FilterMetricEntityId { get; set; }

        public WeightingSchemeDetails WeightingSchemeDetails { get; set; }
    }
    
    public class WeightingSchemeConfiguration : IEntityTypeConfiguration<WeightingScheme>
    {
        public void Configure(EntityTypeBuilder<WeightingScheme> builder)
        {
            builder.HasKey(ws => ws.Id);
            builder.Property(ws => ws.WeightingSchemeDetails).HasJsonConversion();
            // This index ensures that each respondent belongs to a single Weighting Scheme (assuming that the filter metric is 'single choice'). 
            builder.HasIndex(ws => new {ws.WeightingStrategyId, ws.FilterMetricEntityId}).HasFilter(null).IsUnique();
        }
    }
}