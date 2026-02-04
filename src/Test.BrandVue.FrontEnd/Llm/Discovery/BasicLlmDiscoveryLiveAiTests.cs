using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BrandVue;
using BrandVue.EntityFramework;
using BrandVue.Services;
using BrandVue.Services.Llm;
using BrandVue.Services.Llm.Discovery;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using Castle.Core.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.Llm.Discovery
{
    [Explicit("Directly call LLM and need human assessment of quality - not to be automated")]
    [TestFixture]
    public class BasicLlmDiscoveryLiveAiTests1
    {
        private readonly MetadataStructureProvider _metadataProvider;
        private readonly NavLinkParameterAdapter _outputAdapter;
        private readonly LlmDiscoveryService _discoveryService;
        private readonly string _subsetId = "UK";

        public BasicLlmDiscoveryLiveAiTests1()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<AppSettings>()
                .AddEnvironmentVariables()
                .Build();

            var azureAiClientSettings = new AzureAiClientSettings();
            configuration.GetSection("Azure:OpenAI").Bind(azureAiClientSettings);
            var settings = Options.Create(azureAiClientSettings);

            var httpClientFac = Substitute.For<IHttpClientFactory>();
            httpClientFac.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
            var azureChatCompletionService = new AzureChatCompletionService(settings, httpClientFac);

            ISubsetRepository _subsetRepository = Substitute.For<ISubsetRepository>();
            IMeasureRepository _measureRepository = Substitute.For<IMeasureRepository>();
            IPageHierarchyGenerator _pageHierarchyGenerator = Substitute.For<IPageHierarchyGenerator>();

            _measureRepository.GetAllMeasuresIncludingDisabledForSubset(default).ReturnsForAnyArgs(MockMeasures);
            _pageHierarchyGenerator.GetHierarchy(default).ReturnsForAnyArgs(MockPages);

            _metadataProvider = new MetadataStructureProvider(_measureRepository, _subsetRepository, _pageHierarchyGenerator);
            _outputAdapter = new NavLinkParameterAdapter();
            _discoveryService = new LlmDiscoveryService(settings, azureChatCompletionService, _metadataProvider);
        }

        [Test]
        [TestCase("I want to know which users are aware of my brand and how it has changed in the past year", "Awareness")]
        [TestCase("I'm unsure, I just want to know if my marketing is having some effect", "Analysis")]
        [TestCase("Show me brand affinity", "Brand Affinity")]
        public async Task CanMakeBasicRequest(string userRequest, string expectedPageName)
        {
            var results = await _discoveryService.GetNavigationSuggestions(userRequest, _subsetId,_outputAdapter, CancellationToken.None);
            Assert.That(results.Any(x => x.PageName.Contains(expectedPageName, StringComparison.InvariantCultureIgnoreCase)));
        }

        [Test]
        [TestCase("How can I see where my brand performs for loyalty?", "Marketing-Performance", "Brand-Health-Scorecard", "Customer-Experience-Scorecard")]
        public async Task RequestReturnsAtLeastOneRelevantPage(string userRequest, params string[] expectedPageNames)
        {
            var results = await _discoveryService.GetNavigationSuggestions(userRequest, _subsetId, _outputAdapter, CancellationToken.None);
            Assert.That(results.Any(result => expectedPageNames.Any(expectedPageName => result.PageName.Contains(expectedPageName, StringComparison.InvariantCultureIgnoreCase))));
        }

        [Test]
        [TestCase("Show me awareness for March 2024", "2024-03-01", "2024-03-31")]
        public async Task RequestReturnsRelevantTimeframeParameters(string userRequest, string expectedStartDate, string expectedEndDate)
        {
            var results = await _discoveryService.GetNavigationSuggestions(userRequest, _subsetId, _outputAdapter, CancellationToken.None);
            var queryParams = results.First().QueryParams;
            Assert.That(queryParams.Start != null && queryParams.Start.Contains(expectedStartDate));
            Assert.That(queryParams.End != null && queryParams.End.Contains(expectedEndDate));
        }

        [Test]
        public async Task ReturnsTimeframeParamsForRelativeTimeRequest()
        {
            var today = DateTime.Today;
            var firstDayOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);
            var firstDayOfLastMonth = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, 1);

            var expectedStartDate = firstDayOfLastMonth.ToString("yyyy-MM-dd");
            var expectedEndDate = lastDayOfLastMonth.ToString("yyyy-MM-dd");

            await RequestReturnsRelevantTimeframeParameters("Show me my brand's consideration for last month", expectedStartDate, expectedEndDate);
        }

        [Test]
        [TestCase("Give me a list of all users")]
        [TestCase("DROP TABLE Users;")]
        [TestCase("Which of my metrics have moved most this month?")]
        [TestCase("asdkjfhaskjdfhaskjdfh")]
        [TestCase("Show me the performance of a non-existent metric")]
        public async Task RequestReturnsEmptyInAppropriateSituations(string userRequest)
        {
            var results = await _discoveryService.GetNavigationSuggestions(userRequest, _subsetId, _outputAdapter, CancellationToken.None);
            Assert.That(results.IsNullOrEmpty());
        }

        private static PageDescriptor[] MockPages => [
     new PageDescriptor
        {
            Id = 1009,
            Name = "Awareness",
            DisplayName = "Brand Health",
            PageTitle = "The percentage of all respondents who say they have either purchased from or are otherwise aware of a brand <br>Q: When, if ever, have you visited / ordered from the following…? (selecting any option other than 'I do not know this brand')",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 394,
            Name = "Brand Affinity",
            DisplayName = "Brand Health",
            PageTitle = "The percentage of people stating that they 'like' or 'love' a brand (stated as a percentage of all respondents who are both aware of the brand and have an opinion of the brand) <br>Q: How would you describe your opinion of the following brands?",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 396,
            Name = "Brand Affinity Love",
            DisplayName = "Brand Health",
            PageTitle = "The percentage of people stating that they 'love' a brand (stated as a percentage of all respondents who are both aware of the brand and have an opinion of the brand) <br>Q: How would you describe your opinion of the following brands?",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 395,
            Name = "Brand Affinity:Brand Affinity (stacked)",
            DisplayName = "Brand Affinity (stacked)",
            PageTitle = "A breakdown of the affinity ratings for the brands (stated as percentages of all respondents who are both aware of the brand and have an opinion of the brand) <br>Q: How would you describe your opinion of the following brands?",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 2198,
            Name = "Brand Analysis",
            DisplayName = "Brand Analysis",
            PageTitle = "Brand Analysis",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 390,
            Name = "Brand Health",
            DisplayName = "Brand Health",
            PageTitle = "",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 391,
            Name = "Brand Health - Scorecard",
            DisplayName = "Brand Health - Scorecard",
            PageTitle = "Performance across the factors that capture the knowledge and preference for each brand that can lead to a visit and purchase",
            HelpText = ""
        },
        new PageDescriptor
        {
            Id = 366,
            Name = "Brand-Performance",
            DisplayName = "Brand Performance",
            PageTitle = "",
            HelpText = "Brand performance for {{instance}}"
        }];
        private static IEnumerable<Measure> MockMeasures => new List<Measure>
        {
            new Measure
            {
                Name = "Advertising awareness",
                VarCode = "Advertising awareness",
                DisplayName = "Advertising awareness",
                Description = "The percentage of respondents who say they have seen advertising for each brand in the last month (stated as a percentage of all respondents) Q: Have you seen advertising for any of the following brands in the last month?"
            },
            new Measure
            {
                Name = "Affinity",
                VarCode = "Affinity",
                DisplayName = "Affinity",
                Description = "The percentage of people stating that they 'like' or 'love' a brand (stated as a percentage of all respondents who are both aware of the brand and have an opinion of the brand) Q: How would you describe your opinion of the following brands?"
            },
            new Measure
            {
                Name = "Awareness",
                VarCode = "Awareness",
                DisplayName = "Awareness",
                Description = "The percentage of all respondents who say they have either purchased from or are otherwise aware of a brand <br>Q: When, if ever, have you visited / ordered from the following…? (selecting any option other than 'I do not know this brand')"
            },
            new Measure
            {
                Name = "Brand Affinity",
                VarCode = "Brand Affinity",
                DisplayName = "Brand Affinity",
                Description = "The percentage of people stating that they 'like' or 'love' a brand (stated as a percentage of all respondents who are both aware of the brand and have an opinion of the brand) <br>Q: How would you describe your opinion of the following brands?"
            },
            new Measure
            {
                Name = "Brand Love",
                VarCode = "Brand Love",
                DisplayName = "Brand Love",
                Description = "The percentage of people stating that they 'love' a brand (stated as a percentage of all respondents who are both aware of the brand and have an opinion of the brand) Q: How would you describe your opinion of the following brands?"
            },
            new Measure
            {
                Name = "Buzz noise",
                VarCode = "Buzz noise",
                DisplayName = "Buzz noise",
                Description = "The sum total of positive and negative buzz"
            },
            new Measure
            {
                Name = "Image",
                VarCode = "Image",
                DisplayName = "Image",
                Description = "Image characteristics most associated with each brand (stated as a percentage of all respondents asked) Q: Which of these words / statements do you most associate with [BRAND]?"
            }
        };
    }
}
