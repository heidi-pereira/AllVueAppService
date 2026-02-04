using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using NJsonSchema.Annotations;

namespace BrandVue.EntityFramework.MetaData.Reports
{
    public class ReportTemplatePart
    {
        public ReportTemplatePart(
            string metricName,
            string metricVarcode,
            string position,
            string defaultSplitBy,
            string helpText,
            string[] ordering,
            DataSortOrder orderingDirection,
            string[] colours,
            string[] filters,
            CrossMeasure[] breaks,
            bool overrideReportBreaks,
            int? showTop,
            MultipleEntitySplitByAndFilterBy multipleEntitySplitByAndFilterBy,
            ReportOrder? reportOrder,
            BaseExpressionDefinition baseExpressionOverride,
            ReportWaveConfiguration waves,
            SelectedEntityInstances selectedEntityInstances,
            AverageType[] averageTypes,
            int? multiBreakSelectedEntityInstance,
            bool displayMeanValues,
            bool displayStandardDeviation,
            CustomConfigurationOptions customConfigurationOptions,
            string partType,
            bool? showOvertimeData)
        {
            MetricName = metricName;
            MetricVarcode = metricVarcode;
            Position = position;
            DefaultSplitBy = defaultSplitBy;
            HelpText = helpText;
            Ordering = ordering;
            OrderingDirection = orderingDirection;
            Colours = colours;
            Filters = filters;
            Breaks = breaks;
            OverrideReportBreaks = overrideReportBreaks;
            ShowTop = showTop;
            MultipleEntitySplitByAndFilterBy = multipleEntitySplitByAndFilterBy;
            ReportOrder = reportOrder;
            BaseExpressionOverride = baseExpressionOverride;
            Waves = waves;
            SelectedEntityInstances = selectedEntityInstances;
            AverageTypes = averageTypes;
            MultiBreakSelectedEntityInstance = multiBreakSelectedEntityInstance;
            DisplayMeanValues = displayMeanValues;
            DisplayStandardDeviation = displayStandardDeviation;
            CustomConfigurationOptions = customConfigurationOptions;
            ShowOvertimeData = showOvertimeData;
            PartType = partType;
        }

        //Maps to spec1
        public string MetricName { get; set; }
        public string MetricVarcode { get; set; }
        // Maps to spec2
        public string Position { get; set; }
        public string DefaultSplitBy { get; set; }
        public string HelpText { get; set; }
        public string[] Ordering { get; set; }
        public DataSortOrder OrderingDirection { get; set; }
        public string[] Colours { get; set; }
        public string[] Filters { get; set; }
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
        public string PartType { get; set; }
    }
}