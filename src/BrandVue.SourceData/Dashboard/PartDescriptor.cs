using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.CommonMetadata;
using NJsonSchema.Annotations;

namespace BrandVue.SourceData.Dashboard
{
    /// <remarks>
    /// Warning: "Descriptor" is used by all the PublicApi types, but this is not one of those as you can see from the namespace.
    /// </remarks>
    public class PartDescriptor : BaseMetadataEntity
    {
        public PartDescriptor()
        {

        }

        public PartDescriptor(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
        public string FakeId { get; set; }
        public string PaneId { get; set; }
        public string PartType { get; set; }
        public string Spec1 { get; set; }
        public string Spec2 { get; set; }
        public string Spec3 { get; set; }
        public string DefaultSplitBy { get; set; }
        public string HelpText { get; set; }
        public string DefaultAverageId { get; set; }
        public string[] AutoMetrics { get; set; }
        public string[] AutoPanes { get; set; }
        public string[] Ordering { get; set; }
        public DataSortOrder OrderingDirection { get; set; }
        public string[] Colours { get; set; }
        public string[] Filters { get; set; }
        public AxisRange XAxisRange { get; set; }
        public AxisRange YAxisRange { get; set; }
        public string[][] Sections { get; set; }
        public CrossMeasure[] Breaks { get; set; }
        public bool OverrideReportBreaks { get; set; }
        public int? ShowTop { get; set; }
        public MultipleEntitySplitByAndFilterBy MultipleEntitySplitByAndFilterBy { get; set; }
        public ReportOrder? ReportOrder { get; set; }
        [CanBeNull]
        public BaseExpressionDefinition BaseExpressionOverride { get; set; }
        [CanBeNull]
        public ReportWaveConfiguration Waves { get; set; }
        [CanBeNull]
        public SelectedEntityInstances SelectedEntityInstances { get; set; }
        public AverageType[] AverageTypes { get; set; }
        [CanBeNull]
        public int? MultiBreakSelectedEntityInstance { get; set; }
        public bool DisplayMeanValues { get; set; }
        public bool DisplayStandardDeviation { get; set; }
        [CanBeNull]
        public CustomConfigurationOptions CustomConfigurationOptions { get; set; }
        public bool? ShowOvertimeData { get; set; }
        public bool? HideDataLabels { get; set; }
    }

    public struct AxisRange
    {
        public double? Min;
        public double? Max;

        public AxisRange(int? min, int? max)
        {
            Min = min;
            Max = max;
        }

        public AxisRange(double[] range)
        {
            Min = Max = null;

            if (range != null)
            {
                if (range.Length >= 1)
                {
                    Min = range[0];
                }

                if (range.Length >= 2)
                {
                    Max = range[1];
                }
            }
        }
    }

    public static class PartType
    {
        //taken from PartType.ts on frontend
        public const string MetricChangeOnPeriod = "MetricChangeOnPeriod";
        public const string PageLink = "PageLink";
        public const string OpenAssociationsCard = "OpenAssociationsCard";
        public const string ScatterPlotCard = "ScatterPlotCard";
        public const string FunnelCard = "FunnelCard";
        public const string SimplifiedScorecard = "SimplifiedScorecard";
        public const string RankingScorecard = "RankingScorecard";
        public const string RankingOvertimeCard = "RankingOvertimeCard";
        public const string CategoryComparison = "CategoryComparison";
        public const string MultiEntityCompetition = "MultiEntityCompetition";
        //Reports cards
        public const string ReportsCardText = "ReportsCardText";
        public const string ReportsCardChart = "ReportsCardChart";
        public const string ReportsCardStackedMulti = "ReportsCardStackedMulti";
        public const string ReportsCardMultiEntityMultipleChoice = "ReportsCardMultipleChoice";
        public const string ReportsTable = "ReportsTable";
        public const string ReportsCardLine = "ReportsCardLine";
        public const string ReportsCardDoughnut = "ReportsCardDoughnut";
        public const string ReportsCardHeatmapImage = "ReportsCardHeatmapImage";
        public const string ReportsCardFunnel = "ReportsCardFunnel";
    }
}
