using BrandVue.EntityFramework.MetaData.Weightings;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
namespace TestCommon.Weighting
{
    public static class WeightingPlanConfigurationsTestObjects
    {
        private static IEnumerable<WeightingPlanConfiguration> Deserialize(string text, string productShortCode, string subProductId, string subsetId )
        {
            var results =  JsonConvert.DeserializeObject<IEnumerable<WeightingPlanConfiguration>>(text);
            Fixup(results, null, productShortCode, subProductId, subsetId);
            return results;
        }
        private static void Fixup(IEnumerable<WeightingPlanConfiguration> plans, WeightingTargetConfiguration parent, string productShortCode, string subProductId, string subsetId)
        {
            if (plans != null)
            {
                foreach (var plan in plans)
                {
                    plan.Id = 0;
                    plan.ProductShortCode = productShortCode;
                    plan.ParentTarget = parent;
                    plan.ParentWeightingTargetId = parent == null ? null : 0;
                    plan.SubProductId = subProductId;
                    plan.SubsetId = subsetId;
                    Fixup(plan.ChildTargets, plan, productShortCode, subProductId, subsetId);
                }
            }
        }
        private static void Fixup(List<WeightingTargetConfiguration> targets, WeightingPlanConfiguration parent, string productShortCode, string subProductId, string subsetId)
        {
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    target.Id = 0;
                    target.ParentWeightingPlan = parent;
                    target.ParentWeightingPlanId = 0;
                    target.ProductShortCode = productShortCode;
                    target.SubProductId = subProductId;
                    target.SubsetId = subsetId;
                    Fixup(target.ChildPlans, target, productShortCode, subProductId, subsetId);
                }
            }
        }

        public static IEnumerable<WeightingPlanConfiguration> GetSimplePercentageWeightingPlan(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.50,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.50,
            },
			{
                ""EntityInstanceId"": 2,
            },
			{
                ""EntityInstanceId"": 3,
            }
		],
    }
]", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> GetSimplePercentageWeightingPlanNonBalanced(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.60,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.40,
            },
			{
                ""EntityInstanceId"": 2,
            },
			{
                ""EntityInstanceId"": 3,
            }
		],
    }
]", productShortCode, subProductId, subsetId);
        }
        public static IEnumerable<WeightingPlanConfiguration> GetSimpleRimWeightingPlan(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.65,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.34,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.01,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.0,
            }
		],
    },
	{
        ""VariableIdentifier"": ""Household composition_2"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""Target"": 0.6,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.2,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.14,
            },
			{
                ""EntityInstanceId"": 4,
				""Target"": 0.06,
            }
		]
    }
]", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> GetSingleWeightingSchemeWithMissingTargetsStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.65,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.34,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.01,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.0,
            }
		],
    },
	{
        ""VariableIdentifier"": ""Household composition_2"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""Target"": 0.6,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.4,
            }
		]
    }
]", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> FilteredRimOnlyStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId", string filterMetricName = "DecemberWeeksWaveVariable")
        {
            return Deserialize($@"
[
	{{
		""VariableIdentifier"": ""{filterMetricName}"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {{
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {{
                    ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 0,
								""Target"": 0.8,
                            }},
							{{
                                ""EntityInstanceId"": 1,
								""Target"": 0.19,
                            }},
							{{
                                ""EntityInstanceId"": 2,
								""Target"": 0.01,
                            }},
							{{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }}
						]
                    }},
					{{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 1,
								""Target"": 0.5,
							}},
							{{
                                ""EntityInstanceId"": 2,
								""Target"": 0.2,
                            }},
							{{
                                ""EntityInstanceId"": 3,
								""Target"": 0.24,
		                    }},
							{{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }}
						]
                    }}
				]
            }},
			{{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {{
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 0,
								""Target"": 0.85,
                            }},
							{{
                                ""EntityInstanceId"": 1,
								""Target"": 0.13,
                            }},
							{{
                                ""EntityInstanceId"": 2,
								""Target"": 0.02,
		                    }},
							{{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }}
						]
                    }},
					{{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 1,
								""Target"": 0.3,
                            }},
							{{
                                ""EntityInstanceId"": 2,
								""Target"": 0.6,
                            }},
							{{
                                ""EntityInstanceId"": 3,
								""Target"": 0.04,
                            }},
							{{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }}
						],
                    }}
				],
            }}
		],
    }}
]
", productShortCode, subProductId, subsetId);
        }

        /// <summary>
        /// Intentionally excludes some targets. Respondents should be placed in unweighted cell.
        /// </summary>
        public static IEnumerable<WeightingPlanConfiguration> GetSingleTargetWeightingSchemeStrategyMissingTargets(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") 
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.1,
						    },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.1,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.1,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.05,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 3,
				""ChildPlans"": [
                    {
                    ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.05,
                            }
						],
                    }
				]
            }
		]
    }
]
", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> GetWeightingPlanWithChildPlanMarkedAsWeightingGroupRoot(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId")
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": true,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": true,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.1,
						    },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.1,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.1,
                            }
						]
                    }
				]
            }
		]
    }
]
", productShortCode, subProductId, subsetId);
        }

        /// <summary>
        /// The numbers for GetSingleWeightingSchemeStrategy have been checked against another source. This strategy encodes the target weightings generated from that strategy so should have the same results
        /// </summary>
        public static IEnumerable<WeightingPlanConfiguration> GetInvertedSingleTargetWeightingSchemeStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId")
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""Household composition_2"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.38291343,
						    },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.21070205,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.006384617,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.13435165,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.06390823,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.001740056,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 3,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
							{
                                ""EntityInstanceId"": 0,
								""Target"": 0.09080525,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.047923415,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.001271315,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 4,
				""ChildPlans"": [
                    {
                    ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.041929672,
                            },
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.01746631,
                            },
                            {
                                ""EntityInstanceId"": 2,
								""Target"": 0.000604006,
                            },
                            {
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						],
                    }
				]
            }
		]
    }
]
", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> NonFilteredTargetOnlyStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId")
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.38291343,
						    },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.13435165,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.09080525
,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.041929672
,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.21070205
,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.06390823
,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.047923415
,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.01746631
,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.006384617
,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.001740056
,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.001271315
,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.000604006
,
                            }
						]
                    }
				]
            },
			{
                ""EntityInstanceId"": 3,
				""ChildPlans"": [
                    {
                    ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.0,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.0,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.0,
                            }
						],
                    }
				]
            }
		]
    }
]
", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> WeightedFilteredTargetOnlyStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId")
        {
            return BalanceTheTargetsAtTopLevel(FilteredTargetOnlyStrategy(productShortCode, subProductId, subsetId).ToList());
        }

        public static IEnumerable<WeightingPlanConfiguration> FilteredTargetOnlyStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") 
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""DecemberWeeksWaveVariable"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {
                    ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                        ""EntityInstanceId"": 0,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                             ""EntityInstanceId"": 1,
												""Target"": 0.38291344,
                                            },
											{
                                                 ""EntityInstanceId"": 2,
												""Target"": 0.1343517,
                                            },
											{
                                                 ""EntityInstanceId"": 3,
												""Target"": 0.09080526,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.041929673,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 1,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.21070206,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.06390825,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.047923416,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.01746632,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 2,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.0063846186,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.0017400575,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.0012713169,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.0006040074,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 3,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.0,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.0,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.0,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.0,
                                            }
										],
                                    }
								],
                            }
						],
                    }
				],
            },
			{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.1,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 1,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.1,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 2,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 3,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.05,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 3,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.05,
                                            }
										],
                                    }
								],
                            }
						],
                    }
				],
            }
		],
    }
]
", productShortCode, subProductId, subsetId);
        }


        public static IEnumerable<WeightingPlanConfiguration> WeightedFilteredTargetOnlyStrategyWithNegative1(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId")
        {
            return BalanceTheTargetsAtTopLevel(FilteredTargetOnlyStrategyWithNegative1(productShortCode, subProductId, subsetId).ToList());
        }

        private static IEnumerable<WeightingPlanConfiguration> BalanceTheTargetsAtTopLevel(List<WeightingPlanConfiguration> result)
        {
            var childTargets = result.FirstOrDefault().ChildTargets;
            var count = childTargets.Count;
            foreach (var childTarget in childTargets)
            {
                childTarget.Target = 1.0M / count;
            }
            return result;
        }

        public static IEnumerable<WeightingPlanConfiguration> FilteredTargetOnlyStrategyWithNegative1(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId")
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""DecemberWeeksWaveVariable"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {
                    ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                        ""EntityInstanceId"": 0,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                             ""EntityInstanceId"": 1,
												""Target"": 0.38291344,
                                            },
											{
                                                 ""EntityInstanceId"": 2,
												""Target"": 0.1343517,
                                            },
											{
                                                 ""EntityInstanceId"": 3,
												""Target"": 0.09080526,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.041929673,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 1,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.21070206,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.06390825,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.047923416,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.01746632,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 2,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.0063846186,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.0017400575,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.0012713169,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.0006040074,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 3,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.0,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.0,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.0,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.0,
                                            }
										],
                                    }
								],
                            }
						],
                    }
				],
            },
			{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.2,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": -1,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 1,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 2,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 3,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.1,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 2,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 3,
												""Target"": 0.1,
                                            },
											{
                                                ""EntityInstanceId"": 4,
												""Target"": 0.05,
                                            }
										],
                                    }
								],
                            },
							{
                                ""EntityInstanceId"": 3,
								""ChildPlans"": [
                                    {
                                        ""VariableIdentifier"": ""Household composition_2"",
										""IsWeightingGroupRoot"": false,
										""ChildTargets"": [
                                            {
                                                ""EntityInstanceId"": 1,
												""Target"": 0.05,
                                            }
										],
                                    }
								],
                            }
						],
                    }
				],
            }
		],
    }
]
", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> NonFilteredRimToTargetStyleCharacterization(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingSchemeStrategy()
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""Gender"",
        ""IsWeightingGroupRoot"": false,
        ""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.65,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.34,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.01,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.0,
            }
		],
    },
	{
		""VariableIdentifier"": ""Household composition_2"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
			{
				""EntityInstanceId"": 1,
				""Target"": 0.6,
            },
			{
				""EntityInstanceId"": 2,
				""Target"": 0.2,
			},
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.14,
            },
			{
                ""EntityInstanceId"": 4,
				""Target"": 0.06,
            }
		],
	}
]
", productShortCode, subProductId, subsetId);
        }
        public static IEnumerable<WeightingPlanConfiguration> FiveWeightingPlans(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetFiveWeightingSchemesStrategy()
        {
            return Deserialize(@"
[
	{
		""VariableIdentifier"": ""DecemberWeeksWaveVariable"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.8,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.19,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.01,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						],
                    },
					{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.5,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.2,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.24,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }
						],
                    }
				],
            },
			{
                ""EntityInstanceId"": 2,
				""ChildPlans"": [
                    {
                    ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.85,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.13,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.02,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						],
                    },
					{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.3,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.6,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.04,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }
						],
                    }
				],
            },
			{
                ""EntityInstanceId"": 3,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.85,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.13,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.02,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						],
                    },
					{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.3,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.6,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.04,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }
						],
                    }
				],
            },
			{
                ""EntityInstanceId"": 4,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.85,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.13,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.02,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						],
                    },
					{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.3,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.6,
                            },
							{
                        ""EntityInstanceId"": 3,
								""Target"": 0.04,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }
						],
                    }
				],
            },
			{
                ""EntityInstanceId"": 5,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
								""Target"": 0.85,
                            },
							{
                                ""EntityInstanceId"": 1,
								""Target"": 0.13,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.02,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.0,
                            }
						],
                    },
					{
                        ""VariableIdentifier"": ""Household composition_2"",
						""IsWeightingGroupRoot"": false,
						""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
								""Target"": 0.3,
                            },
							{
                                ""EntityInstanceId"": 2,
								""Target"": 0.6,
                            },
							{
                                ""EntityInstanceId"": 3,
								""Target"": 0.04,
                            },
							{
                                ""EntityInstanceId"": 4,
								""Target"": 0.06,
                            }
						],
                    }
				],
            }
		],
    }
]
", productShortCode, subProductId, subsetId);
        }


        public static IEnumerable<WeightingPlanConfiguration> GetVerySimpleRimWeightingPlan(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.495,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.495,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.01,
            }
		],
    },
	{
        ""VariableIdentifier"": ""Age"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""Target"": 0.5,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.3,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.2,
            }
		]
    }
]", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> GetSingleInvertedWeightingSchemeStrategy(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""Household composition_2"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 1,
				""Target"": 0.6,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.2,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.14,
            },
			{
                ""EntityInstanceId"": 4,
				""Target"": 0.06,
            }
		],
    },
	{
        ""VariableIdentifier"": ""Gender"",
		""IsWeightingGroupRoot"": false,
		""ChildTargets"": [
            {
                ""EntityInstanceId"": 0,
				""Target"": 0.65,
            },
			{
                ""EntityInstanceId"": 1,
				""Target"": 0.34,
            },
			{
                ""EntityInstanceId"": 2,
				""Target"": 0.01,
            },
			{
                ""EntityInstanceId"": 3,
				""Target"": 0.0,
            }
		]
    }
]", productShortCode, subProductId, subsetId);
        }



        public static IEnumerable<WeightingPlanConfiguration> GetVerySimpleRimWeightingPlanWithCountry(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""COUNTRY"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {
                ""EntityInstanceId"": 100,
				""ChildPlans"": [
                    {
                            ""VariableIdentifier"": ""Gender"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {
                                    ""EntityInstanceId"": 0,
				                    ""Target"": 0.495,
                                },
			                    {
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.495,
                                },
			                    {
                                    ""EntityInstanceId"": 2,
				                    ""Target"": 0.01,
                                }
		                    ],
                        },
	                    {
                            ""VariableIdentifier"": ""Age"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.5,
                                },
			                    {
                                    ""EntityInstanceId"": 2,
				                    ""Target"": 0.3,
                                },
			                    {
                                    ""EntityInstanceId"": 3,
				                    ""Target"": 0.2,
                                }
		                    ]
                        }                ],
		    },
            {
                ""EntityInstanceId"": 200,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
				                ""Target"": 0.495,
                            },
			                {
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.495,
                            },
			                {
                                ""EntityInstanceId"": 2,
				                ""Target"": 0.01,
                            }
		                ],
                    },
	                {
                        ""VariableIdentifier"": ""Age"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.5,
                            },
			                {
                                ""EntityInstanceId"": 2,
				                ""Target"": 0.3,
                            },
			                {
                                ""EntityInstanceId"": 3,
				                ""Target"": 0.2,
                            }
		                ]
                    }
                ],
		    },
        ],
    }
]", productShortCode, subProductId, subsetId);
        }


        public static IEnumerable<WeightingPlanConfiguration> GetVerySimpleRimWeightingPlanWithCountryAndInvalidTargets(string productShortCode = "ProductShortCode", string subProductId = "SubProductId", string subsetId = "SubsetId") //Was GetSingleWeightingStratagey()
        {
            return Deserialize(@"
[
    {
        ""VariableIdentifier"": ""COUNTRY"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {
                ""EntityInstanceId"": 100,
				""ChildPlans"": [
                    {
                            ""VariableIdentifier"": ""Gender"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {
                                    ""EntityInstanceId"": 0,
				                    ""Target"": 0.4,
                                },
			                    {
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.4,
                                },
			                    {
                                    ""EntityInstanceId"": 2,
				                    ""Target"": 0.01,
                                }
		                    ],
                        },
	                    {
                            ""VariableIdentifier"": ""Age"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.5,
                                },
			                    {
                                    ""EntityInstanceId"": 2,
				                    ""Target"": 0.3,
                                },
			                    {
                                    ""EntityInstanceId"": 3,
				                    ""Target"": 0.2,
                                }
		                    ]
                        }                ],
		    },
            {
                ""EntityInstanceId"": 200,
				""ChildPlans"": [
                    {
                        ""VariableIdentifier"": ""Gender"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 0,
				                ""Target"": 0.495,
                            },
			                {
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.495,
                            },
			                {
                                ""EntityInstanceId"": 2,
				                ""Target"": 0.01,
                            }
		                ],
                    },
	                {
                        ""VariableIdentifier"": ""Age"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.5,
                            },
			                {
                                ""EntityInstanceId"": 2,
				                ""Target"": 0.3,
                            },
			                {
                                ""EntityInstanceId"": 3,
				                ""Target"": 0.2,
                            }
		                ]
                    }
                ],
		    },
        ],
    }
]", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> GetRimWeightingWithNullGendersPlanWithCountry(double? target1=0.5, double? target2=0.5)
        {
            string productShortCode = "ProductShortCode";
            string subProductId = "SubProductId"; 
            string subsetId = "SubsetId";


            return Deserialize($@"
[
    {{
        ""VariableIdentifier"": ""COUNTRY"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {{
                {(target1.HasValue ? "\"Target\":" + target1.Value + "," : "")}
                ""EntityInstanceId"": 100,
				""ChildPlans"": [
                    {{
                            ""VariableIdentifier"": ""Gender"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {{
                                    ""EntityInstanceId"": 0,
				                    ""Target"": 0.6,
                                }},
			                    {{
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.4,
                                }},
			                    {{
                                    ""EntityInstanceId"": 2,
				                    ""Target"": null,
                                }}
		                    ],
                        }},
	                    {{
                            ""VariableIdentifier"": ""Age"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {{
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.5,
                                }},
			                    {{
                                    ""EntityInstanceId"": 2,
				                    ""Target"": 0.3,
                                }},
			                    {{
                                    ""EntityInstanceId"": 3,
				                    ""Target"": 0.2,
                                }}
		                    ]
                        }}
                    ],
		    }},
            {{
                {(target2.HasValue ? "\"Target\":"+target2.Value+"," : "")}
                ""EntityInstanceId"": 200,
				""ChildPlans"": [
                    {{
                        ""VariableIdentifier"": ""Gender"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 0,
				                ""Target"": 0.6,
                            }},
			                {{
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.4,
                            }},
			                {{
                                ""EntityInstanceId"": 2,
				                ""Target"": null,
                            }}
		                ],
                    }},
	                {{
                        ""VariableIdentifier"": ""Age"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.5,
                            }},
			                {{
                                ""EntityInstanceId"": 2,
				                ""Target"": 0.3,
                            }},
			                {{
                                ""EntityInstanceId"": 3,
				                ""Target"": 0.2,
                            }}
		                ]
                    }}
                ],
		    }},
        ],
    }}
]", productShortCode, subProductId, subsetId);
        }

        public static IEnumerable<WeightingPlanConfiguration> GetRimWeightingWithNullGendersPlanWithCountryTargetPopulation()
        {
            string productShortCode = "ProductShortCode";
            string subProductId = "SubProductId";
            string subsetId = "SubsetId";


            return Deserialize($@"
[
    {{
        ""VariableIdentifier"": ""COUNTRY"",
        ""IsWeightingGroupRoot"": true,
        ""ChildTargets"": [
            {{
                ""TargetPopulation"": 10023,
                ""EntityInstanceId"": 100,
				""ChildPlans"": [
                    {{
                            ""VariableIdentifier"": ""Gender"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {{
                                    ""EntityInstanceId"": 0,
				                    ""Target"": 0.6,
                                }},
			                    {{
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.4,
                                }},
			                    {{
                                    ""EntityInstanceId"": 2,
				                    ""Target"": null,
                                }}
		                    ],
                        }},
	                    {{
                            ""VariableIdentifier"": ""Age"",
		                    ""IsWeightingGroupRoot"": false,
		                    ""ChildTargets"": [
                                {{
                                    ""EntityInstanceId"": 1,
				                    ""Target"": 0.5,
                                }},
			                    {{
                                    ""EntityInstanceId"": 2,
				                    ""Target"": 0.3,
                                }},
			                    {{
                                    ""EntityInstanceId"": 3,
				                    ""Target"": 0.2,
                                }}
		                    ]
                        }}
                    ],
		    }},
            {{
                ""TargetPopulation"": 15037,
                ""EntityInstanceId"": 200,
				""ChildPlans"": [
                    {{
                        ""VariableIdentifier"": ""Gender"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 0,
				                ""Target"": 0.6,
                            }},
			                {{
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.4,
                            }},
			                {{
                                ""EntityInstanceId"": 2,
				                ""Target"": null,
                            }}
		                ],
                    }},
	                {{
                        ""VariableIdentifier"": ""Age"",
		                ""IsWeightingGroupRoot"": false,
		                ""ChildTargets"": [
                            {{
                                ""EntityInstanceId"": 1,
				                ""Target"": 0.5,
                            }},
			                {{
                                ""EntityInstanceId"": 2,
				                ""Target"": 0.3,
                            }},
			                {{
                                ""EntityInstanceId"": 3,
				                ""Target"": 0.2,
                            }}
		                ]
                    }}
                ],
		    }},
        ],
    }}
]", productShortCode, subProductId, subsetId);
        }
    }
}