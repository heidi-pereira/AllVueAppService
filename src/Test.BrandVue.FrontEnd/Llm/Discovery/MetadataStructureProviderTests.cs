using BrandVue.Services;
using BrandVue.Services.Llm.Discovery;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Test.BrandVue.FrontEnd.Llm.Discovery;

[TestFixture]
public class MetadataStructureProviderTests
{
    private readonly MetadataStructureProvider _metadataProvider;
    private readonly string _subsetId = "UK";

    public MetadataStructureProviderTests()
    {
        ISubsetRepository subsetRepository = Substitute.For<ISubsetRepository>();
        IMeasureRepository measureRepository = Substitute.For<IMeasureRepository>();
        IPageHierarchyGenerator pageHierarchyGenerator = Substitute.For<IPageHierarchyGenerator>();

        measureRepository.GetAllMeasuresIncludingDisabledForSubset(default).ReturnsForAnyArgs(MockMeasures);
        pageHierarchyGenerator.GetHierarchy(default).ReturnsForAnyArgs(MockPages);

        _metadataProvider = new MetadataStructureProvider(measureRepository, subsetRepository, pageHierarchyGenerator);
    }


    [Test]
    public void GetPages_ReturnsForRequestedMetricsOnly()
    {
        // Arrange
        string[] requestedMetrics = ["Awareness", "Affinity"];
        int expectedPageCount = 6;

        // Act
        var pages = _metadataProvider.GetPages(_subsetId, requestedMetrics);
        var actualPageCount = pages.Count();

        // Assert
        Assert.That(actualPageCount, Is.EqualTo(expectedPageCount));
    }

    [Test]
    public async Task GetPages_ReturnsFlattenedList()
    {
        // Arrange
        string[] requestedMetrics = ["Awareness", "Affinity"];

        // Act
        var pages = _metadataProvider.GetPages(_subsetId, requestedMetrics);

        // Assert
        Assert.That(pages.Any(a => a.Id == 1010));
        Assert.That(pages.Any(a => a.Id == 1011));

    }

    [Test]
    public async Task GetPagesAndReferencedMetrics_ReturnsForRequestedMetricsOnly()
    {
        // Arrange
        string[] requestedMetrics = ["Awareness", "Affinity"];
        int expectedPageCount = 6;

        // Act
        var pages = _metadataProvider.GetPagesAndReferencedMetrics(_subsetId, requestedMetrics);
        var actualPageCount = pages.Count();

        // Assert
        Assert.That(actualPageCount, Is.EqualTo(expectedPageCount));
    }

    [Test]
    public void GetPagesAndReferencedMetrics_IncludesRequestedMetricsInResults()
    {
        // Arrange
        string[] requestedMetrics = ["Awareness", "Affinity"];

        // Act
        var pages = _metadataProvider.GetPagesAndReferencedMetrics(_subsetId, requestedMetrics);

        // Assert
        Assert.That(pages.All(a => a.MetricNames.Any(b => requestedMetrics.Contains(b))));
    }


    [Test]
    public void GetMeasures_LiveOnly()
    {
        // Act
        var measures = _metadataProvider.GetMeasures(_subsetId);

        // Assert
        Assert.That(measures.All(a => !a.Disabled));
    }

    private static PageDescriptor[] MockPages => [
     new PageDescriptor
        {
            Id = 1009,
            Name = "Awareness",
            DisplayName = "Brand Health",
            PageTitle = "The percentage of all respondents who say they have either purchased from or are otherwise aware of a brand <br>Q: When, if ever, have you visited / ordered from the following…? (selecting any option other than 'I do not know this brand')",
            HelpText = "",
            ChildPages = new []
            {
                new PageDescriptor
                {
                    Id = 1010,
                    Name = "Awareness:Brand Awareness",
                    DisplayName = "Brand Health",
                    PageTitle = "The percentage of all respondents who say they have either purchased from or are otherwise aware of a brand <br>Q: When, if ever, have you visited / ordered from the following…? (selecting any option other than 'I do not know this brand')",
                    HelpText = ""
                },
                new PageDescriptor
                {
                    Id = 1011,
                    Name = "Awareness:Brand Awareness (stacked)",
                    DisplayName = "Brand Health",
                    PageTitle = "A breakdown of the awareness ratings for the brands (stated as percentages of all respondents) <br>Q: When, if ever, have you visited / ordered from the following…? (selecting any option other than 'I do not know this brand')",
                    HelpText = ""
                }
            }
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
            HelpText = "",
            ChildPages = new []
            {
                new PageDescriptor
                {
                    Id = 2199,
                    Name = "Brand Analysis:Brand Analysis 1",
                    DisplayName = "Brand Analysis 1",
                    PageTitle = "Brand Analysis 1",
                    HelpText = ""
                },
                new PageDescriptor
                {
                    Id = 2200,
                    Name = "Brand Analysis:Brand Analysis 2",
                    DisplayName = "Brand Analysis 2",
                    PageTitle = "Brand Analysis 2",
                    HelpText = ""
                }
            }
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
            },
            new Measure
            {
                Disabled = true,
                Name = "Disabled Measure",
                VarCode = "DisabledMeasure",
                DisplayName = "Disabled Measure",
                Description = "Disabled Measure"
            }
        };
}

