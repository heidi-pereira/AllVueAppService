using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using JsonKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Page
{

    [Newtonsoft.Json.JsonConverter(typeof(JsonKnownTypesConverter<CustomConfigurationOptions>)), JsonDiscriminator(Name = "discriminator")]
    [KnownType(typeof(HeatMapReportOptions))]

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
    [JsonDerivedType(typeof(HeatMapReportOptions), "HeatMapReportOptions")]

    public abstract class CustomConfigurationOptions
    {
    }

    public enum HeatMapKeyPosition
    {
        None,
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
    }

    public class HeatMapReportOptions : CustomConfigurationOptions
    {
        public int RadiusInPixels { get; set; }
        public int Intensity { get; set; }
        public float OverlayTransparency { get; set; }
        public HeatMapKeyPosition KeyPosition { get; set; }
        public bool DisplayKey { get; set; }
        public bool DisplayClickCounts { get; set; }
    }

    public class MultipleEntitySplitByAndFilterBy
    {
        public string SplitByEntityType { get; set; }
        public EntityTypeAndInstance[] FilterByEntityTypes { get; set; }
    }

    public class SelectedEntityInstances
    {
        public int[] SelectedInstances { get; set; }
    }

    public class EntityTypeAndInstance
    {
        public string Type { get; set; }
        //Null means all instances of type
        public int? Instance { get; set; }
    }
    
    [Table("Parts")]
    public class DbPart
    {
        public int Id { get; set; }
        public string ProductShortCode { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        public string PaneId { get; set; }
        public string PartType { get; set; }
        public string Spec1 { get; set; }
        public string Spec2 { get; set; }
        public string Spec3 { get; set; }
        public string DefaultSplitBy { get; set; }
        public string HelpText { get; set; }
        public string DefaultAverageId { get; set; }
        public string AutoMetrics { get; set; }
        public string AutoPanes { get; set; }
        public string Ordering { get; set; }
        public string OrderingDirection { get; set; }
        public string Colours { get; set; }
        public string Filters { get; set; }
        public string XRange { get; set; }
        public string YRange { get; set; }
        public string Sections { get; set; }
        public CrossMeasure[] Breaks { get; set; }
        public bool OverrideReportBreaks { get; set; }
        public int? ShowTop { get; set; }
        public MultipleEntitySplitByAndFilterBy MultipleEntitySplitByAndMain { get; set; }
        public ReportOrder? ReportOrder { get; set; }
        public BaseExpressionDefinition BaseExpressionOverride { get; set; }
        public ReportWaveConfiguration Waves { get; set; }
        public SelectedEntityInstances SelectedEntityInstances { get; set; }
        public AverageType[] AverageType { get; set; }
        public int? MultiBreakSelectedEntityInstance { get; set; }
        public bool DisplayMeanValues { get; set; }
        public bool DisplayStandardDeviation { get; set; }
        public CustomConfigurationOptions CustomConfigurationOptions { get; set; }
        public bool? ShowOvertimeData { get; set; }
        public bool? HideDataLabels { get; set; }
    }

    public class DbPartConfiguration : IEntityTypeConfiguration<DbPart>
    {
        public void Configure(EntityTypeBuilder<DbPart> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(b => b.Breaks).HasJsonConversion();
            builder.Property(b => b.MultipleEntitySplitByAndMain).HasJsonConversion();
            builder.Property(b => b.BaseExpressionOverride).HasJsonConversion();
            builder.Property(b => b.Waves).HasJsonConversion();
            builder.Property(b => b.SelectedEntityInstances).HasJsonConversion();
            builder.Property(b => b.AverageType).HasJsonConversion(SqlTypeConstants.DefaultVarcharLength);
            builder.Property(b => b.CustomConfigurationOptions).HasJsonConversion();
        }
    }
}
