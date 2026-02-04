using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace BrandVue.Services.Llm.Discovery
{
    public class NavigationOptionFunction : IDiscoveryFunctionToolInvocation
    {
        [Required]
        [Description("Sentence to succinctly explain the following link.")]
        public string MessageToUser { get; init; }
        [Required]
        [Description("Page Selected.")]
        public string Page { get; init; }
        [Required]
        [Description("Chart type to display.")]
        public ChartType ChartType { get; init; }
        [Description("If true, the end date will be now and use most recent data available. If false, we need an end date.")]
        public bool UseMostRecentData { get; init; }
        [Description("End date for the analysis period in YYYY-MM-DD format, only set when CurrentData is false.")]
        [DataType(DataType.Date)]
        public DateTime? End { get; init; }
        [Description("Averaging or aggregation period for the data. This controls the granularity of the graph's data points.")]
        public IntervalOptions Average { get; init; }
        [Description("The total time period to analyze, ie. how far back in time the data will go..")]
        public DateRangeOptions Range { get; init; }
        [Description("Period to compare to when charts have a comparison with a previous period")]
        public PeriodOptions? Period { get; init; }
        [Description("Filters results by demographic.")]
        public DemographicFilter Filters { get; init; }
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChartType
    {
        [EnumMember(Value = "FullPage")]
        [Description("Multiple charts on a page, only available for specific predefined reports")]
        FullPage,
        [EnumMember(Value = "OverTime")]
        [Description("Basic line chart showing competitors over time")]
        OverTime,
        [EnumMember(Value = "Competition")]
        [Description("Bar charts showing competitors")]
        Competition,
        [EnumMember(Value = "Profile")]
        [Description("Bar chart with demograph breakdown")]
        Profile,
        [EnumMember(Value = "ProfileOverTime")]
        [Description("Over time line chart with demographic breakdown")]
        ProfileOverTime,
        [EnumMember(Value = "PerformanceVsKeyCompetitors")]
        [Description("Bar chart with key competitors")]
        PerformanceVsKeyCompetitors,
        [EnumMember(Value = "RankingTable")]
        [Description("Table ranking the brands")]
        RankingTable,
        [EnumMember(Value = "ScorecardPerformance")]
        [Description("Box and whisker graph showing performance against peer averages")]
        ScorecardPerformance,
    }
    public class DemographicFilter
    {
        [Description("Filter. Age range for demographic analysis, can be a hyphenated range or start with < or >, or a single specific age.")]
        public string Age { get; init; }
        [Description("Filter. Gender for demographic analysis.")]
        public string Gender { get; init; }
        [Description("Filter. Region for analysis, 1 for London, 2 for South, 3 for Midlands, 4 for North, 5 for Scotland and Northern Ireland")]
        public int Region { get; init; }
        [Description("Filter. Segment for demographic analysis, with 0 for AB, 1 for C1, 2 for C2, and 3 for DE")]
        public int Seg { get; init; }
    }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IntervalOptions
    {
        //todo these should come from the Averages repository
        [EnumMember(Value = "MonthlyOver12Months")]
        [Description("12 month moving average")]
        MonthlyOver12Months,
        [EnumMember(Value = "MonthlyOver6Months")]
        [Description("6 month moving average")]
        MonthlyOver6Months,
        [EnumMember(Value = "MonthlyOver3Months")]
        [Description("3 month moving average")]
        MonthlyOver3Months,
        [EnumMember(Value = "Monthly")]
        [Description("Monthly")]
        Monthly,
        [EnumMember(Value = "Quarterly")]
        [Description("Quarterly")]
        Quarterly,
        [EnumMember(Value = "Days28")]
        [Description("28 Days")]
        Days28,
        [EnumMember(Value = "Days14")]
        [Description("14 Days")]
        Days14,
        [EnumMember(Value = "Weeks12")]
        [Description("12 Weeks")]
        Weeks12
    }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DateRangeOptions
    {
        // TODO - we could generate a list of ranges based on the current date and pass that to our messages

        [EnumMember(Value = "LastQuarter")]
        [Description("Previous complete calendar quarter")]
        LastQuarter,
        [EnumMember(Value = "ThisQuarter")]
        [Description("Current quarter")]
        ThisQuarter,
        [EnumMember(Value = "ThisYear")]
        [Description("Current year")]
        ThisYear,
        [EnumMember(Value = "LastYear")]
        [Description("Last 12 months")]
        LastYear,
        [EnumMember(Value = "Last2Years")]
        [Description("Last 24 months")]
        Last2Years,
        [EnumMember(Value = "Last5Years")]
        [Description("Last 5 years")]
        Last5Years,
        [EnumMember(Value = "LastMonth")]
        [Description("Previous month")]
        LastMonth,
        [EnumMember(Value = "Last6Months")]
        [Description("Current month")]
        ThisMonth,
        [EnumMember(Value = "Weeks12")]
        [Description("Last 6 months")]
        Last6Months
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PeriodOptions
    {
        [Description("Current Period Only")]
        CurrentPeriodOnly,
        [Description("Current and Previous Month")]
        CurrentAndPreviousPeriod,
        [Description("Current and 6 months ago")]
        LastSixMonths,
        [Description("Current and 12 months ago")]
        SameLastYear
    }
}