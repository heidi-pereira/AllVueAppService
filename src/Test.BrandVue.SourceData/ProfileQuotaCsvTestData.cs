using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.SourceData
{
    public class ProfileQuotaCsvTestData
    {

        // Categories tab in map file. For a profiling field (field column) this provides entity ids to human readable labels mapping (categories column)
        public static IReadOnlyCollection<Dictionary<string, string>> ProfilingFieldsDictionaries => LoadCsvData(ProfilingFieldNames, ProfilingFieldValues).ToList();
        
        // Filters tab in map file. For a human readable label (right hand side of a categories bit) it provides the quota cell key part (left hand side of a categories bit)
        public static IReadOnlyCollection<Dictionary<string, string>> FilterFieldsDictionaries => LoadCsvData(FilterFieldNames, FilterFieldValues).ToList();

        private static readonly string[] FilterFieldNames =
        {
            "name", "field", "allText", "filterType", "valueType", "categories", "question", "active", "include",
            "defaultValue","subset"
        };

        private static readonly string[][] FilterFieldValues = {
            new[] {"Gender", "Gender", "Male & Female", "Profile", "Category", "F:Female|M:Male", "Are you male or female?", "1", "", ""},
            new[] {"Age", "Age", "All Ages", "Profile", "Category", "16-24:16-24|25-39:25-39|40-54:40-54|55-74:55-74", "How old are you?", "1", "", ""},
            new[] {"Country", "Country", "All Countries", "Profile", "Category", "1:UK|4:USA", "Where do you live?", "1", "", ""},
            new[] {"Region", "Region", "All Regions", "Profile", "Category", "L:London|N:North|M:Midlands|S:South", "Where do you live?", "", "", "","UK"},
            new[] {"Region", "Region", "All Regions", "Profile", "Category", "M:Midwest|N:Northeast|S:South|W:West", "Where in the USA do you live?", "", "", "","US"},
            new[] {"Location", "Location", "All Location Types", "Profile", "Category", "1:I live in a city with a population more than 500,000 people|2:I live about a 30 minute drive away from a city of more than 500,000 people|3:I do not live in or a 30 minute drive away from a city of more than 500,000 people", "Which of the following best describes the area you live in?<inst>Please select one option</inst>", "", "", ""},
            new[] {"Dress size", "Dress_size_female", "All Dress Sizes", "Profile", "Category", "1-2:Small|3:Medium|4-6:Large", "Typically what size of clothing do you wear for dresses?", "1", "", ""},
            new[] {"Household Income", "Household_Income", "All Incomes", "Profile", "Value", "0-74999:Low|75000-500000:High", "What is your ANNUAL HOUSEHOLD income in (USD - $)?", "", "", ""},
            new[] {"Ethnicity", "Ethnicity", "All Ethnicities", "Profile", "Category", "1:White|2:Hispanic / Latino|3: Black / African American", "Would you describe yourself as:", "", "", ""},
            new[] {"Moving average", "Moving_Average", "", "Profile", "Category", "SevenDays:7 days|FourteenDays:14 days|TwentyEightDays:28 days|TwelveWeeks:12 weeks|TwentySixWeeks:26 weeks|Monthly:Monthly|Quarterly:Quarterly", "", "", "", "TwentyEightDays"},
            new[] {"Seg", "Seg", "All SEGs", "Profile", "Category", "1:ABC1|2:C2DE", "", "", "", "", "UK"},
            new[] {"Seg", "Seg", "All SEGs", "Profile", "Category", "L:Low|H:High", "", "", "", "", "US"}
        };
        
        private static readonly string[] ProfilingFieldNames =
        {
            "field", "type", "name", "categories", "question", "","subset" //??
        };


        private static readonly string[][] ProfilingFieldValues = {
            new [] {"Gender", "Category", "Gender", "0:Female|1:Male", "Are you male or female?", ""},
            new [] {"Age", "Value", "Age", "16-24:16-24|25-39:25-39|40-54:40-54|55-74:55-74", "How old are you?", ""},
            new [] {"Country", "Category", "Country", "1:UK|4:USA", "Where do you live?", ""},
            new [] {"UKRegion", "Category", "Region", "11:London|1-5:North|6-8:Midlands|9-10,12:South", "Where do you live?", "","UK"},
            new [] {"US_state", "Category", "Region", "14,15,16,17,23,24,26,28,35,37,43,52:Midwest|7,8,9,20,21,22,30,31,33,40,41,47,51:Northeast|1,3,4,10,11,18,19,25,32,34,36,38,42,44,45,48,50:South|2,5,6,12,13,27,29,39,46,49,53:West", "Where in the USA do you live?", "US ONLY","US"},
            new [] {"Location", "Category", "Location", "1:I live in a city with a population more than 500,000 people|2:I live about a 30 minute drive away from a city of more than 500,000 people|3:I do not live in or a 30 minute drive away from a city of more than 500,000 people", "Which of the following best describes the area you live in? <inst>Please select one option</inst>", ""},
            new [] {"Dress_size_female", "Category", "Dress_size_female", "1-2:Small|3:Medium|4-6:Large", "Typically what size of clothing do you wear for dresses?", ""},
            new [] {"Household_Income", "Value", "Household_Income", "0-749:Low|750-5000:High", "What is your ANNUAL HOUSEHOLD income in (USD - $)?", "US ONLY"},
            new [] {"Ethnicity", "Category", "Ethnicity", "1:White|2:Hispanic / Latino|3: Black / African American", "Would you describe yourself as:", "US ONLY"},
            new [] {"SEG1", "Category", "SEG1", "1-3:ABC1|4-8:C2DE|9:Retired", "", ""},
            new [] {"SEG2", "Category", "SEG2", "1-3:ABC1|4-8:C2DE|9:Retired", "", ""},
        };
        
        private static IEnumerable<Dictionary<string, string>> LoadCsvData(string[] fieldNames, string[][] fieldValues)
        {
            return fieldValues.Select(row =>
                row.Select((cell, i) => new KeyValuePair<string, string>(fieldNames[i], cell))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }
}