using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using Newtonsoft.Json;

namespace TestCommon.Weighting
{
    public static class WeightingTestObjects
    {

        public static GroupedVariableDefinition GetGroupedVariableDefinition()
        {
            return JsonConvert.DeserializeObject<GroupedVariableDefinition>(@"
            {
	            ""ToEntityTypeName"": ""DecemberWeeksWaveVariable"",
	            ""ToEntityTypeDisplayNamePlural"": ""DecemberWeeksWaveVariable"",
	            ""Groups"": [
		            {
			            ""ToEntityInstanceName"": ""Week1"",
			            ""ToEntityInstanceId"": 1,
			            ""Component"": {
				            ""MinDate"": ""2020-12-01T00:00:00Z"",
				            ""MaxDate"": ""2020-12-06T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Week2"",
			            ""ToEntityInstanceId"": 2,
			            ""Component"": {
				            ""MinDate"": ""2020-12-07T00:00:00Z"",
				            ""MaxDate"": ""2020-12-13T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Week3"",
			            ""ToEntityInstanceId"": 3,
			            ""Component"": {
				            ""MinDate"": ""2020-12-14T00:00:00Z"",
				            ""MaxDate"": ""2020-12-20T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Week4"",
			            ""ToEntityInstanceId"": 4,
			            ""Component"": {
				            ""MinDate"": ""2020-12-21T00:00:00Z"",
				            ""MaxDate"": ""2020-12-27T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Week5"",
			            ""ToEntityInstanceId"": 5,
			            ""Component"": {
				            ""MinDate"": ""2020-12-28T00:00:00Z"",
				            ""MaxDate"": ""2020-12-31T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            }
	            ],
	            ""discriminator"": ""GroupedVariableDefinition""
            }
            ");
        }

        public static GroupedVariableDefinition GetPartialOverlappingGroupedVariableDefinition()
        {
            return JsonConvert.DeserializeObject<GroupedVariableDefinition>(@"
            {
	            ""ToEntityTypeName"": ""OverlappingWaveVariable"",
	            ""ToEntityTypeDisplayNamePlural"": ""OverlappingWaveVariable"",
	            ""Groups"": [
		            {
			            ""ToEntityInstanceName"": ""Period1"",
			            ""ToEntityInstanceId"": 1,
			            ""Component"": {
				            ""MinDate"": ""2020-12-01T00:00:00Z"",
				            ""MaxDate"": ""2020-12-06T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Period2"",
			            ""ToEntityInstanceId"": 2,
			            ""Component"": {
				            ""MinDate"": ""2020-12-07T00:00:00Z"",
				            ""MaxDate"": ""2020-12-13T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Period3"",
			            ""ToEntityInstanceId"": 3,
			            ""Component"": {
				            ""MinDate"": ""2020-12-12T00:00:00Z"",
				            ""MaxDate"": ""2020-12-20T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            }
	            ],
	            ""discriminator"": ""GroupedVariableDefinition""
            }
            ");
        }

        public static GroupedVariableDefinition GetDuplicateGroupedVariableDefinition()
        {
            return JsonConvert.DeserializeObject<GroupedVariableDefinition>(@"
            {
	            ""ToEntityTypeName"": ""DuplicateWaveVariable"",
	            ""ToEntityTypeDisplayNamePlural"": ""DuplicateWaveVariable"",
	            ""Groups"": [
		            {
			            ""ToEntityInstanceName"": ""Period1"",
			            ""ToEntityInstanceId"": 1,
			            ""Component"": {
				            ""MinDate"": ""2020-12-01T00:00:00Z"",
				            ""MaxDate"": ""2020-12-06T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Period2"",
			            ""ToEntityInstanceId"": 2,
			            ""Component"": {
				            ""MinDate"": ""2020-12-07T00:00:00Z"",
				            ""MaxDate"": ""2020-12-13T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            },
		            {
			            ""ToEntityInstanceName"": ""Period3"",
			            ""ToEntityInstanceId"": 3,
			            ""Component"": {
				            ""MinDate"": ""2020-12-07T00:00:00Z"",
				            ""MaxDate"": ""2020-12-13T23:59:59.999Z"",
				            ""discriminator"": ""DateRangeVariableComponent""
			            }
		            }
	            ],
	            ""discriminator"": ""GroupedVariableDefinition""
            }
            ");
        }
    }
}
