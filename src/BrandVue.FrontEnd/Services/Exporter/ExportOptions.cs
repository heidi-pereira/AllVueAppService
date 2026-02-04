using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;

namespace BrandVue.Services.Exporter
{
    public class ExportOptions
    {
        public bool SinglePage { get; set; }
        public bool MultiPage => !SinglePage;
        public SigDiffOptions SigDiffOptions { get; set; } 
        public ReportOrder ResultSortingOrder { get; set; }
        public bool IncludeCounts { get; }
        public bool CalculateIndexScores { get; }
        public bool HighlightLowSample{ get; }
        public int DecimalPlaces { get; }
        public bool HideEmptyRows { get; }
        public bool HideEmptyColumns { get; }
        public bool HideTotalColumn { get; }
        public bool ShowMultipleTablesAsSingle { get; }
        public int? ShowTop { get; set; }
        public bool IsDataWeighted { get; set; }
        public bool DisplayMeanValues { get; set; }
        public bool DisplayStandardDeviation { get; set; }
        public int? LowSampleThreshold { get; set; }

        public ExportOptions(
            bool singlePageExport,
            SigDiffOptions sigDiffOptions,
            ReportOrder resultSortingOrder,
            bool includeCounts,
            bool calculateIndexScores,
            bool highlightLowSample,
            int decimalPlaces,
            bool hideEmptyRows,
            bool hideEmptyColumns,
            bool hideTotalColumn,
            bool showMultipleTablesAsSingle,
            bool isDataWeighted,
            int? lowSampleThreshold,
            int? showTop = null,
            bool displayMeanValues = false,
            bool displayStandardDeviation = false)
        {
            SinglePage = singlePageExport;
            SigDiffOptions = sigDiffOptions;
            ResultSortingOrder = resultSortingOrder;
            IncludeCounts = includeCounts;
            CalculateIndexScores = calculateIndexScores;
            HighlightLowSample = highlightLowSample;
            DecimalPlaces = decimalPlaces;
            HideEmptyRows = hideEmptyRows;
            HideEmptyColumns = hideEmptyColumns;
            HideTotalColumn = hideTotalColumn;
            ShowMultipleTablesAsSingle = showMultipleTablesAsSingle;
            ShowTop = showTop;
            IsDataWeighted = isDataWeighted;
            DisplayMeanValues = displayMeanValues;
            DisplayStandardDeviation = displayStandardDeviation;
            LowSampleThreshold = lowSampleThreshold;
        }

        public ExportOptions(ExportOptions options)
        {
            SinglePage = options.SinglePage;
            SigDiffOptions = options.SigDiffOptions;
            ResultSortingOrder = options.ResultSortingOrder;
            IncludeCounts = options.IncludeCounts;
            CalculateIndexScores = options.CalculateIndexScores;
            HighlightLowSample = options.HighlightLowSample;
            DecimalPlaces = options.DecimalPlaces;
            HideEmptyRows = options.HideEmptyRows;
            HideEmptyColumns = options.HideEmptyColumns;
            HideTotalColumn = options.HideTotalColumn;
            ShowMultipleTablesAsSingle = options.ShowMultipleTablesAsSingle;
            ShowTop = options.ShowTop;
            IsDataWeighted = options.IsDataWeighted;
            DisplayMeanValues = options.DisplayMeanValues;
            DisplayStandardDeviation = options.DisplayStandardDeviation;
            LowSampleThreshold = options.LowSampleThreshold;
        }
    }
}
