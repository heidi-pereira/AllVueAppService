using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BrandVue;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.Services.Llm;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.LlmInsights
{
    [Explicit("Directly call LLM and need human assessment of quality - not to be automated")]
    [TestFixture]
    public class BasicLlmInsightsTests
    {
        private readonly AzureChatCompletionService _azureChatCompletionService;
        private readonly IOptions<AzureAiClientSettings> _settings;

        public BasicLlmInsightsTests()
        {
            _settings = Options.Create(new AzureAiClientSettings() {
                MaxRetries = 0,
                Temperature = 0,
                DefaultTimeout = 30,
                Endpoint = "https://savanta-aiml-studio-eastus.openai.azure.com/",
                Key = "FOOBAR",
                Deployment = "Savanta-Aila-4o"});
            var httpClientFac = Substitute.For<IHttpClientFactory>();
            httpClientFac.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
            _azureChatCompletionService =
                new AzureChatCompletionService(_settings, httpClientFac);
        }

        
        [Test]
        //https://savanta.all-vue.com/retail/ui/demand-and-usage/penetration-l12m/over-time?Active=10&Average=Monthly&End=2024-06-30&EntitySetAverages=3924&Highlighted=10.138.196.49&Legend=24.56.92.112.121.124.143.160.185.Grocery+%28competitive+average%29&Period=Previous&Set=3924&SplitBy=brand&Start=2023-06-01
        [TestCase("MinorMovementAlmostRandomNoise",
            "The percentage of all respondents who have purchased products from a brand in the last month", 
            "Poundland", "%")]

        //https://thecardfactory.all-vue.com/retail/ui/customer-experience/nps/over-time?End=2024-06-30&EntitySetAverages=3575&Legend=28&Peer=26.27.28.34.63.120.134&Set=3575&SplitBy=brand&Start=2020-12-22&Subset=All
        //Human observation: Card Factory’s NPS has increased significantly this month, whilst Moonpig’s has fallen. This pushes Card Factory to the top of the competitor set again.
        [TestCase("SeveralYearObservation",
            "The level of advocacy for the retailers by those that have been customers in the past 3 months (stated as a Net Promoter Score - the 'percentage of promoters' minus the 'percentage of detractors')",
            "Card Factory", "NPS")]

        //https://savanta.all-vue.com/retail/ui/demand-and-usage/penetration-lm/over-time?Active=26&Average=Monthly&End=2024-06-30&EntitySetAverages=5323&Highlighted=148.161.185.26&Legend=24.56.112.121.124.143.160.Grocery+%28competitive+average%29&Period=Previous&Set=5323&SplitBy=brand&Start=2023-06-01
        [TestCase("NotablePeakForOneBrand",
            "The percentage of all respondents who have purchased products from a brand in the last month",
            "Card Factory", "%")]

        //https://savanta.all-vue.com/retail/ui/brand-health/loyalty-scheme-usage/over-time?Active=6&Average=Monthly&End=2024-06-30&EntitySetAverages=3837&Highlighted=104.118.161.6.90.95&Legend=24.56.112.121.124.143.160.185.Grocery+%28competitive+average%29&Period=Previous&Set=3837&SplitBy=brand&Start=2023-06-01
        [TestCase("SixBrandsNotableDecline",
            "Of the respondents who have been customers of each brand in the last 3 months, the percentage who say they have used each brands loyalty scheme",
            "Lidl", "%")]

        //https://savanta.all-vue.com/retail/ui/brand-attention/advertising-awareness/over-time?Average=Monthly&End=2024-06-30&EntitySetAverages=3835&Highlighted=164.201.4&Legend=24.56.92.112.121.124.143.160.185.Grocery+%28competitive+average%29&Period=Previous&Set=3835&SplitBy=brand&Start=2023-06-01
        [TestCase("SixBrandsWithDistinctLeader",
            "The percentage of all respondents that have seen advertising for a brand in the last month", 
            "Tesco", "%")]

        //https://savanta.all-vue.com/retail/ui/brand-attention/advertising-awareness/over-time?Average=Monthly&End=2024-06-30&Legend=24.56.92.112.121.124.143.160.185.Grocery+%28competitive+average%29&Period=Previous&Set=3835&Start=2023-06-01
        //could/should return nothing here
        [TestCase("TwoEntityResponseWithUniformNoise",
            "The percentage of all respondents that have seen advertising for a brand in the last month",
            "Cath Kidston", "%")]

        //https://savanta.all-vue.com/retail/ui/brand-attention/advertising-awareness/over-time?Average=Monthly&End=2024-06-30&EntitySetAverages=4499&Highlighted=115.133.140.155.172.173.175.178.179.181.187.199.46.73.89.98&Legend=24.56.92.112.121.124.143.160.185.Grocery+%28competitive+average%29&Period=Previous&Set=4499&SplitBy=brand&Start=2023-06-01
        //should probably mention the market leader, as well as focus brand
        [TestCase("ManyBrandsWithClearWinners",
            "The percentage of all respondents that have seen advertising for a brand in the last month", 
            "The Range", "%")]

        //https://savanta.all-vue.com/eatingout/ui/brand-attention/buzz/negative-buzz/over-time?Active=142&Average=Monthly&End=2018-12-01&EntitySetAverages=0&Highlighted=142&Period=Previous&Set=3470&SplitBy=brand&Start=2017-10-01
        //this should mention the KFC news, as it's big, even if mc'ds is the focus
        [TestCase("BrandWasInTheNews",
            "Those who have heard something negative about each brand in the last month (stated as a percentage of all respondents)",
            "McDonald's", "%")]
        public async Task CanMakeBasicRequest(string file, string question, string focusedBrand, string unit = "%")
        {
            string jsonContent = File.ReadAllText("LlmInsights/Resources/" + file + ".json");
            var weightedResults = JsonConvert.DeserializeObject<EntityWeightedDailyResults[]>(jsonContent);
            var result = new OverTimeResults
            {
                EntityWeightedDailyResults = weightedResults
            };
            var requestAdapter = Substitute.For<IRequestAdapter>();
            var focus = weightedResults.First(x => x.EntityInstance.Name == focusedBrand).EntityInstance.Id;
            var requestParam = new ResultsProviderParameters()
            {
                FocusEntityInstanceId = focus,
                RequestedInstances = new TargetInstances(TestEntityTypeRepository.Brand,
                    weightedResults.Select(x => x.EntityInstance).ToArray()),
                PrimaryMeasure = new Measure()
                {
                    Name = file,
                    Minimum = unit == "%" ? 0 : -10,
                    Maximum = unit == "%" ? 1 : 10,
                    NumberFormat = unit,
                    HelpText = question,
                }
            };
            requestAdapter
                .CreateParametersForCalculationWithAdditionalFilter(Arg.Any<MultiEntityRequestModel>())
                .Returns((arg) => requestParam);
            var insightsService = new LlmInsightsGeneratorService(_settings, _azureChatCompletionService, requestAdapter);

            var request = new MultiEntityRequestModel(file, "All", 
                new Period(), 
                new EntityInstanceRequest("", []), 
                [],
                new DemographicFilter(), 
                new CompositeFilterModel(),
                [],
                [],
                false,
                SigConfidenceLevel.NinetyFive,
                focus);
            
            var insights = (await insightsService.GetLlmInsightsFromResults(new OverTimeRequestData(result, request, []))).ToList();
            
            foreach (var insightResult in insights)
            {
                Console.WriteLine(JsonConvert.SerializeObject(insightResult));
            }
            Assert.That(insights, Is.Not.Empty);
        }
    }
}
