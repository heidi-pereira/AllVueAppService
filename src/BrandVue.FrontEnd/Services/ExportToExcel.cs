using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;
using BrandVue.Models.ExcelExport;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BrandVue.Services
{
    public class ExportToExcel
    {
        private readonly string _appTitle;
        private readonly string _averageDescriptorName;
        private readonly int? _lowSampleCount;
        private readonly string _companyCopyright;
        private readonly bool _displayExtendedCopyrightMessage;
        private readonly string _helpText;
        private readonly string _subset;
        private ExcelWorksheet _introSheet;
        private int _introSheetRowId;
        private readonly ExcelPackage _excelPackage;
        private IPagesRepository _pagesRepository;
        private IPanesRepository _panesRepository;
        private IPartsRepository _partsRepository;

        private const string SampleSizeTitle = "Sample size";
        private const string LowSampleTitle = "Low sample";
        private const string SignificanceTitle = "Significance";

        public ExportToExcel(string[] modelFilterDescriptions, string companyCopyright, string appTitle, bool displayExtendedCopyrightMessage, string averageDescriptorName, string helpText, string subset, int? lowSampleCount)
        {
            _appTitle = appTitle;
            _averageDescriptorName = averageDescriptorName;
            _lowSampleCount = lowSampleCount;
            _companyCopyright = companyCopyright;
            _displayExtendedCopyrightMessage = displayExtendedCopyrightMessage;
            _helpText = helpText;
            _subset = subset;
            _excelPackage = new ExcelPackage();
            CreateIntroSheet(modelFilterDescriptions, _excelPackage);
        }

        public ExportToExcel(string companyCopyright, string appTitle, bool displayExtendedCopyrightMessage, string averageDescriptorName, string helpText, string subset, int? lowSampleCount)
        {
            _appTitle = appTitle;
            _averageDescriptorName = averageDescriptorName;
            _lowSampleCount = lowSampleCount;
            _companyCopyright = companyCopyright;
            _displayExtendedCopyrightMessage = displayExtendedCopyrightMessage;
            _helpText = helpText;
            _subset = subset;
            _excelPackage = new ExcelPackage();
            CreateIntroSheet(null, _excelPackage);
        }

        public ExportToExcel(IPagesRepository pagesRepository, IPanesRepository panesRepository, IPartsRepository partsRepository)
        {
            _pagesRepository = pagesRepository;
            _panesRepository = panesRepository;
            _partsRepository = partsRepository;

            _excelPackage = new ExcelPackage();
        }

        private bool IsLowSampleEnabled => _lowSampleCount.HasValue;

        private bool IsLowSample(uint sampleSize)
        {
            return IsLowSampleEnabled && sampleSize <= _lowSampleCount.Value;
        }

        private static void SetLightGreyTitle(ExcelRange datasheetCell, string title)
        {
            datasheetCell.Value = title;
            datasheetCell.Style.Font.Color.SetColor(Color.LightSlateGray);
        }

        private static void SetSampleSizeTitle(ExcelRange dataSheetCell, DateTimeOffset dateOfResult)
        {
            SetLightGreyTitle(dataSheetCell, $"{SampleSizeTitle} ({dateOfResult.Date.ToShortDateString()})");
        }

        private void SetLowSampleTitle(ExcelRange dataSheetCell, DateTimeOffset dateOfResult)
        {
            if (IsLowSampleEnabled)
            {
                SetLightGreyTitle(dataSheetCell, $"{LowSampleTitle} ({dateOfResult.Date.ToShortDateString()})");
            }
        }

        private void SetDate(ExcelRange cell, DateTimeOffset date)
        {
            cell.Value = date.UtcDateTime;
            cell.Style.Numberformat.Format = "d-mmm-yy";
        }

        private static Stream GetExcelStream(ExcelPackage excelPackage)
        {
            var ms = new MemoryStream();
            excelPackage.SaveAs(ms);
            ms.Flush();
            ms.Position = 0;
            return ms;
        }

        private void CreateIntroSheet(string[] filterDescriptions, ExcelPackage excelPackage)
        {
            _introSheet = excelPackage.Workbook.Worksheets.Add("Intro");
            _introSheetRowId = 1;
            _introSheet.Cells[_introSheetRowId, 1].Value = $"{_companyCopyright} {_appTitle} Data Export";
            _introSheet.Cells[_introSheetRowId, 1].Style.Font.Bold = true;
            _introSheet.Cells[_introSheetRowId++, 1].Style.Font.Size = 20;
            _introSheet.Cells[_introSheetRowId++, 1].Value = $"Copyright © {DateTime.Now.Year} {_companyCopyright} All rights reserved.";
            if (_displayExtendedCopyrightMessage)
            {
                _introSheet.Cells[_introSheetRowId++, 1].Value =
                    $"{_companyCopyright} is the owner of all intellectual property rights in the document and its contents.{Environment.NewLine}No part of this document may be published, distributed, extracted, re-utilised, or reproduced in any material form, {Environment.NewLine}except with our express prior written approval.";
            }
            _introSheetRowId++;
            if (_helpText.Length > 0)
            {
                var plaintextHelpText = Regex.Replace(_helpText.Replace("<br>", Environment.NewLine), "<[^>]*>", "");
                _introSheet.Cells[_introSheetRowId++, 1].Value = $"Survey question / chart information: {plaintextHelpText}";
            }
            if (_subset.Length > 0 && _subset != BrandVueDataLoader.All)
            {
                _introSheet.Cells[_introSheetRowId++, 1].Value = $"Subset: {_subset}";
            }

            _introSheet.Cells[_introSheetRowId++, 1].Value = $"Average: {_averageDescriptorName}";

            if (filterDescriptions != null)
            {
                foreach (var filter in filterDescriptions)
                {
                    var results = filter.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (results.Length > 1 && !string.IsNullOrWhiteSpace(results[1]))
                    {
                        _introSheet.Cells[_introSheetRowId++, 1].Value = filter;
                    }
                }
            }

            _introSheetRowId++;
        }

        private void WriteSampleSizeIntroInformation(RankingTableResult data)
        {
            if (data == null)
                return;
            var results = data?.CurrentWeightedDailyResult;
            string entityInstanceName = data.EntityInstance?.Name ?? "No Name";
            WriteSampleSizeIntroInformation(entityInstanceName, results);
        }

        private void WriteSampleSizeIntroInformation(EntityWeightedDailyResults data)
        {
            if (data == null)
                return;
            var results = data?.WeightedDailyResults.LastOrDefault();
            string entityInstanceName = data.EntityInstance?.Name ?? "No Name";
            WriteSampleSizeIntroInformation(entityInstanceName, results);
        }

        private void WriteSampleSizeIntroInformation(string entityInstanceName, WeightedDailyResult dailyResult)
        {
            WriteSampleSizeIntroInformation(entityInstanceName, dailyResult?.Date.Date.ToShortDateString() ?? "", dailyResult?.UnweightedSampleSize ?? 0);
        }
        private void WriteSampleSizeIntroInformation(string entityInstanceName, SampleSizeMetadata sampleSizeMetadata)
        {
            if (sampleSizeMetadata.SampleSizeByMetric != null)
            {
                foreach (var metric in sampleSizeMetadata.SampleSizeByMetric)
                {
                    WriteSampleSizeIntroInformation(metric.Key, sampleSizeMetadata.CurrentDate?.Date.ToShortDateString() ?? "", metric.Value.Unweighted);
                }
            }
            else
            {
                WriteSampleSizeIntroInformation(entityInstanceName, sampleSizeMetadata.CurrentDate?.Date.ToShortDateString() ?? "", sampleSizeMetadata.SampleSize.Unweighted);
            }
        }

        private void WriteSampleSizeIntroInformation(string entityInstanceName, string dateString, double sampleSize)
        {
            _introSheet.Cells[_introSheetRowId, 1].Value =
                $"{entityInstanceName} sample size {dateString}:";
            _introSheet.Cells[_introSheetRowId, 2].Value = sampleSize;
            _introSheetRowId++;
        }

        private void WriteLowSampleSizeIntroInformation(string[] lowSampleTitles)
        {
            if (lowSampleTitles.Any())
            {
                bool hasSeenLowSampleCount = false;
                _introSheet.Cells[_introSheetRowId, 1].Value = "The following have a low sample size";
                foreach (var lowSampleBrand in lowSampleTitles)
                {

                    _introSheet.Cells[_introSheetRowId++, 2].Value = lowSampleBrand;
                    hasSeenLowSampleCount = true;
                }

                if (hasSeenLowSampleCount)
                {
                    _introSheet.Cells[_introSheetRowId, 1].Value = $"Low samples size (<= {_lowSampleCount}) have been detected in this export. Indicated by:";
                    SetCellWarning(_introSheet.Cells[_introSheetRowId, 1]);
                    SetCellLowSample(_introSheet.Cells[_introSheetRowId, 2]);
                }
            }
        }

        private void WriteLowSampleSizeIntroInformation(LowSampleSummary[] lowSampleBrandIds, ImmutableArray<EntityInstance> requestedEntityInstances)
        {
            if (lowSampleBrandIds.Any())
            {
                var titles = lowSampleBrandIds.Select(lowSampleBrand =>
                    requestedEntityInstances.FirstOrDefault(x => x.Id == lowSampleBrand.EntityInstanceId)?.Name ??
                    $"Brand {lowSampleBrand} {lowSampleBrand.Name}").ToArray();
                var hashSet = new HashSet<string>(titles).ToArray();
                WriteLowSampleSizeIntroInformation(hashSet);
            }
        }

        private void SetLowSampleSize(ExcelColumn column, uint sampleSize, ExcelRange lowCell, ExcelRange valueCell)
        {
            if (IsLowSample(sampleSize))
            {
                column.Hidden = false;
                SetCellLowSample(lowCell);
                SetCellWarning(valueCell);
            }
        }


        private static void SetCellLowSample(ExcelRange lowCell)
        {
            lowCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            SetCellWarning(lowCell);
        }

        private static void SetCellWarning(ExcelRange lowCell)
        {
            lowCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            lowCell.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
        }

        public void CreateProfileOverTimeExport(BreakdownByAgeResults profileOverTimeResults, TargetInstances requestedEntities, Measure measure, Subset subset, ICollection<AverageTotalRequestModel> averageRequests, EntityInstance focusInstance)
        {
            WriteSampleSizeIntroInformation(focusInstance?.Name ?? "", profileOverTimeResults.Total.LastOrDefault());
            WriteLowSampleSizeIntroInformation(profileOverTimeResults.LowSampleSummary, requestedEntities.OrderedInstances);

            var dataSheet = _excelPackage.Workbook.Worksheets.Add("By age group");
            dataSheet.Cells[1, 1].Value = "Brand";
            dataSheet.Cells[1, 2].Value = "By age group";

            var row = 2;
            dataSheet.Cells[row, 1].Value = profileOverTimeResults.EntityInstance.Name;

            foreach (var val in profileOverTimeResults.ByAgeGroup)
            {
                WriteProfileOverTimeRow(dataSheet, measure, subset, row, val.Category, val.WeightedDailyResults.ToArray());
                row++;
            }

            if (averageRequests != null)
            {
                foreach (var averageTotalRequestModel in averageRequests)
                {
                    WriteProfileOverTimeRow(dataSheet, measure, subset, row, averageTotalRequestModel.AverageName, profileOverTimeResults.Total);
                }
            }
            AutoFitColumns(dataSheet);
        }

        private void WriteProfileOverTimeRow(ExcelWorksheet dataSheet, Measure measure, Subset subset, int row, string categoryName, WeightedDailyResult[] results)
        {
            int col = 3;
            dataSheet.Cells[row, 2].Value = categoryName;

            foreach (var daily in results)
            {
                dataSheet.Cells[row, col].Value = daily.WeightedResult;
                dataSheet.Cells[row, col].Style.Numberformat.Format = measure.ExcelNumberFormat(subset);
                dataSheet.Cells[row, col + results.Length].Value = daily.UnweightedSampleSize;

                dataSheet.Cells[1, col].Value = daily.Date.ToString("d");
                SetLowSampleSize(dataSheet.Column(col + results.Length * 2), daily.UnweightedSampleSize, dataSheet.Cells[row, col + results.Length * 2], dataSheet.Cells[row, col]);
                SetSampleSizeTitle(dataSheet.Cells[1, col + results.Length], daily.Date);
                SetLowSampleTitle(dataSheet.Cells[1, col + results.Length * 2], daily.Date);
                col++;
            }
        }

        internal void CreateOverTimeExportSummary(Subset currentSubset, HashSet<DateTimeOffset> dates, Dictionary<(string brand, Measure measure), Dictionary<DateTimeOffset, (double value, uint sampleSize)>> data)
        {
            int row = 1;
            var dataSheet = _excelPackage.Workbook.Worksheets.Add("Summary");
            var orderedDates = dates.OrderBy(x => x.Date).ToArray();
            GenerateHeader(dataSheet, orderedDates);
            row++;
            var lowSummaryInformationToDisplayOnIntoSheet = new HashSet<string>();
            var datesOfLowSummary = new HashSet<DateTimeOffset>();

            foreach (var keyValue in data)
            {
                int col = 1;
                dataSheet.Cells[row, col++].Value = keyValue.Key.brand;
                dataSheet.Cells[row, col++].Value = keyValue.Key.measure.Name;
                foreach (var dateTimeOffset in orderedDates)
                {
                    bool setLowSample = true;
                    if (keyValue.Value.ContainsKey(dateTimeOffset))
                    {
                        (double value, uint sampleSize) = keyValue.Value[dateTimeOffset];

                        dataSheet.Cells[row, col].Style.Numberformat.Format = keyValue.Key.measure.ExcelNumberFormat(currentSubset);
                        if (sampleSize > 0)
                        {
                            dataSheet.Cells[row, col].Value = value;
                            setLowSample = IsLowSample(sampleSize);
                        }
                    }

                    if (setLowSample)
                    {
                        SetCellWarning(dataSheet.Cells[row, col]);
                        SetCellLowSample(dataSheet.Cells[row, col + orderedDates.Length]);
                        lowSummaryInformationToDisplayOnIntoSheet.Add($"{keyValue.Key.brand} ({keyValue.Key.measure.Name})");
                        datesOfLowSummary.Add(dateTimeOffset);
                    }
                    col++;
                }
                row++;
            }
            WriteLowSampleSizeIntroInformation(lowSummaryInformationToDisplayOnIntoSheet.ToArray());
            RemoveUnusedLowSampleColumns(orderedDates, datesOfLowSummary, dataSheet);
            AutoFitColumns(dataSheet);
        }

        private static void GenerateHeader(ExcelWorksheet dataSheet, DateTimeOffset[] orderedDates)
        {
            int row = 1;
            int col = 1;
            dataSheet.Cells[row, col++].Value = "Brand";
            dataSheet.Cells[row, col++].Value = "Measure";
            foreach (var dateTimeOffset in orderedDates)
            {
                dataSheet.Cells[row, col].Value = dateTimeOffset.Date.ToString("d");
                dataSheet.Cells[row, col + orderedDates.Length].Value = $"{LowSampleTitle} ({dateTimeOffset.Date:d})";
                col++;
            }
        }

        private static void RemoveUnusedLowSampleColumns(DateTimeOffset[] orderedDates, HashSet<DateTimeOffset> lowSummaryDates,
            ExcelWorksheet dataSheet)
        {
            var lowSummaryDataToDelete = orderedDates.Where(x => !lowSummaryDates.Contains(x)).OrderByDescending(x => x.Date);
            foreach (var dateTimeOffset in lowSummaryDataToDelete)
            {
                var index = Array.IndexOf(orderedDates, dateTimeOffset);
                var colToDelete = index + 2 + orderedDates.Length;
                dataSheet.DeleteColumn(colToDelete + 1);
            }
        }

        public void CreateRankedBrandExport(RankingTableResults rankingTableResults, TargetInstances requestedEntities, Measure measure, Subset subset, EntityInstance focusInstance)
        {
            var currentPeriod = rankingTableResults.Results[0].CurrentWeightedDailyResult.Date;
            var previousPeriod = rankingTableResults.Results[0].PreviousWeightedDailyResult.Date;
            WriteSampleSizeIntroInformation(rankingTableResults.Results.Where(x => x.EntityInstance == focusInstance)?.FirstOrDefault());
            WriteLowSampleSizeIntroInformation(rankingTableResults.LowSampleSummary, requestedEntities.OrderedInstances);

            var dataSheet = _excelPackage.Workbook.Worksheets.Add("Ranked Brands");

            dataSheet.Cells[1, 1].Value = "Brand";

            int row = 2;
            string excelResultFormat = measure.ExcelNumberFormat(subset);
            if (currentPeriod == previousPeriod)
            {
                SetRankingTitlesForPeriod(dataSheet, currentPeriod, 2, 1);

                foreach (var item in rankingTableResults.Results)
                {
                    dataSheet.Cells[row, 1].Value = item.EntityInstance.Name;
                    SetRankingCellsForRow(dataSheet, row, item.CurrentRank, item.CurrentWeightedDailyResult, excelResultFormat, 2, 1);
                    row++;
                }
            }
            else
            {
                SetRankingTitlesForPeriod(dataSheet, currentPeriod, 2, 2);
                SetRankingTitlesForPeriod(dataSheet, previousPeriod, 3, 2);
                dataSheet.Cells[1, 10].Value = SignificanceTitle;

                foreach (var item in rankingTableResults.Results)
                {
                    dataSheet.Cells[row, 1].Value = item.EntityInstance.Name;
                    SetRankingCellsForRow(dataSheet, row, item.CurrentRank, item.CurrentWeightedDailyResult, excelResultFormat, 2, 2);
                    SetRankingCellsForRow(dataSheet, row, item.PreviousRank, item.PreviousWeightedDailyResult, excelResultFormat, 3, 2);
                    SetCellSignificance(dataSheet.Cells[row, 10], item.CurrentWeightedDailyResult);
                    row++;
                }
            }

            AutoFitColumns(dataSheet);
        }

        private void SetRankingCellsForRow(ExcelWorksheet dataSheet, int row, int? rank, WeightedDailyResult weightedDailyResult, string resultFormat, int startingColumn, int columnStep)
        {
            dataSheet.Cells[row, startingColumn].Value = rank.HasValue ? rank.ToString() : "-";
            int resultColumn = startingColumn + columnStep;
            dataSheet.Cells[row, resultColumn].Value = weightedDailyResult.WeightedResult;
            dataSheet.Cells[row, resultColumn].Style.Numberformat.Format = resultFormat;
            dataSheet.Cells[row, startingColumn + columnStep * 2].Value = weightedDailyResult.UnweightedSampleSize;
            int lowSampleColumn = startingColumn + columnStep * 3;
            SetLowSampleSize(dataSheet.Column(lowSampleColumn), weightedDailyResult.UnweightedSampleSize, dataSheet.Cells[row, lowSampleColumn],
                dataSheet.Cells[row, resultColumn]);
        }

        private void SetRankingTitlesForPeriod(ExcelWorksheet dataSheet, DateTimeOffset period, int startingColumn, int columnStep)
        {
            dataSheet.Cells[1, startingColumn].Value = dataSheet.Cells[1, startingColumn + columnStep].Value = period.Date.ToShortDateString();
            SetSampleSizeTitle(dataSheet.Cells[1, startingColumn + columnStep * 2], period);
            SetLowSampleTitle(dataSheet.Cells[1, startingColumn + columnStep * 3], period);
        }

        private static void SetCellSignificance(ExcelRange currentWeightedDailyResult,
            WeightedDailyResult itemCurrentWeightedDailyResult)
        {
            if (itemCurrentWeightedDailyResult.Significance == null)
            {
                return;
            }

            var textColour = Color.Black;
            var sigText = string.Empty;
            switch (itemCurrentWeightedDailyResult.Significance.Value)
            {
                case Significance.Down:
                    textColour = Color.Red;
                    sigText = "Down";
                    break;
                case Significance.Up:
                    textColour = Color.Green;
                    sigText = "Up";
                    break;
            }
            currentWeightedDailyResult.Style.Font.Color.SetColor(textColour);
            currentWeightedDailyResult.Value = sigText;
        }

        public void CreateStackedProfileExport(StackedProfileResults stackedProfileResults, TargetInstances requestedEntities, Measure measure, Subset subset, EntityInstance focusInstance)
        {
            if (measure.EntityCombination.Count() != 1)
            {
                throw new ArgumentException("Measure must have single entity type to export stacked profile data");
            }
            WriteSampleSizeIntroInformation(focusInstance?.Name, stackedProfileResults?.Data?.FirstOrDefault(x => x.EntityInstance.Equals(focusInstance))?.Total.FirstOrDefault());
            WriteLowSampleSizeIntroInformation(stackedProfileResults.LowSampleSummary, requestedEntities.OrderedInstances);
            AddStackedTotalsSheet(stackedProfileResults, measure, subset);
            AddBreakdownSheet("By age group", measure, subset, stackedProfileResults.Data, results => results.ByAgeGroup, item => item?.Measure.Name);
            AddBreakdownSheet("By region", measure, subset, stackedProfileResults.Data, results => results.ByRegion, item => item?.Measure.Name);
            AddBreakdownSheet("By SEG", measure, subset, stackedProfileResults.Data, results => results.BySocioEconomicGroup, item => item?.Measure.Name);

        }

        private void AddStackedTotalsSheet(StackedProfileResults stackedProfileResults, Measure measure, Subset subset)
        {
            var dataSheet = _excelPackage.Workbook.Worksheets.Add("Totals");
            dataSheet.Cells[1, 1].Value = "Brand";
            dataSheet.Cells[1, 2].Value = "Segment";
            var row = 2;

            foreach (var item in stackedProfileResults.Data)
            {
                int col = 1;
                dataSheet.Cells[row, col++].Value = item?.EntityInstance?.Name;
                dataSheet.Cells[row, col++].Value = item?.Measure.Name;

                WriteProfileTotalRow(item, dataSheet, row, col, measure, subset);
                row++;
            }

            AutoFitColumns(dataSheet);
        }

        public void CreateProfileExport(BreakdownResults profileResults, TargetInstances requestedEntities, Measure measure, Subset subset, ICollection<BreakdownResults> averages, EntityInstance focusInstance)
        {
            if (measure.EntityCombination.Count() > 1)
            {
                throw new ArgumentException("Measure must have single entity type to export profile data");
            }
            WriteSampleSizeIntroInformation(focusInstance?.Name ?? "", profileResults.Data?.FirstOrDefault()?.Total?.LastOrDefault());
            WriteLowSampleSizeIntroInformation(profileResults.LowSampleSummary, requestedEntities.OrderedInstances);
            AddBreakdownTotalsSheet(profileResults, measure, subset, averages);
            AddBreakdownSheet("By age group", measure, subset, profileResults.Data, results => results.ByAgeGroup, null, averages);
            AddBreakdownSheet("By region", measure, subset, profileResults.Data, results => results.ByRegion, null, averages);
            AddBreakdownSheet("By SEG", measure, subset, profileResults.Data, results => results.BySocioEconomicGroup, null, averages);
            AddBreakdownSheet("By gender", measure, subset, profileResults.Data, results => results.ByGender, item => item?.Measure.Name, averages);
        }


        private void AddBreakdownTotalsSheet(BreakdownResults profileResults, Measure measure, Subset subset, IEnumerable<BreakdownResults> averages)
        {
            var dataSheet = _excelPackage.Workbook.Worksheets.Add("Totals");
            dataSheet.Cells[1, 1].Value = "Brand";

            var row = 2;
            foreach (var item in profileResults.Data)
            {
                int col = 1;
                dataSheet.Cells[row, col++].Value = item?.EntityInstance?.Name;
                WriteProfileTotalRow(item, dataSheet, row, col, measure, subset);
                row++;
            }

            foreach (var average in averages)
            {
                if (average?.Data != null && average.Data.Length > 0 && average.Data[0].Total.Count > 0)
                {
                    string date = average.Data[0].Total[0].Date.ToString("d");
                    int col = -1;
                    if (dataSheet.Cells[1, 2].Value.ToString().Contains(date)) col = 2;
                    else if (dataSheet.Cells[1, 3].Value.ToString().Contains(date)) col = 3;

                    if (col >= 2)
                    {
                        dataSheet.Cells[row, 1].Value = average.Data[0]?.EntityInstance?.Name;
                        WriteProfileTotalRow(average.Data[0], dataSheet, row, col, measure, subset, col == 3 ? 1 : 0);
                    }
                }
            }
            AutoFitColumns(dataSheet);
        }

        private void WriteProfileTotalRow(BrokenDownResults item, ExcelWorksheet dataSheet, int row, int initialCol, Measure measure, Subset subset, int colOffset = 0)
        {
            var col = initialCol;
            foreach (var val in item.Total)
            {
                dataSheet.Cells[1, col].Value = val.Date.ToString("d");
                dataSheet.Cells[row, col].Value = val.WeightedResult;
                dataSheet.Cells[row, col].Style.Numberformat.Format = measure.ExcelNumberFormat(subset);

                int sampleSizeColumn = col + item.Total.Count + colOffset;
                SetSampleSizeTitle(dataSheet.Cells[1, sampleSizeColumn], val.Date);
                dataSheet.Cells[row, sampleSizeColumn].Value = val.UnweightedSampleSize;

                int lowSampleColumn = col + item.Total.Count * 2 + colOffset * 2;
                SetLowSampleTitle(dataSheet.Cells[1, lowSampleColumn], val.Date);
                SetLowSampleSize(dataSheet.Column(lowSampleColumn), val.UnweightedSampleSize, dataSheet.Cells[row, lowSampleColumn], dataSheet.Cells[row, col]);

                col++;
            }
        }

        private void AddBreakdownSheet(string title, Measure measure, Subset subset, ICollection<BrokenDownResults> allBrandBrokenDownResults,
            Func<BrokenDownResults, ICollection<CategoryResults>> getCollection,
            Func<BrokenDownResults, string> extraColumnResult = null, ICollection<BreakdownResults> averagesBreakDownResults = null)
        {
            if (allBrandBrokenDownResults.Any())
            {
                static bool HasExtraColumn(object val)
                {
                    return val != null;
                }

                var dataSheet = _excelPackage.Workbook.Worksheets.Add(title);
                int firstRowCol = 1;

                dataSheet.Cells[1, firstRowCol++].Value = measure.EntityCombination.SingleOrDefault()?.DisplayNameSingular ?? measure.Name;
                if (HasExtraColumn(extraColumnResult))
                {
                    dataSheet.Cells[1, firstRowCol++].Value = "Segment";
                }

                dataSheet.Cells[1, firstRowCol].Value = title;

                var row = 2;
                foreach (var item in allBrandBrokenDownResults)
                {
                    var resultsCollection = getCollection(item);
                    int colStart = WriteBreakdownItemNames(dataSheet, item, row, 1, HasExtraColumn, extraColumnResult);
                    row = WriteBreakdownItemRows(dataSheet, measure, subset, resultsCollection, row, colStart);
                }

                if (averagesBreakDownResults != null)
                {
                    foreach (var average in averagesBreakDownResults)
                    {
                        foreach (var breakdownResult in average.Data)
                        {
                            var resultsCollection = getCollection(breakdownResult);
                            using var enumerator = resultsCollection.GetEnumerator();
                            if (enumerator.MoveNext() && enumerator.Current != null)
                            {
                                int colStart = WriteBreakdownItemNames(dataSheet, breakdownResult, row, 1, HasExtraColumn, extraColumnResult);
                                int colOffset = 0;
                                string date = enumerator.Current.WeightedDailyResults[0].Date.ToString("d");
                                var dateHeader = dataSheet.Cells[1, colStart + 2].Value;
                                if (dateHeader?.ToString()?.Equals(date) ?? false) colOffset = 1;
                                row = WriteBreakdownItemRows(dataSheet, measure, subset, resultsCollection, row, colStart, colOffset);
                            }
                        }
                    }
                }

                AutoFitColumns(dataSheet);
            }
        }

        private static int WriteBreakdownItemNames(ExcelWorksheet dataSheet, BrokenDownResults item, int row, int colStart, Func<object, bool> hasExtraColumn, Func<BrokenDownResults, string> extraColumnResult = null)
        {
            int col = colStart;
            dataSheet.Cells[row, col++].Value = item?.EntityInstance?.Name;
            if (hasExtraColumn(extraColumnResult))
            {
                dataSheet.Cells[row, col++].Value = extraColumnResult(item);
            }

            return col;
        }

        private int WriteBreakdownItemRows(ExcelWorksheet dataSheet, Measure measure, Subset subset, ICollection<CategoryResults> resultsCollection, int rowStart, int colStart, int colOffset = 0)
        {
            int row = rowStart;
            foreach (var val in resultsCollection)
            {
                var col = colStart;
                dataSheet.Cells[row, col++].Value = val.Category;

                foreach (var daily in val.WeightedDailyResults)
                {
                    dataSheet.Cells[row, col + colOffset].Value = daily.WeightedResult;
                    dataSheet.Cells[row, col + colOffset].Style.Numberformat.Format = measure.ExcelNumberFormat(subset);
                    dataSheet.Cells[1, col + colOffset].Value = daily.Date.ToString("d");

                    int sampleSizeColumn = col + val.WeightedDailyResults.Count + colOffset * 2;
                    SetSampleSizeTitle(dataSheet.Cells[1, sampleSizeColumn], daily.Date);
                    dataSheet.Cells[row, sampleSizeColumn].Value = daily.UnweightedSampleSize;

                    int lowSampleColumn = col + colOffset * 3 + val.WeightedDailyResults.Count * 2;
                    SetLowSampleTitle(dataSheet.Cells[1, lowSampleColumn], daily.Date);
                    SetLowSampleSize(dataSheet.Column(lowSampleColumn), daily.UnweightedSampleSize, dataSheet.Cells[row, lowSampleColumn], dataSheet.Cells[row, col]);

                    col++;
                }

                row++;
            }

            return row;
        }

        public void FinalizeExport()
        {
            _introSheet.Cells.AutoFitColumns();
        }

        public Stream ToStream()
        {
            return GetExcelStream(_excelPackage);
        }

        public void CreateOverTimeExport(CuratedResultsForExport exportData, OverTimeAverageResultsForMetric[] averages, Subset currentSubset,
            TargetInstances requestedInstances, TargetInstances[] filterInstances, EntityInstance focusInstance)
        {
            WriteSampleSizeIntroInformation(exportData.Data.FirstOrDefault()?.Data?.FirstOrDefault(y => y.EntityInstance == focusInstance));
            WriteLowSampleSizeIntroInformation(exportData.LowSampleSummary, requestedInstances.OrderedInstances);
            for (int index = 0; index < exportData.Data.Length; index++)
            {
                var brandResultsForMeasure = exportData.Data[index];
                var weightedDailyResults = brandResultsForMeasure.Data;
                var dataSheet = _excelPackage.Workbook.Worksheets.Add(LimitUniqueSheetName($"{index + 1}. {brandResultsForMeasure.Measure.Name}"));
                switch (brandResultsForMeasure.Measure.CalculationType)
                {
                    case CalculationType.Text:
                        CreateWordleExport(dataSheet, weightedDailyResults, brandResultsForMeasure);
                        break;

                    default:
                        CreateOverTimeExport(dataSheet, weightedDailyResults, brandResultsForMeasure, currentSubset, averages.SingleOrDefault(a => a.Measure == exportData.Data[index].Measure), requestedInstances, filterInstances);
                        break;
                }
                AutoFitColumns(dataSheet);
            }
        }

        public void CreateOverTimeCrossbreakExport(CrossbreakCompetitionResults exportData,
            OverTimeResults totalData,
            Measure measure,
            CrossMeasure breaks,
            Subset subset,
            TargetInstances requestedInstances,
            TargetInstances[] filterInstances,
            EntityInstance focusInstance)
        {
            if (totalData != null)
            {
                WriteSampleSizeIntroInformation(totalData.EntityWeightedDailyResults?.FirstOrDefault(y => y.EntityInstance == focusInstance));
            }
            else if (exportData.SampleSizeMetadata != null && exportData.SampleSizeMetadata.SampleSizeByEntity.Any())
            {
                var prefix = exportData.SampleSizeMetadata.SampleSizeEntityInstanceName != null ?
                    $"{exportData.SampleSizeMetadata.SampleSizeEntityInstanceName} - " : "";
                foreach (KeyValuePair<string, UnweightedAndWeightedSample> kvp in exportData.SampleSizeMetadata.SampleSizeByEntity)
                {
                    var name = $"{prefix}{kvp.Key}";
                    var date = exportData.SampleSizeMetadata.CurrentDate?.Date.ToShortDateString() ?? "";
                    var sample = kvp.Value;
                    WriteSampleSizeIntroInformation(name, date, sample.Unweighted);
                }
            }
            WriteLowSampleSizeIntroInformation(exportData.LowSampleSummary, requestedInstances.OrderedInstances);
            var dataSheet = _excelPackage.Workbook.Worksheets.Add(LimitUniqueSheetName($"{1}. {measure.Name}"));
            CreateOverTimeCrossbreakExport(dataSheet, exportData, breaks, measure, subset, requestedInstances, filterInstances);
            AutoFitColumns(dataSheet);
        }

        public void CreateCategoryExport(ExcelExportCategoryModel model)
        {
            AddCategorySummarySheet(model);
            foreach (var card in model.CategoryResultCards.OrderBy(c => c.PaneIndex))
            {
                AddCategoryResultSheet(model, card);
            }
        }

        private void AddCategorySummarySheet(ExcelExportCategoryModel model)
        {
            var dataSheet = _excelPackage.Workbook.Worksheets.Add("Summary");
            var includeIndex = model.CategoryResultCards.Any(r => r.IsDetailed);
            AddCategoryHeader(dataSheet, model, true, includeIndex);

            var dataRow = 2;
            foreach (var card in model.CategoryResultCards.OrderBy(c => c.PaneIndex))
            {
                var topNumber = card.IsDetailed ? 10 : 3;
                var topResults = SortResultsFunction(model.SortKey)(card.Results).Take(topNumber);
                foreach (var result in topResults)
                {
                    AddCategoryResultRow(dataSheet, dataRow, result, card.Title);
                    dataRow++;
                }
            }

            AutoFitColumns(dataSheet);
        }

        private void AddCategoryResultSheet(ExcelExportCategoryModel model, CategoryExportResultCard card)
        {
            var dataSheet = _excelPackage.Workbook.Worksheets.Add(card.Title);
            AddCategoryHeader(dataSheet, model, false, card.IsDetailed);

            var dataRow = 2;
            foreach (var result in SortResultsFunction(model.SortKey)(card.Results))
            {
                AddCategoryResultRow(dataSheet, dataRow, result);
                dataRow++;
            }

            if (!string.IsNullOrEmpty(card.QuestionText))
            {
                dataRow++;
                dataSheet.Cells[dataRow, 1].Value = "Question Text";
                dataSheet.Cells[dataRow, 2].Value = card.QuestionText;
            }

            AutoFitColumns(dataSheet);
        }

        private void AddCategoryHeader(ExcelWorksheet dataSheet, ExcelExportCategoryModel model, bool includeDemographic, bool hasIndex)
        {
            var columnId = 1;
            if (includeDemographic)
            {
                dataSheet.Cells[1, columnId].Value = "Demographic";
                columnId++;
            }

            dataSheet.Cells[1, columnId].Value = "Metric";
            dataSheet.Cells[1, columnId + 1].Value = model.FirstBaseVariableName ?? model.ActiveBrand;
            dataSheet.Cells[1, columnId + 2].Value = model.SecondBaseVariableName ?? "Average of competitor brands";
            if (hasIndex)
            {
                dataSheet.Cells[1, columnId + 3].Value = "Index";
            }
        }

        private void AddCategoryResultRow(ExcelWorksheet dataSheet, int dataRow, CategoryExportResult result, string demographic = null)
        {
            var columnId = 1;
            if (demographic != null)
            {
                dataSheet.Cells[dataRow, columnId].Value = demographic;
                columnId++;
            }

            dataSheet.Cells[dataRow, columnId].Value = result.Name;
            dataSheet.Cells[dataRow, columnId + 1].Value = result.FirstBaseValue;
            dataSheet.Cells[dataRow, columnId + 1].Style.Numberformat.Format = "0.0%";
            dataSheet.Cells[dataRow, columnId + 2].Value = result.SecondBaseValue;
            dataSheet.Cells[dataRow, columnId + 2].Style.Numberformat.Format = "0.0%";
            if (result.Index != null)
            {
                dataSheet.Cells[dataRow, columnId + 3].Value = result.Index;
            }
        }

        private Func<CategoryExportResult[], IEnumerable<CategoryExportResult>> SortResultsFunction(CategorySortKey sortKey)
        {
            switch (sortKey)
            {
                case CategorySortKey.BestScores:
                    return results => results
                        .Where(r => r.FirstBaseValue != null)
                        .OrderByDescending(r => r.FirstBaseValue);
                case CategorySortKey.WorstScores:
                    return results => results
                        .Where(r => r.FirstBaseValue != null)
                        .OrderBy(r => r.FirstBaseValue);
                case CategorySortKey.OverPerforming:
                    return results => results
                        .Where(r => r.FirstBaseValue != null && r.SecondBaseValue != null)
                        .OrderByDescending(r => r.FirstBaseValue - r.SecondBaseValue);
                case CategorySortKey.UnderPerforming:
                    return results => results
                        .Where(r => r.FirstBaseValue != null && r.SecondBaseValue != null)
                        .OrderBy(r => r.FirstBaseValue - r.SecondBaseValue);
            }

            return results => results;
        }


        private static void AutoFitColumns(ExcelWorksheet sheet)
        {
            var hiddenColumns = new List<int>();
            for (int colIndex = 1; colIndex <= sheet.Dimension.Columns; colIndex++)
            {
                var column = sheet.Column(colIndex);
                if (column.Hidden)
                {
                    hiddenColumns.Add(colIndex);
                }
            }
            sheet.Cells.AutoFitColumns();
            foreach (int hiddenColumn in hiddenColumns)
            {
                sheet.Column(hiddenColumn).Hidden = true;
            }
        }

        private static void CreateWordleExport(ExcelWorksheet dataSheet, EntityWeightedDailyResults[] weightedDailyResultses,
            ResultsForMeasure resultsForMeasure)
        {
            dataSheet.Cells[1, 1].Value = "Brand";
            dataSheet.Cells[1, 2].Value = "Data";
            dataSheet.Cells[1, 3].Value = "Date";
            int currentCol = 4;
            var lookupToCol = new Dictionary<string, int>();

            for (var row = 0; row < weightedDailyResultses.Length; row++)
            {
                dataSheet.Cells[row + 2, 1].Value = weightedDailyResultses[row].EntityInstance.Name;
                dataSheet.Cells[row + 2, 2].Value = resultsForMeasure.Measure.Name;
                for (var col = 0; col < weightedDailyResultses[row].WeightedDailyResults.Count; col++)
                {
                    var columnIndex = currentCol;
                    var wr = weightedDailyResultses[row].WeightedDailyResults[col];
                    dataSheet.Cells[row + 2, 3].Value = wr.Date.Date.ToShortDateString();
                    if (!lookupToCol.ContainsKey(wr.Text))
                    {
                        lookupToCol[wr.Text] = currentCol++;
                        dataSheet.Cells[1, columnIndex].Value = wr.Text;
                    }

                    columnIndex = lookupToCol[wr.Text];
                    dataSheet.Cells[row + 2, columnIndex].Value = wr.UnweightedSampleSize;
                    dataSheet.Cells[row + 2, columnIndex].Style.Numberformat.Format = resultsForMeasure.Measure.NumberFormat;
                }
            }
        }

        private void CreateOverTimeExport(ExcelWorksheet dataSheet, EntityWeightedDailyResults[] brandWeightedDailyResults, ResultsForMeasure resultsForMeasure, Subset currentSubset, OverTimeAverageResultsForMetric averageForMetric, TargetInstances requestedInstances, TargetInstances[] filterInstances)
        {
            var entityTypeNames = new string[filterInstances.Length];
            var combinedEntityInstanceNames = new string[filterInstances.Length];

            for (int i = 0; i < filterInstances.Length; i++)
            {
                string entityTypeName = filterInstances[i].OrderedInstances.Length <= 1
                    ? filterInstances[i].EntityType.DisplayNameSingular
                    : filterInstances[i].EntityType.DisplayNamePlural;
                entityTypeNames[i] = entityTypeName;
                combinedEntityInstanceNames[i] = GetNamesFromEntityInstances(filterInstances[i].OrderedInstances);
            }

            dataSheet.Cells[1, 1].Value = requestedInstances.EntityType.DisplayNameSingular ?? string.Empty;
            var columnIndex = 2;
            foreach (var name in entityTypeNames)
            {
                dataSheet.Cells[1, columnIndex].Value = name;
                columnIndex++;
            }
            dataSheet.Cells[1, columnIndex].Value = "Data";

            var namedMeasures = brandWeightedDailyResults.Select(r =>
                (
                    RowName: r.EntityInstance?.Name,
                    Columns: r.WeightedDailyResults.Select(d => (d.Date, WeightedResult: d.WeightedResult, d.UnweightedSampleSize))
                )
            ).ToList();
            if (averageForMetric != null)
            {
                foreach (var average in averageForMetric.Results)
                {
                    if (average.Results != null)
                        namedMeasures.Add((RowName: GetAverageDisplayName(average.AverageName), Columns: average.Results.WeightedDailyResults.Select(d => (d.Date, WeightedResult: d.WeightedResult, d.UnweightedSampleSize))));
                }
            }

            for (var row = 0; row < namedMeasures.Count; row++)
            {
                (string rowName, var columns) = namedMeasures[row];
                dataSheet.Cells[row + 2, 1].Value = rowName;
                var colIndex = 2;
                foreach (string combinedNames in combinedEntityInstanceNames)
                {
                    dataSheet.Cells[row + 2, colIndex].Value = combinedNames;
                    colIndex++;
                }

                dataSheet.Cells[row + 2, colIndex].Value = resultsForMeasure.Measure.Name;
                colIndex++;
                var columnsList = columns.ToList();
                foreach ((var date, double result, uint sampleSize) in columnsList)
                {
                    if (row == 0)
                    {
                        dataSheet.Cells[1, colIndex].Value = date.Date.ToShortDateString();
                        SetSampleSizeTitle(dataSheet.Cells[1, colIndex + columnsList.Count], date);
                        SetLowSampleTitle(dataSheet.Cells[1, colIndex + columnsList.Count * 2], date);
                    }
                    else if (!dataSheet.Cells[1, colIndex].Value.ToString().Contains(date.Date.ToShortDateString()))
                    {
                        throw new ArgumentOutOfRangeException(nameof(averageForMetric),
                            $"Overtime Export: mismatch in dates [{dataSheet.Cells[1, colIndex].Value}] [{date.Date.ToShortDateString()}]. [Row: ]{row}, [Col: {colIndex}]");
                    }

                    dataSheet.Cells[row + 2, colIndex].Value = result;
                    dataSheet.Cells[row + 2, colIndex].Style.Numberformat.Format = resultsForMeasure.Measure.ExcelNumberFormat(currentSubset);
                    dataSheet.Cells[row + 2, colIndex + columnsList.Count].Value = sampleSize;
                    SetLowSampleSize(dataSheet.Column(colIndex + columnsList.Count * 2), sampleSize,
                        dataSheet.Cells[row + 2, colIndex + columnsList.Count * 2], dataSheet.Cells[row + 2, colIndex]);
                    colIndex++;
                }
            }
        }

        private static string GetAverageDisplayName(string averageName)
        {
            var displayName = averageName;
            if (!displayName.ToLower().Contains("competitive average"))
            {
                displayName += " (competitive average)";
            }

            return displayName;
        }

        private void CreateOverTimeCrossbreakExport(ExcelWorksheet dataSheet, CrossbreakCompetitionResults data, CrossMeasure breaks, Measure measure, Subset subset, TargetInstances requestedInstances, TargetInstances[] filterInstances)
        {
            var headerColumn = 1;
            dataSheet.Cells[1, headerColumn++].Value = requestedInstances.EntityType.DisplayNameSingular ?? string.Empty;
            dataSheet.Cells[1, headerColumn++].Value = breaks.MeasureName;

            var combinedEntityInstanceNames = new string[filterInstances.Length];
            for (int i = 0; i < filterInstances.Length; i++)
            {
                string entityTypeName = filterInstances[i].OrderedInstances.Length <= 1
                    ? filterInstances[i].EntityType.DisplayNameSingular
                    : filterInstances[i].EntityType.DisplayNamePlural;
                combinedEntityInstanceNames[i] = GetNamesFromEntityInstances(filterInstances[i].OrderedInstances);
                dataSheet.Cells[1, headerColumn++].Value = entityTypeName;
            }

            dataSheet.Cells[1, headerColumn++].Value = "Data";

            var firstEntityResults = data.InstanceResults.First().EntityResults;
            var rowIndex = 2;
            for (var i = 0; i < firstEntityResults.Length; i++)
            {
                var entityInstance = firstEntityResults[i].EntityInstance;
                dataSheet.Cells[rowIndex, 1].Value = entityInstance.Name;
                foreach (var breakResult in data.InstanceResults)
                {
                    var entityResult = breakResult.EntityResults.Single(r => r.EntityInstance.Id == entityInstance.Id);
                    var colIndex = 2;
                    dataSheet.Cells[rowIndex, colIndex++].Value = breakResult.BreakName;
                    foreach (var instanceName in combinedEntityInstanceNames)
                    {
                        dataSheet.Cells[rowIndex, colIndex++].Value = instanceName;
                    }
                    dataSheet.Cells[rowIndex, colIndex++].Value = measure.Name;

                    var numResults = entityResult.WeightedDailyResults.Count;
                    for (var resultIndex = 0; resultIndex < numResults; resultIndex++)
                    {
                        var dailyResult = entityResult.WeightedDailyResults[resultIndex];
                        if (string.IsNullOrEmpty(dataSheet.Cells[1, colIndex].Value?.ToString()))
                        {
                            dataSheet.Cells[1, colIndex].Value = dailyResult.Date.Date.ToShortDateString();
                            SetSampleSizeTitle(dataSheet.Cells[1, colIndex + numResults], dailyResult.Date);
                            SetLowSampleTitle(dataSheet.Cells[1, colIndex + numResults * 2], dailyResult.Date);
                        }
                        else if (!dataSheet.Cells[1, colIndex].Value.ToString().Contains(dailyResult.Date.Date.ToShortDateString()))
                        {
                            throw new ArgumentOutOfRangeException(nameof(data),
                                $"Overtime Export: mismatch in dates [{dataSheet.Cells[1, colIndex].Value}] [{dailyResult.Date.Date.ToShortDateString()}]. [Row: ]{rowIndex}, [Col: {colIndex}]");
                        }
                        dataSheet.Cells[rowIndex, colIndex].Value = dailyResult.WeightedResult;
                        dataSheet.Cells[rowIndex, colIndex].Style.Numberformat.Format = measure.ExcelNumberFormat(subset);
                        dataSheet.Cells[rowIndex, colIndex + numResults].Value = dailyResult.UnweightedSampleSize;
                        SetLowSampleSize(dataSheet.Column(colIndex + numResults * 2), dailyResult.UnweightedSampleSize,
                            dataSheet.Cells[rowIndex, colIndex + numResults * 2], dataSheet.Cells[rowIndex, colIndex]);

                        colIndex++;
                    }

                    rowIndex++;
                }
            }
        }

        private static string GetNamesFromEntityInstances(ImmutableArray<EntityInstance> instances)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < instances.Length; i++)
            {
                if (i != 0)
                {
                    builder.Append(", ");
                }
                builder.Append(instances[i].Name);
            }
            return builder.ToString();
        }

        public void CreateMultiMetricsExport(MultiMetricResults multiMetricResults, TargetInstances requestedEntities, IReadOnlyCollection<Measure> measures, Subset currentSubset, Dictionary<string, MultiMetricAverageResults> averages, EntityInstance focusInstance)
        {
            var resultsByMeasureThenEntity = ReshapeAndMergeResultsIntoOutputFormat(multiMetricResults, measures, averages).ToList();

            var entityResultsByMeasure = resultsByMeasureThenEntity
                .ToLookup(record => record.Measure)
                .Select((measureGrouping, index) => (Measure: measureGrouping.Key, Index: index, EntityResults: measureGrouping.Select(t => (t.EntityName, t.Results)).ToArray()));

            var uniqueOrderedResultDates = resultsByMeasureThenEntity
                .SelectMany(d => d.Results.YieldNonNullEntries().Select(r => r.Date))
                .ToImmutableSortedSet();

            var dateIndexLookup = uniqueOrderedResultDates
            .Select((d, i) => (Date: d, Index: i))
            .ToImmutableDictionary(d => d.Date, d => d.Index);

            CreateIntroPage(multiMetricResults, requestedEntities, focusInstance);
            CreateSummarySheet(currentSubset, requestedEntities.EntityType, uniqueOrderedResultDates, dateIndexLookup, resultsByMeasureThenEntity.ToArray());
            CreateMeasureSheets(currentSubset, requestedEntities.EntityType, uniqueOrderedResultDates, dateIndexLookup, entityResultsByMeasure);
        }

        private static IEnumerable<DailyMeasureResult> ReshapeAndMergeResultsIntoOutputFormat(MultiMetricResults multiMetricResults,
            IReadOnlyCollection<Measure> measures, Dictionary<string, MultiMetricAverageResults> averages)
        {
            var activeAndCompetitorsSeries = multiMetricResults.ActiveSeries
                .Yield()
                .Concat(multiMetricResults.ComparisonSeries);

            return multiMetricResults.OrderedMeasures.SelectMany((measureName, index) =>
                GetDailyResultsForEachEntityForMeasure(multiMetricResults, measures, averages, measureName, activeAndCompetitorsSeries, index));
        }

        private static IEnumerable<DailyMeasureResult> GetDailyResultsForEachEntityForMeasure(MultiMetricResults multiMetricResults,
            IReadOnlyCollection<Measure> measures, Dictionary<string, MultiMetricAverageResults> averages, string measureName,
            IEnumerable<MultiMetricSeries> activeAndCompetitorsSeries, int index)
        {
            var measure = measures.Single(m => m.Name.Equals(measureName, StringComparison.OrdinalIgnoreCase));
            var entityResultsForMeasure = activeAndCompetitorsSeries.Select(e => (EntityName: e.EntityInstance?.Name, Data: e.OrderedData[index]));
            var entityMeasureResults = entityResultsForMeasure.Select(results => new DailyMeasureResult(results.EntityName, measure, results.Data));

            if (multiMetricResults.ComparisonSeries.Length <= 1 || averages is null || !averages.Any())
                return entityMeasureResults;

            var averageRecords = averages.Select(x => new DailyMeasureResult(x.Key, measure, x.Value.Average[index].WeightedDailyResult.Yield().ToArray()));
            return entityMeasureResults.Concat(averageRecords);
        }

        private void CreateIntroPage(MultiMetricResults multiMetricResults, TargetInstances requestedEntities,
            EntityInstance focusInstance)
        {
            WriteSampleSizeIntroInformation(focusInstance?.Name, multiMetricResults.SampleSizeMetadata);
            WriteLowSampleSizeIntroInformation(multiMetricResults.LowSampleSummary, requestedEntities.OrderedInstances);
        }

        private void CreateIntroPage(SplitMetricResults splitMetricResults, TargetInstances requestedEntities,
            EntityInstance focusInstance)
        {
            WriteSampleSizeIntroInformation(focusInstance?.Name, splitMetricResults.SampleSizeMetadata);
            WriteLowSampleSizeIntroInformation(splitMetricResults.LowSampleSummary, requestedEntities.OrderedInstances);
        }

        private void CreateMeasureSheets(Subset currentSubset, EntityType entityType,
            ImmutableSortedSet<DateTimeOffset> uniqueOrderedResultDates, IReadOnlyDictionary<DateTimeOffset, int> dateIndexLookup,
            IEnumerable<(Measure Measure, int Index, (string EntityName, WeightedDailyResult[] Results)[] EntityResults)> entityResultsByMeasure)
        {
            foreach (var measureGrouping in entityResultsByMeasure)
            {
                CreateMeasureSheet(currentSubset, entityType, uniqueOrderedResultDates, dateIndexLookup, measureGrouping);
            }
        }

        private void CreateMeasureSheet(Subset currentSubset, EntityType entityType,
            ImmutableSortedSet<DateTimeOffset> uniqueOrderedResultDates, IReadOnlyDictionary<DateTimeOffset, int> dateIndexLookup,
            (Measure Measure, int Index, (string EntityName, WeightedDailyResult[] Results)[] EntityResults) measureGrouping)
        {
            int startingDateColumn = entityType.IsProfile ? 1 : 2; //First column of daily results
            (var measure, int index, var entityResults) = measureGrouping;
            string sheetName = MakeSheetName(measure.Name, index);
            var dataSheet = _excelPackage.Workbook.Worksheets.Add(sheetName);
            if (!entityType.IsProfile) dataSheet.Cells[1, 1].Value = entityType.DisplayNameSingular;
            SetDateHeaders(uniqueOrderedResultDates, dataSheet, startingDateColumn);
            for (int rowIndex = 0; rowIndex < entityResults.Length; rowIndex++)
            {
                var measureEntityCategoryResult = entityResults[rowIndex];
                (string entityName, var results) = measureEntityCategoryResult;
                int activeRow = rowIndex + 2; //To offset 0 base and header row
                dataSheet.Cells[activeRow, 1].Value = entityName;
                WriteDailyResults(currentSubset, dateIndexLookup, results, dataSheet, activeRow, startingDateColumn, measure, uniqueOrderedResultDates.Count);
            }

            AutoFitColumns(dataSheet);
        }

        private void CreateSummarySheet(Subset currentSubset, EntityType entityType,
            ImmutableSortedSet<DateTimeOffset> uniqueOrderedResultDates, IReadOnlyDictionary<DateTimeOffset, int> dateIndexLookup,
            DailyMeasureResult[] resultsByMeasureThenEntity)
        {
            int startingDateColumn = entityType.IsProfile ? 2 : 3; //First column of daily results
            var dataSheet = _excelPackage.Workbook.Worksheets.Add("Summary");
            dataSheet.Cells[1, 1].Value = entityType.IsProfile ? "Metric" : entityType.DisplayNameSingular;
            if (!entityType.IsProfile) dataSheet.Cells[1, 2].Value = "Metric";
            SetDateHeaders(uniqueOrderedResultDates, dataSheet, startingDateColumn);

            int resultCount = uniqueOrderedResultDates.Count;
            for (int rowIndex = 0; rowIndex < resultsByMeasureThenEntity.Length; rowIndex++)
            {
                var resultSet = resultsByMeasureThenEntity[rowIndex];
                int activeRow = rowIndex + 2; //To offset 0 base and header row
                dataSheet.Cells[activeRow, 1].Value = entityType.IsProfile ? resultSet.Measure.Name : resultSet.EntityName;
                if (!entityType.IsProfile) dataSheet.Cells[activeRow, 2].Value = resultSet.Measure.Name;
                WriteDailyResults(currentSubset, dateIndexLookup, resultSet.Results, dataSheet, activeRow, startingDateColumn, resultSet.Measure, resultCount);
            }
            AutoFitColumns(dataSheet);
        }

        private void SetDateHeaders(ImmutableSortedSet<DateTimeOffset> uniqueOrderedResultDates, ExcelWorksheet dataSheet, int startingColumn)
        {
            int resultCount = uniqueOrderedResultDates.Count;
            for (int index = 0; index < resultCount; index++)
            {
                var date = uniqueOrderedResultDates[index];
                dataSheet.Cells[1, startingColumn + index].Value = date.ToString("d");
                SetLowSampleTitle(dataSheet.Cells[1, startingColumn + index + resultCount], date);
            }
        }

        private void WriteDailyResults(Subset currentSubset, IReadOnlyDictionary<DateTimeOffset, int> dateIndexLookup, WeightedDailyResult[] results, ExcelWorksheet dataSheet, int activeRow, int startingDateColumn, Measure measure, int resultCount)
        {
            foreach (var dailyResult in results.YieldNonNullEntries())
            {
                int dateIndex = dateIndexLookup[dailyResult.Date];
                var activeResultCell = dataSheet.Cells[activeRow, startingDateColumn + dateIndex];
                activeResultCell.Value = dailyResult.WeightedResult;
                activeResultCell.Style.Numberformat.Format = measure.ExcelNumberFormat(currentSubset);
                int lowSampleDateColumnIndex = startingDateColumn + dateIndex + resultCount;
                SetLowSampleSize(dataSheet.Column(lowSampleDateColumnIndex), dailyResult.UnweightedSampleSize, dataSheet.Cells[activeRow, lowSampleDateColumnIndex], activeResultCell);
            }
        }

        //Careful there seems to be a limit of 31 chars for a named sheet...
        //
        //https://stackoverflow.com/questions/3681868/is-there-a-limit-on-an-excel-worksheets-name-length
        //
        private const int MaxSheetnameLength = 31;
        private static string LimitUniqueSheetName(string sheetName)
        {
            if (sheetName.Length > MaxSheetnameLength)
            {
                sheetName = sheetName.Substring(0, MaxSheetnameLength);
            }
            return sheetName;
        }

        private static string MakeSheetName(string sheetName, int columnIndex)
        {
            if (sheetName.Length > MaxSheetnameLength)
            {
                var colName = $"~{columnIndex}";
                sheetName = sheetName.Substring(0, MaxSheetnameLength - colName.Length) + colName;
            }
            return sheetName;
        }

        private record DailyMeasureResult(string EntityName, Measure Measure, WeightedDailyResult[] Results);

        public void ExportPagesPanesAndParts()
        {
            var pagesSheet = _excelPackage.Workbook.Worksheets.Add("Pages");
            pagesSheet.Cells["A1"].LoadFromCollection(_pagesRepository.GetPages().OrderBy(p => p.Id), true);
            pagesSheet.Cells.AutoFitColumns();

            var panesSheet = _excelPackage.Workbook.Worksheets.Add("Panes");
            panesSheet.Cells["A1"].LoadFromCollection(_panesRepository.GetPanes().OrderBy(p => p.Id), true);
            panesSheet.Cells.AutoFitColumns();

            var partsSheet = _excelPackage.Workbook.Worksheets.Add("Parts");
            partsSheet.Cells["A1"].LoadFromCollection(_partsRepository.GetParts().OrderBy(p => p.Id), true);
            partsSheet.Cells.AutoFitColumns();
        }

        public void CreateSplitMetricExport(SplitMetricResults splitMetricResults, Subset subset, TargetInstances requestedInstances, 
            TargetInstances[] filterInstances, EntityInstance focusInstance, string name, string[] measureNames, Measure measure)
        {
            CreateIntroPage(splitMetricResults, requestedInstances, focusInstance);

            var splitMetricSheet = _excelPackage.Workbook.Worksheets.Add(name);
            WriteSplitMetricResults(splitMetricSheet, splitMetricResults, requestedInstances, filterInstances, measureNames, measure, subset);
        }

        private void WriteSplitMetricResults(ExcelWorksheet splitMetricSheet, SplitMetricResults splitMetricResults, 
            TargetInstances requestedInstances, TargetInstances[] filterInstances, string[] measureNames, Measure measure, Subset subset)
        {
            splitMetricSheet.Cells[1, 1].Value = requestedInstances.EntityType.IsBrand ? "Brand" : "Metric";
            splitMetricSheet.Cells[1, 2].Value = requestedInstances.EntityType.IsBrand ? "Metric" : "Brand";

            var metricNames = new List<string>();
            for(int i = 0; i < requestedInstances.OrderedInstances.Length; i++)
            {
                if (filterInstances.First().EntityType.IsBrand)
                {
                    metricNames.Add(filterInstances.First().OrderedInstances.First().Identifier);
                }
                else
                {
                    metricNames.Add($"{filterInstances.First().EntityType.DisplayNameSingular}:{filterInstances.First().OrderedInstances.First().Identifier}");
                }
            };

            splitMetricSheet.Cells[2, 1].LoadFromCollection(splitMetricResults.OrderedMeasures);
            splitMetricSheet.Cells[2, 2].LoadFromCollection(metricNames);

            var columnNumber = 3;
            foreach (var weightedDailyResult in splitMetricResults.OrderedResults)
            {
                splitMetricSheet.Cells[1, columnNumber].Value = measureNames[columnNumber - 3];
                var rowNumber = 2;
                for (int i = 0; i < weightedDailyResult.Length; i++)
                {
                    splitMetricSheet.Cells[rowNumber, columnNumber].Value = weightedDailyResult[i].WeightedResult;
                    splitMetricSheet.Cells[rowNumber, columnNumber].Style.Numberformat.Format = measure.ExcelNumberFormat(subset);
                    rowNumber++;
                }
                columnNumber++;
            }
            splitMetricSheet.Cells.AutoFitColumns();
        }

        // Function has been adjusted to handle a null focusInstance
        // Strictly speaking, the focusInstance should always be set, but this is a temporary fix to handle the case where it is not
        // This can be reverted once we are confident that the focusInstance is always set
        private void SortMultiEntityResultsByFocusEntityValues(StackedMultiEntityResults exportData,
            Dictionary<string, IEnumerable<OverTimeAverageResults>> averages, EntityInstance focusInstance)
        {
            bool IsFocusInstance(EntityWeightedDailyResults e) => focusInstance is null || e.EntityInstance.Id == focusInstance.Id;
            exportData.ResultsPerInstance = exportData.ResultsPerInstance
                   .OrderByDescending(x => x.Data.First(IsFocusInstance)?.WeightedDailyResults?.Last()?.WeightedResult)
                   .ToArray();
        }

        public void CreateMultiEntityAllEntityExport(StackedMultiEntityResults exportData, Dictionary<string, IEnumerable<OverTimeAverageResults>> averages, Subset currentSubset, TargetInstances requestedInstances, TargetInstances[] filterInstances, EntityInstance focusInstance, Measure measure)
        {
            SortMultiEntityResultsByFocusEntityValues(exportData, averages, focusInstance);
            WriteMultiEntityAllEntityIntroInformation(exportData, requestedInstances, focusInstance);

            var resultsSheet = _excelPackage.Workbook.Worksheets.Add(measure.Name);

            var weightedResultCount = exportData.ResultsPerInstance.First().Data.First().WeightedDailyResults.Count;
            var instanceCount = exportData.ResultsPerInstance.Length;
            var columnOffset = instanceCount;

            WriteMultiEntityAllEntityHeadings(resultsSheet, exportData, weightedResultCount, columnOffset);
            WriteMultiEntityAllEntityResults(resultsSheet, exportData, averages, currentSubset, measure, weightedResultCount, columnOffset);
            WriteMultiEntityAllEntityAverages(resultsSheet, exportData, averages, currentSubset, measure, weightedResultCount, columnOffset);
        }

        // Function has been adjusted to handle a null focusInstance
        // Strictly speaking, the focusInstance should always be set, but this is a temporary fix to handle the case where it is not
        // This can be reverted once we are confident that the focusInstance is always set
        private void WriteMultiEntityAllEntityIntroInformation(StackedMultiEntityResults exportData, TargetInstances requestedInstances, EntityInstance focusInstance)
        {
            bool IsFocusInstance(EntityWeightedDailyResults e) => focusInstance is null || e.EntityInstance.Id == focusInstance.Id;
            var sampleSizes = exportData.ResultsPerInstance.Select(results => results.Data.First(IsFocusInstance)?.WeightedDailyResults.Last().UnweightedSampleSize);

            var firstSampleSize = sampleSizes.First();
            if (sampleSizes.All(sampleSize => sampleSize == firstSampleSize))
            {

                WriteSampleSizeIntroInformation(
                    focusInstance is not null ? focusInstance.Name : "(No focus instance)",
                    exportData.ResultsPerInstance.Select(results => results.Data.First(IsFocusInstance)?.WeightedDailyResults.Last()).First());
            }
            else
            {
                foreach (var instanceResult in exportData.ResultsPerInstance)
                {
                    var weightedDailyResult = instanceResult.Data.First(e => e.EntityInstance.Id == focusInstance.Id)
                        ?.WeightedDailyResults.Last();
                    WriteSampleSizeIntroInformation(instanceResult.FilterInstance.Name, weightedDailyResult);
                }
            }

            WriteLowSampleSizeIntroInformation(exportData.LowSampleSummary, requestedInstances.OrderedInstances);
        }

        private void WriteMultiEntityAllEntityHeadings(ExcelWorksheet resultsSheet, StackedMultiEntityResults exportData, int weightedResultCount, int columnOffset)
        {
            var firstResultsInstance = exportData.ResultsPerInstance.First();
            for (var dataIndex = 0; dataIndex < firstResultsInstance.Data.Length; dataIndex++)
            {
                for (var resultIndex = 0; resultIndex < weightedResultCount; resultIndex++)
                {
                    var rowNum = (dataIndex * weightedResultCount) + resultIndex + 2;
                    resultsSheet.Cells[rowNum, 1].Value = firstResultsInstance.Data[dataIndex].EntityInstance.Name;
                    SetDate(resultsSheet.Cells[rowNum, 2], firstResultsInstance.Data[dataIndex].WeightedDailyResults[resultIndex].Date);
                }
            }

            for (var instanceIndex = 0; instanceIndex < exportData.ResultsPerInstance.Length; instanceIndex++)
            {
                var columnNum = instanceIndex + 3;
                var title = exportData.ResultsPerInstance[instanceIndex].FilterInstance.Name;
                resultsSheet.Cells[1, columnNum].Value = title;
                SetLightGreyTitle(resultsSheet.Cells[1, columnOffset + columnNum], $"{title} ({SampleSizeTitle})");
            }
        }

        private void WriteMultiEntityAllEntityResults(ExcelWorksheet resultsSheet, StackedMultiEntityResults exportData,
            Dictionary<string, IEnumerable<OverTimeAverageResults>> averages, Subset currentSubset, Measure measure, int weightedResultCount, int columnOffset)
        {
            for (var instanceIndex = 0; instanceIndex < exportData.ResultsPerInstance.Length; instanceIndex++)
            {
                for (var dataIndex = 0; dataIndex < exportData.ResultsPerInstance[instanceIndex].Data.Length; dataIndex++)
                {
                    for (var resultIndex = 0; resultIndex < weightedResultCount; resultIndex++)
                    {
                        var rowNum = (dataIndex * weightedResultCount) + resultIndex + 2;
                        var columnNum = instanceIndex + 3;
                        var result = exportData.ResultsPerInstance[instanceIndex].Data[dataIndex].WeightedDailyResults[resultIndex];
                        WriteMultiEntityAllEntityValueAndSampleSize(resultsSheet, result, rowNum, columnNum,
                            columnOffset, measure, currentSubset);
                    }
                }
            }
        }

        private void WriteMultiEntityAllEntityAverages(ExcelWorksheet resultsSheet, StackedMultiEntityResults exportData, Dictionary<string, IEnumerable<OverTimeAverageResults>> averages, Subset currentSubset, Measure measure, int weightedResultCount, int columnOffset)
        {
            var rowNum = (exportData.ResultsPerInstance.First().Data.Length * weightedResultCount) + 2;
            foreach (var (name, average) in averages)
            {
                for (var resultIndex = 0; resultIndex < weightedResultCount; resultIndex++)
                {
                    var currentRow = rowNum + resultIndex;
                    resultsSheet.Cells[currentRow, 1].Value = name;
                    SetDate(resultsSheet.Cells[currentRow, 2], average.First().WeightedDailyResults[resultIndex].Date);
                }

                for (var instanceIndex = 0; instanceIndex < exportData.ResultsPerInstance.Length; instanceIndex++)
                {
                    var columnNum = instanceIndex + 3;
                    var currentAverage = average.FirstOrDefault(x =>
                        x.WeightedDailyResults.First().Text == exportData.ResultsPerInstance[instanceIndex].FilterInstance.Name);
                    if (currentAverage != null)
                    {
                        for (var resultIndex = 0; resultIndex < weightedResultCount; resultIndex++)
                        {
                            var result = currentAverage.WeightedDailyResults[resultIndex];
                            WriteMultiEntityAllEntityValueAndSampleSize(resultsSheet, result, rowNum + resultIndex, columnNum,
                                columnOffset, measure, currentSubset);
                        }
                    }
                }

                rowNum += weightedResultCount;
            }
        }

        private void WriteMultiEntityAllEntityValueAndSampleSize(ExcelWorksheet resultsSheet, WeightedDailyResult result, int rowNum, int columnNum, int columnOffset, Measure measure, Subset currentSubset)
        {
            resultsSheet.Cells[rowNum, columnNum].Value = result.WeightedResult;
            resultsSheet.Cells[rowNum, columnNum].Style.Numberformat.Format = measure.ExcelNumberFormat(currentSubset);
            resultsSheet.Cells[rowNum, columnOffset + columnNum].Value = result.UnweightedSampleSize;
            SetLowSampleSize(resultsSheet.Column(columnNum), result.UnweightedSampleSize,
                resultsSheet.Cells[rowNum, columnOffset + columnNum], resultsSheet.Cells[rowNum, columnNum]);
        }
    }
}