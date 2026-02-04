using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Reports
{
    public class ReportTemplate
    {
        public int Id { get; set; }
        public string TemplateDisplayName { get; set; }
        public string TemplateDescription { get; set; }
        public string UserId { get; set; }
        public VariableConfiguration BaseVariable { get; set; }
        public SavedReportTemplate SavedReportTemplate { get; set; }
        public List<ReportTemplatePart> ReportTemplateParts { get; set; }
        public IEnumerable<VariableConfiguration> UserDefinedVariableDefinitions { get; set; }
        public AverageConfiguration AverageConfiguration { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SavedReportTemplate : AllVueReport
    {
        public SavedReportTemplate(
            bool isShared,
            ReportOrder order,
            int decimalPlaces,
            ReportType reportType,
            ReportWaveConfiguration waves,
            List<CrossMeasure> breaks,
            bool singlePageExport,
            bool highlightSignificance,
            CrosstabSignificanceType significanceType,
            DisplaySignificanceDifferences displaySignificanceDifferences,
            SigConfidenceLevel sigConfidenceLevel,
            bool includeCounts,
            bool calculateIndexScores,
            bool highlightLowSample,
            bool isDataWeighted,
            bool hideEmptyRows,
            bool hideEmptyColumns,
            bool hideTotalColumn,
            bool hideDataLabels,
            bool showMultipleTablesAsSingle,
            BaseDefinitionType? baseTypeOverride,
            List<DefaultReportFilter> defaultFilters,
            ReportOverTimeConfiguration overTimeConfig,
            string subsetId,
            int? lowSampleThreshold)
        {
            IsShared = isShared;
            Order = order;
            DecimalPlaces = decimalPlaces;
            ReportType = reportType;
            Waves = waves;
            Breaks = breaks;
            SinglePageExport = singlePageExport;
            HighlightSignificance = highlightSignificance;
            SignificanceType = significanceType;
            DisplaySignificanceDifferences = displaySignificanceDifferences;
            SigConfidenceLevel = sigConfidenceLevel;
            IncludeCounts = includeCounts;
            CalculateIndexScores = calculateIndexScores;
            HighlightLowSample = highlightLowSample;
            IsDataWeighted = isDataWeighted;
            HideEmptyRows = hideEmptyRows;
            HideEmptyColumns = hideEmptyColumns;
            HideTotalColumn = hideTotalColumn;
            HideDataLabels = hideDataLabels;
            ShowMultipleTablesAsSingle = showMultipleTablesAsSingle;
            BaseTypeOverride = baseTypeOverride;
            DefaultFilters = defaultFilters;
            OverTimeConfig = overTimeConfig;
            SubsetId = subsetId;
            LowSampleThreshold = lowSampleThreshold;
        }
    }

    public class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
    {
        public void Configure(EntityTypeBuilder<ReportTemplate> builder)
        {
            builder.ToTable("ReportTemplates", "Reports");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.TemplateDisplayName)
                .IsRequired()
                .HasMaxLength(SqlTypeConstants.DefaultVarcharLength);

            builder.Property(e => e.TemplateDescription)
                .HasMaxLength(SqlTypeConstants.DefaultVarcharLength);

            builder.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(SqlTypeConstants.DefaultVarcharLength);

            builder.Property(e => e.BaseVariable)
                .HasJsonConversion();

            builder.Property(e => e.SavedReportTemplate)
                .IsRequired()
                .HasJsonConversion();

            builder.Property(e => e.ReportTemplateParts)
                .HasJsonConversion();

            builder.Property(e => e.UserDefinedVariableDefinitions)
                .HasJsonConversion();

            builder.Property(e => e.AverageConfiguration)
                .HasJsonConversion();

            builder.Property(e => e.AverageConfiguration)
                .HasJsonConversion();

            builder.Property(e => e.CreatedAt)
                .IsRequired();
        }
    }
}