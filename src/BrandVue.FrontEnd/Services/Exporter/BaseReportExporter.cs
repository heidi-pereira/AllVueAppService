using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.Models;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Utils;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Averages;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData;
using System.Text.RegularExpressions;
using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Services.Exporter
{
    public class BaseReportExporter
    {
        private readonly IPagesRepository _pagesRepository;
        private readonly IPanesRepository _panesRepository;
        private readonly IPartsRepository _partsRepository;
        private readonly IMeasureBaseDescriptionGenerator _baseDescriptionGenerator;
        protected IProductContext _productContext;
        protected IWeightingPlanRepository _weightingPlanRepository;
        protected IAverageDescriptorRepository _averageDescriptorRepository;
        protected IResponseWeightingRepository _responseWeightingRepository;

        public BaseReportExporter(
            IPagesRepository pagesRepository,
            IPanesRepository panesRepository,
            IPartsRepository partsRepository,
            IProductContext productContext,
            IWeightingPlanRepository weightingPlanRepository,
            IAverageDescriptorRepository averageDescriptorRepository,
            IMeasureBaseDescriptionGenerator baseDescriptionGenerator,
            IResponseWeightingRepository responseWeightingRepository)
        {
            _pagesRepository = pagesRepository;
            _panesRepository = panesRepository;
            _partsRepository = partsRepository;
            _productContext = productContext;
            _weightingPlanRepository = weightingPlanRepository;
            _averageDescriptorRepository = averageDescriptorRepository;
            _baseDescriptionGenerator = baseDescriptionGenerator;
            _responseWeightingRepository = responseWeightingRepository;
        }

        protected static string GetMetricDisplayText(PartDescriptor part, Measure measure)
        {
            if (!string.IsNullOrWhiteSpace(part.HelpText))
            {
                return part.HelpText.StripHtmlTags();
            }
            if (!string.IsNullOrWhiteSpace(measure.HelpText))
            {
                return measure.HelpText.StripHtmlTags();
            }
            return measure.DisplayName;
        }

        protected PartDescriptor GetPart(int partId)
        {
            return _partsRepository.GetById(partId);
        }

        protected List<PartDescriptor> GetParts(int pageid)
        {
            var page = _pagesRepository.GetPages()
                .Single(x => x.Id == pageid);
            var paneIdsForPage = _panesRepository.GetPanes()
                .Where(pane => pane.PageName == page.Name)
                .Select(pane => pane.Id)
                .ToHashSet();
            var reportParts = _partsRepository.GetParts()
                .Where(part => paneIdsForPage.Contains(part.PaneId));
            return reportParts.OrderBy(x => int.TryParse(x.Spec2, out var value) ? value : -1).ToList();
        }

        protected ReportOrder GetSortOrderForPart(ReportOrder reportSettingsSortOrder, PartDescriptor part)
        {
            if (part.ReportOrder.HasValue)
            {
                return part.ReportOrder.Value;
            }
            return reportSettingsSortOrder;
        }

        protected BaseExpressionDefinition GetBaseExpressionOverride(PartDescriptor part, Measure measure, BaseDefinitionType? reportBaseTypeOverride, int? reportBaseVariableId)
        {
            if (!measure.HasCustomBase)
            {
                var isBaseOverrideDisabled = part.PartType == PartType.ReportsCardFunnel;

                if (part.BaseExpressionOverride != null && !isBaseOverrideDisabled)
                {
                    return part.BaseExpressionOverride;
                }

                if (reportBaseTypeOverride.HasValue)
                {
                    return new BaseExpressionDefinition
                    {
                        BaseType = isBaseOverrideDisabled ? BaseDefinitionType.SawThisQuestion : reportBaseTypeOverride.Value,
                        BaseVariableId = isBaseOverrideDisabled ? null : reportBaseVariableId,
                        BaseMeasureName = part.Spec1
                    };
                }
            }

            return null;
        }

        protected string GetBaseDescription(PartDescriptor part, Measure measure, BaseDefinitionType? reportBaseTypeOverride, int? reportBaseVariableId)
        {
            var baseExpressionOverride = GetBaseExpressionOverride(part, measure, reportBaseTypeOverride, reportBaseVariableId);
            return GetBaseDescription(measure, baseExpressionOverride);
        }

        protected string GetBaseDescription(Measure measure, BaseExpressionDefinition baseExpressionOverride)
        {
            if (measure.HasCustomBase || baseExpressionOverride == null)
            {
                return measure.SubsetSpecificBaseDescription;
            }
            return _baseDescriptionGenerator.BaseExpressionDefinitionToString(baseExpressionOverride);
        }

        protected void SetBaseDescriptionForMeasure(Measure measure, Subset subset)
        {
            var (baseDescription, hasCustomBase) = _baseDescriptionGenerator.GetBaseDescriptionAndHasCustomBase(measure, subset);
            measure.SubsetSpecificBaseDescription = baseDescription;
            measure.HasCustomBase = hasCustomBase;
        }

        protected IEnumerable<InstanceResult> GetOrderedCrosstabResults(ReportOrder order, IEnumerable<InstanceResult> results, string Overall_Score_Column)
        {
            switch (order)
            {
                case ReportOrder.ResultOrderDesc:
                    return results.OrderByDescending(r => r.Values[Overall_Score_Column].Result);
                case ReportOrder.ResultOrderAsc:
                    return results.OrderBy(r => r.Values[Overall_Score_Column].Result);
                case ReportOrder.ScriptOrderDesc:
                    return results;
                case ReportOrder.ScriptOrderAsc:
                    return results.Reverse();
                default:
                    return results;
            }
        }

        protected void SortCompetitionResults(ReportOrder order, CompetitionResults results)
        {
            if (order == ReportOrder.ResultOrderAsc || order == ReportOrder.ResultOrderDesc)
            {
                //sort results in descending order by sum of values across breaks
                var sortedIndices = results.PeriodResults.First().ResultsPerEntity.Select((_, index) =>
                {
                    var sum = results.PeriodResults.Aggregate(0.0, (total, current) => total += current.ResultsPerEntity[index].WeightedDailyResults[0].WeightedResult);
                    return (Index: index, Sum: sum);
                }).OrderByDescending(t => t.Sum).ToArray();

                foreach (var period in results.PeriodResults)
                {
                    var data = period.ResultsPerEntity;
                    period.ResultsPerEntity = sortedIndices.Select(t => data[t.Index]).ToArray();
                }
            }

            if (order == ReportOrder.ScriptOrderAsc || order == ReportOrder.ResultOrderAsc)
            {
                foreach (var period in results.PeriodResults)
                {
                    period.ResultsPerEntity = period.ResultsPerEntity.AsEnumerable().Reverse().ToArray();
                }
            }
        }

        protected void SortCrossbreakCompetitionResults(ReportOrder order, CrossbreakCompetitionResults results)
        {
            if (order == ReportOrder.ResultOrderAsc || order == ReportOrder.ResultOrderDesc)
            {
                //sort results in descending order by sum of values across periods
                var sortedIndices = results.InstanceResults.First().EntityResults.Select((_, index) =>
                {
                    var sum = results.InstanceResults.Aggregate(0.0, (total, current) => total += current.EntityResults[index].WeightedDailyResults[0].WeightedResult);
                    return (Index: index, Sum: sum);
                }).OrderByDescending(t => t.Sum).ToArray();

                foreach (var breakResults in results.InstanceResults)
                {
                    var data = breakResults.EntityResults;
                    breakResults.EntityResults = sortedIndices.Select(t => data[t.Index]).ToArray();
                }
            }

            if (order == ReportOrder.ScriptOrderAsc || order == ReportOrder.ResultOrderAsc)
            {
                foreach (var breakResults in results.InstanceResults)
                {
                    breakResults.EntityResults = breakResults.EntityResults.AsEnumerable().Reverse().ToArray();
                }
            }
        }

        protected void ShowTopForCompetitionResults(int? showTopN, CompetitionResults results)
        {
            if (showTopN.HasValue)
            {
                foreach (var period in results.PeriodResults)
                {
                    period.ResultsPerEntity = period.ResultsPerEntity.Take(showTopN.Value).ToArray();
                }
            }
        }

        protected void ShowTopForCrossbreakCompetitionResults(int? showTopN, CrossbreakCompetitionResults results)
        {
            if (showTopN.HasValue)
            {
                foreach (var breakResult in results.InstanceResults)
                {
                    breakResult.EntityResults = breakResult.EntityResults.Take(showTopN.Value).ToArray();
                }
            }
        }

        public static string GetSampleSizeDescription(SampleSizeMetadata sampleSizeMeta, Subset subset)
        {
            var description = $"Sample size: n = {sampleSizeMeta.SampleSize.Unweighted.AddCommaSeparators(subset)}";
            if (sampleSizeMeta.SampleSize.HasDifferentWeightedSample)
            {
                description += $" (weighted n = {sampleSizeMeta.SampleSize.Weighted.AddCommaSeparators(subset)})";
            }

            if (sampleSizeMeta?.SampleSizeByEntity != null && sampleSizeMeta.SampleSizeByEntity.Count > 0)
            {
                var allSampleSizes = sampleSizeMeta.SampleSizeByEntity.Select(kvp => kvp.Value).ToArray();
                var unweightedValues = allSampleSizes.Select(v => v.Unweighted).Distinct().ToArray();
                var weightedValues = allSampleSizes.Select(v => v.Weighted).Distinct().ToArray();
                var allHasSameSample = allSampleSizes.All(v => !v.HasDifferentWeightedSample);
                if (unweightedValues.Length == 1 && (allHasSameSample || weightedValues.Length == 1))
                {
                    description = $"Sample size: n = {unweightedValues.Single().AddCommaSeparators(subset)}";
                    if (!allHasSameSample)
                    {
                        description += $" (weighted n = {weightedValues.Single().AddCommaSeparators(subset)})";
                    }
                }
                else
                {
                    var entitySamples = sampleSizeMeta.SampleSizeByEntity.Select(kvp =>
                    {
                        var sample = $"n = {kvp.Value.Unweighted.AddCommaSeparators(subset)}";
                        if (kvp.Value.HasDifferentWeightedSample)
                        {
                            sample += $"; weighted n = {kvp.Value.Weighted.AddCommaSeparators(subset)}";
                        }
                        return $"{kvp.Key} ({sample})";
                    });
                    description = $"Sample sizes: {string.Join("; ", entitySamples)}";
                }
            }

            return description;
        }

        protected string ExcelNumberFormat(Measure measure, Subset currentSubset, int decimalPlaces)
        {
            //this is adapted from formatting in metric.ts
            switch (measure.NumberFormat)
            {
                case "time_minutes":
                    switch (decimalPlaces)
                    {
                        case 1:
                            return "0.0";
                        case 2:
                            return "0.00";
                        default:
                            return "0";
                    }
                case "currency":
                    switch (currentSubset.Iso2LetterCountryCode)
                    {
                        case "us":
                            return "$#,##0.00";
                        case "gb":
                            return "£#,##0.00";
                        default:
                            return "€#,##0.00";
                    }

                case "+0;-0;0":
                case "0;-0;0":
                    switch (decimalPlaces)
                    {
                        case 1:
                            return "0.0;-0.0;0.0";
                        case 2:
                            return "0.00;-0.00;0.00";
                        default:
                            return "0;-0;0";
                    }

                //this slightly confusing logic is to match how brandvue behaves
                case "+0.0;-0.0;0.0":
                    switch (decimalPlaces)
                    {
                        case 2:
                            return "0.00;-0.00;0.00";
                        default:
                            return "0.0;-0.0;0.0";
                    }

                case "0.0;-0.0;0.0":
                    switch (decimalPlaces)
                    {
                        case 0:
                            return "0.0;-0.0;0.0";
                        default:
                            return "0.00;-0.00;0.00";
                    }

                default:
                    switch (decimalPlaces)
                    {
                        case 1:
                            return "0.0%";
                        case 2:
                            return "0.00%";
                        default:
                            return "0%";
                    }
            };
        }

        protected string SurveyName => _productContext.SurveyName;
        protected bool HasWeightingModel(Subset subset) =>
            _weightingPlanRepository.GetLoaderWeightingPlansForSubset(_productContext.ShortCode, _productContext.SubProductId, subset.Id).Any() ||
            _responseWeightingRepository.AreThereAnyRootResponseWeights(subset.Id);

        protected bool DoResultsHaveWeighting(Subset subset, AverageDescriptor average)
        {
            if (average.WeightingMethod == WeightingMethod.QuotaCell)
            {
                if (!_productContext.IsAllVue)
                    return true;

                return HasWeightingModel(subset);
            }
            return false;
        }
    }
}
