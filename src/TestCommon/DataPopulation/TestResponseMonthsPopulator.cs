using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;

namespace TestCommon.DataPopulation
{
    public class TestResponseMonthsPopulator
    {
        public TestResponseMonthsPopulator(ResponseFieldManager responseFieldManager)
        {
            TestResponseFactory = new TestResponseFactory(responseFieldManager);
        }

        public TestResponseFactory TestResponseFactory { get; }

        public (CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData)[] CreateRespondentsForResults(Measure measure,
            CalculationPeriodSpan calculationPeriodSpan,
            (EntityValue[] EntityValues, QuotaCell QuotaCell, ValueResponseCount[] ResponseSpecs)[] intendedResults)
        {
            var measureStartDate = (measure.StartDate ?? DateTimeOffset.MinValue).AddMinutes(1);
            var startDate = calculationPeriodSpan.StartDate > measureStartDate
                ? calculationPeriodSpan.StartDate
                : measureStartDate;
            var endDate = calculationPeriodSpan.EndDate.Date.AddDays(1).AddMinutes(-1);
            var months = GetMonthsBetween(startDate, endDate).ToList();

            return GenerateResponsesSplitEvenlyAcrossDateRange(months, intendedResults, measure.Field);
        }

        private IEnumerable<DateTimeOffset> GetMonthsBetween(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            for (var currentDate = startDate; currentDate < endDate; currentDate = currentDate.AddMonths(1))
            {
                yield return currentDate;
            }
        }

        private (CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData)[] GenerateResponsesSplitEvenlyAcrossDateRange(List<DateTimeOffset> months, (EntityValue[] EntityValues, QuotaCell QuotaCell, ValueResponseCount[] ResponseSpecs)[] intendedResults, ResponseFieldDescriptor field)
        {
            var allProfileResponseEntities = new List<(CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData)>();
            foreach (var month in months)
            {
                var monthStart = month.GetFirstDayOfMonth();
                var monthEnd = month.GetLastDayOfMonthOnOrAfter();
                var monthEntities = intendedResults.SelectMany(intendedResult =>
                    intendedResult.ResponseSpecs.SelectMany(valueResponseCount =>
                        CreateResponsesForMonth(monthStart, monthEnd, valueResponseCount, (intendedResult.EntityValues, intendedResult.QuotaCell), months.Count, field)
                    )
                );
                allProfileResponseEntities.AddRange(monthEntities.ToArray());
            }

            return allProfileResponseEntities.ToArray();
        }

        private IEnumerable<(CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData)> CreateResponsesForMonth(DateTimeOffset monthStartDate, DateTimeOffset monthEndDate, ValueResponseCount valueResponseCount,
            (EntityValue[] EntityValues, QuotaCell QuotaCell) intendedResult,
            int numberOfMonthsToSplitOver, ResponseFieldDescriptor field)
        {
            if (valueResponseCount.Count % numberOfMonthsToSplitOver != 0)
                throw new ArgumentOutOfRangeException(nameof(valueResponseCount), valueResponseCount.Count,
                    $"Dividing number of responses [{valueResponseCount.Count}] over months [{numberOfMonthsToSplitOver}] results in fractional number of responses per month, which will make predicting the output impossible");

            int numberOfResponses = (int) (valueResponseCount.Count / numberOfMonthsToSplitOver);
            var answers = new[] {TestAnswer.For(field, valueResponseCount.Value, intendedResult.EntityValues)};
            var responsesWithAnswers = Enumerable.Repeat(answers, numberOfResponses).ToArray();
            var testResponsesInQuotaCell = TestResponseFactory.CreateTestResponses(monthStartDate, monthEndDate, responsesWithAnswers).ToArray();
            return testResponsesInQuotaCell.Select(r =>
            {
                var profileResponseQuota = new CellResponse(r.ProfileResponse, intendedResult.QuotaCell);
                return (profileResponseQuota, r.EntityMeasureData);
            });
        }
    }
}