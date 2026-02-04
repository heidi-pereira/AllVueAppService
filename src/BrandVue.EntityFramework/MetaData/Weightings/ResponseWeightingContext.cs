using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    public class ResponseWeightingContext
    {
        public const string ResponseWeightingContextIdShadowPropertyName = "ResponseWeightingContextId";

        public ResponseWeightingContext()
        {
            ResponseWeights = new List<ResponseWeightConfiguration>();
        }

        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [Required, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [Required, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Context { get; set; }
        [Required, MaxLength(50)]
        public string SubsetId { get; set; }
        public int? WeightingTargetId { get; set; }
        public ICollection<ResponseWeightConfiguration> ResponseWeights { get; set; }
    }

    public class ResponseWeightingContextConfiguration : IEntityTypeConfiguration<ResponseWeightingContext>
    {
        void IEntityTypeConfiguration<ResponseWeightingContext>.Configure(EntityTypeBuilder<ResponseWeightingContext> builder)
        {
            builder.HasIndex(rwc => new { rwc.ProductShortCode, rwc.SubProductId, rwc.SubsetId, rwc.WeightingTargetId }).IsUnique();
        }
    }
}
