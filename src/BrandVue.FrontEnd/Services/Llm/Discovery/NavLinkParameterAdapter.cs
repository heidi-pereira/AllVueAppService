using BrandVue.Models;

namespace BrandVue.Services.Llm.Discovery
{
    public class NavLinkParameterAdapter : IOutputAdapter<AnnotatedQueryParams>
    {
        public IEnumerable<AnnotatedQueryParams> CreateFromFunctionCalls(
            IEnumerable<IDiscoveryFunctionToolInvocation> toolInvocations)
        {
            foreach (var invocation in toolInvocations)
            {
                if (invocation is NavigationOptionFunction navOption)
                {
                    var dateRange = (navOption.UseMostRecentData ? DateTime.UtcNow : navOption.End ?? DateTime.UtcNow).CreateDateRangeFromOptions(navOption.Range);
                    
                    yield return new AnnotatedQueryParams
                    {
                        MessageToUser = navOption.MessageToUser,
                        PageName = navOption.Page,
                        PartType = navOption.ChartType.ToString(),
                        QueryParams = new QueryParams()
                        {
                            Period = navOption.Period switch
                            {
                                PeriodOptions.CurrentPeriodOnly => ComparisonPeriodSelection.CurrentPeriodOnly,
                                PeriodOptions.CurrentAndPreviousPeriod => ComparisonPeriodSelection.CurrentAndPreviousPeriod,
                                PeriodOptions.LastSixMonths => ComparisonPeriodSelection.LastSixMonths,
                                PeriodOptions.SameLastYear => ComparisonPeriodSelection.SameLastYear,
                                _ => null
                            },
                            Average = navOption.Average.ToString(),
                            Start = dateRange.start.Date.ToString("yyyy-MM-dd"),
                            End = dateRange.end.Date.ToString("yyyy-MM-dd"),
                            
                        },
                    };
                }
            }
        }
    }
}