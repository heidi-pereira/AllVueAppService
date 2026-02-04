using BrandVue.EntityFramework;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.Models;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData;
using Vue.Common.Constants.Constants;

namespace BrandVue.Services
{
    public class PageHierarchyGenerator : IPageHierarchyGenerator
    {
        public static string[] PROTECTED_PAGE_NAMES => new[] { Appendix, Crosstabbing, ReportsPageName, SettingsPageName };

        private readonly IPagesRepository _pagesRepo;
        private readonly IPanesRepository _panesRepo;
        private readonly IPartsRepository _partsRepo;
        private readonly ISavedReportRepository _reportRepository;
        private readonly IUserContext _userContext;
        private readonly IMeasureRepository _measureRepository;
        private readonly InitialWebAppConfig _initialWebAppConfig;
        private readonly IProductContext _productContext;
        private readonly IAllVueConfigurationRepository _allVueConfigurationRepository;
        private const string Appendix = "Appendix";
        private const string Crosstabbing = "Crosstabbing";
        private const string ReportsPageName = "Reports";
        private const string SettingsPageName = "Settings";
        private const string ConfigurationPageName = "Configuration";
        private const string UsersPageName = "Users";
        private const string WeightingPageName = "Weighting";
        private const string ExportsPageName = "Exports";
        private const string SurveyConfigurationPageName = "SurveyConfiguration";
        private const string FeaturesPageName = "Features";

        private const string CrossTabPaneType = "CrosstabPage";
        private const string ReportsTabPaneType = "ReportsPage";
        private const string SettingsTabPaneType = "SettingsPage";
        private const string CrosstabPageDisplayName = "Explore data";
        private const string ReportVueTabPaneType = "ReportVuePage";
        private const string AllVueWebPagePaneType = "WebPage";

        //Used in layout.less & surveyVueEntry.less to override top-level grid layout
        private const string AllVueLayoutType = "allvuelayout";
        private static readonly ViewTypeEnum[] AllSimplePanes = { ViewTypeEnum.OverTime, ViewTypeEnum.Competition, ViewTypeEnum.Profile, ViewTypeEnum.ProfileOverTime, ViewTypeEnum.RankingTable};
        private static readonly ViewTypeEnum[] AllSimplePanesExceptProfileOverTime = { ViewTypeEnum.OverTime, ViewTypeEnum.Competition, ViewTypeEnum.Profile, ViewTypeEnum.RankingTable };
        private static readonly ViewTypeEnum[] OverTimeAndCompetitionPanesOnly = { ViewTypeEnum.OverTime, ViewTypeEnum.Competition };

        public PageHierarchyGenerator(
            IPagesRepository pages,
            IPanesRepository panes,
            IPartsRepository parts,
            ISavedReportRepository reportRepository,
            IUserContext userContext,
            IMeasureRepository measureRepository,
            InitialWebAppConfig initialWebAppConfig,
            IProductContext productContext,
            IAllVueConfigurationRepository allVueConfigurationRepository)
        {
            _pagesRepo = pages;
            _panesRepo = panes;
            _partsRepo = parts;
            _reportRepository = reportRepository;
            _userContext = userContext;
            _measureRepository = measureRepository;
            _initialWebAppConfig = initialWebAppConfig;
            _productContext = productContext;
            _allVueConfigurationRepository = allVueConfigurationRepository;
        }

        public PageDescriptor[] GetHierarchy(params Subset[] selectedSubsets)
        {
            var pageHierarchy = GetPagePaneAndPartHierarchy(selectedSubsets);
            var usedMeasureNames = GetUsedMeasureNamesForPageHierarchy(pageHierarchy);

            foreach (var measure in GetMeasuresToGeneratePanesFor(selectedSubsets, usedMeasureNames))
            {
                var targetPage = GetOrCreateTargetPageForMeasure(selectedSubsets, pageHierarchy, measure);

                var panesToGenerateForMeasure = measure switch
                {
                    _ when EntityCombinationBrandOnly(measure) => AllSimplePanes,
                    _ when NonEmptyEntityCombination(measure) => AllSimplePanesExceptProfileOverTime,
                    _ => OverTimeAndCompetitionPanesOnly
                };
                if (_productContext.IsAllVue)
                {
                    panesToGenerateForMeasure = panesToGenerateForMeasure.Where(viewType =>
                        viewType != ViewTypeEnum.ProfileOverTime && viewType != ViewTypeEnum.Profile).ToArray();
                }

                targetPage.Panes = panesToGenerateForMeasure.Select(i => new PaneDescriptor
                {
                    Id = $"{measure.Name}_{i}",
                    PaneType = "Standard",
                    PageName = targetPage.Name,
                    Height = 500,
                    View = (int)i,
                    Parts = new[]
                    {
                            new PartDescriptor
                            {
                                PaneId = $"{Appendix}_{i}", Spec1 = measure.Name,
                                PartType = PaneTypeToPartType(i)
                            }
                        }
                }).ToArray();
            }

            MoveAppendixPageToEndOfHierarchyAndOrderByName(pageHierarchy);

            GenerateAutoMetrics(pageHierarchy, null);

            return pageHierarchy.ChildPages.ToArray();
        }


        private static bool ShouldGenerateAppendixEntry(Measure measure, IReadOnlySet<string> usedMeasureNames, bool isValidDeploymentEnvForAppendixMeasures) =>
            !usedMeasureNames.Contains(measure.Name) && isValidDeploymentEnvForAppendixMeasures;

        private IEnumerable<Measure> GetMeasuresToGeneratePanesFor(Subset[] selectedSubsets,
            IReadOnlySet<string> usedMeasureNames)
        {
            bool shouldGenerateAppendix = _initialWebAppConfig.ShouldGenerateAppendix;

            var measures = selectedSubsets.Any()
                ? selectedSubsets.SelectMany(subset => _measureRepository.GetAllMeasuresWithDisabledPropertyFalseForSubset(subset))
                : _measureRepository.GetAllForCurrentUser();

            return measures
                .Where(m => !m.DisableMeasure && (m.CalculationType == CalculationType.YesNo ||
                                                  m.CalculationType == CalculationType.Average || EatingOutMarketMetricsHelper.IsEatingOutMarketMetric(m.CalculationType)))
                .Where(m => ShouldGenerateAppendixEntry(m, usedMeasureNames, shouldGenerateAppendix));
        }

        private static string TrimColonDelimitedPrefixFromPageName(string name, string prefix) =>
            name.StartsWith($"{prefix}:") ? name.Replace($"{prefix}:", string.Empty) : name;

        private PageDescriptor GetOrCreateTargetPageForMeasure(Subset[] selectedSubsets, PageDescriptor hierarchyDescriptors, Measure measure)
        {
            var pageName = Appendix;
            var targetPage = hierarchyDescriptors.ChildPages.SingleOrDefault(p => p.Name == pageName);

            var measureHelpText = selectedSubsets.Any() && measure.HelpText.IsNullOrWhiteSpace() && measure.PrimaryFieldDependencies.OnlyOrDefault() is {} f ? f.GetDataAccessModel(selectedSubsets.First().Id).Question : measure.HelpText;

            if (targetPage == null)
            {

                targetPage = SynthesizePageDescriptor(pageName,
                    "Standard",
                    "",
                    measure.Subset,
                    new[] { Roles.SystemAdministrator });

                if (((IMetadataEntity)targetPage).Included(_userContext.Role, selectedSubsets))
                {
                    hierarchyDescriptors.ChildPages.Add(targetPage);
                }
            }

            targetPage.ChildPages ??= new List<PageDescriptor>();

            var targetPageName = TrimColonDelimitedPrefixFromPageName(measure.Name, targetPage.Name);

            if (targetPageName.Equals(targetPage.Name))
                return targetPage;

            var minorOrSubPage = SynthesizePageDescriptor(targetPageName, targetPage.PageType == "Standard" ? "SubPage" : "MinorPage", measureHelpText, measure.Subset);
            targetPage.ChildPages.Add(minorOrSubPage);

            return minorOrSubPage;
        }

        private static PageDescriptor SynthesizePageDescriptor(string name, string pageType, string helpText, IEnumerable<Subset> subsets, string[] roles = null, string displayName = null, string layout = null, bool startPage = false)
        {
            var pageSubsetConfigurations = subsets?.Select(subset => new PageSubsetConfigurationModel(subset.Id, true)).ToList() ?? new List<PageSubsetConfigurationModel>();
            return new PageDescriptor
            {
                Name = name,
                DisplayName = displayName ?? name,
                HelpText = helpText,
                PageSubsetConfiguration = pageSubsetConfigurations,
                PageType = pageType,
                Roles = roles,
                ChildPages = new List<PageDescriptor>(),
                Layout = layout,
                StartPage = startPage
            };
        }

        private static bool NonEmptyEntityCombination(Measure measure) =>
            measure.EntityCombination.Any();

        // Profile over time is currently only supported by brand metrics as it requires a single focus instance id
        private static bool EntityCombinationBrandOnly(Measure measure) =>
            measure.EntityCombination.Count() == 1 && measure.EntityCombination.First().IsBrand;

        private static void MoveAppendixPageToEndOfHierarchyAndOrderByName(PageDescriptor hierarchyDescriptors)
        {
            var appendixPage = hierarchyDescriptors.ChildPages.SingleOrDefault(p => p.Name == Appendix);

            if (appendixPage == null)
                return;

            appendixPage.ChildPages = appendixPage.ChildPages.OrderBy(cp => cp.Name).ToList();

            if (hierarchyDescriptors.ChildPages.Remove(appendixPage))
            {
                hierarchyDescriptors.ChildPages = hierarchyDescriptors.ChildPages.Append(appendixPage).ToList();
            }
        }

        private static void GenerateAutoMetrics(PageDescriptor page, PageDescriptor parent)
        {
            if (page.Panes != null
                && page.Panes.Length == 1
                && page.ChildPages == null
                && page.Panes.Single().Parts.Length == 1
                && page.Panes.Single().Parts.Single().AutoMetrics != null)
            {
                int childIndex = parent.ChildPages.IndexOf(page);
                parent.ChildPages.RemoveAt(childIndex);
                var newParent = new PageDescriptor
                {
                    Name = page.Name,
                    DisplayName = page.DisplayName,
                    ChildPages = new List<PageDescriptor>(new[] { page })
                };
                parent.ChildPages.Insert(childIndex, newParent);
                page.Name = "All " + page.Name;
                page.DisplayName = "All " + page.DisplayName;

                var paneDescriptor = page.Panes.Single();
                var partDescriptor = paneDescriptor.Parts.Single();
                string sharedPrefix = GetSharedPrefix(partDescriptor.AutoMetrics);

                foreach (string metric in partDescriptor.AutoMetrics.OrderBy(metricName=>metricName))
                {
                    string name = metric.Substring(sharedPrefix.Length);
                    var autoMetricPage = new PageDescriptor
                    {
                        Name = name,
                        DisplayName = name,
                        PageSubsetConfiguration = page.PageSubsetConfiguration,
                        HelpText = page.HelpText,
                        Panes = partDescriptor.AutoPanes.Select(view => new PaneDescriptor
                        {
                            PaneType = paneDescriptor.PaneType,
                            Height = paneDescriptor.Height,
                            View = int.Parse(view),
                            Parts = new[] {new PartDescriptor
                            {
                                Spec1 = metric,
                                Spec3 = string.Empty, // Currently needed for Profile and Profile Over time view
                                PartType = PaneTypeToPartType(view)
                            }}
                        }).ToArray()
                    };
                    newParent.ChildPages.Add(autoMetricPage);
                }

            }

            if (page.ChildPages == null)
                return;

            foreach (var childPage in page.ChildPages.ToArray())
            {
                GenerateAutoMetrics(childPage, page);
            }
        }

        private static string GetSharedPrefix(IReadOnlyList<string> values)
        {
            if (values.Count <= 1)
            {
                return string.Empty;
            }

            int commonPrefixLength = 0;
            while(commonPrefixLength < values.Min(p => p.Length))
            {
                char compChar = values[0][commonPrefixLength];
                if (values.Any(p => p[commonPrefixLength] != compChar))
                {
                    break;
                }

                commonPrefixLength++;
            }

            return values[0].Substring(0, commonPrefixLength);
        }

        private static string PaneTypeToPartType(string pane) =>
            pane switch
            {
                "1" => "BoxChartTall",
                "2" => "ColumnChart",
                "3" => "ProfileChart",
                "4" => "ProfileChartOverTime",
                "5" => "RankingTable",
                _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, "Unsupported pane type")
            };
        private static string PaneTypeToPartType(ViewTypeEnum pane) =>
            pane switch
            {
                ViewTypeEnum.OverTime => "BoxChartTall",
                ViewTypeEnum.Competition => "ColumnChart",
                ViewTypeEnum.Profile => "ProfileChart",
                ViewTypeEnum.ProfileOverTime=> "ProfileChartOverTime",
                ViewTypeEnum.RankingTable => "RankingTable",
                _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, "Unsupported pane type")
            };
        private void AddPanesAndPartsToSubHierarchy(ILookup<string, PaneDescriptor> panesByPageName, ILookup<string, PartDescriptor> partsByPaneId, Subset[] subsets, PageDescriptor page)
        {
            string role = _userContext.Role;

            page.Panes = panesByPageName[page.Name].Where(p => ((IMetadataEntity)p).Included(role, subsets)).Clone().ToArray();
            foreach (var pane in page.Panes)
            {
                pane.Parts = partsByPaneId[pane.Id].Where(p => ((IMetadataEntity)p).Included(role, subsets)).ToArray();
            }

            if (page.ChildPages == null) return;

            foreach (var childPage in page.ChildPages)
            {
                AddPanesAndPartsToSubHierarchy(panesByPageName, partsByPaneId, subsets, childPage);
            }
        }

        private void FilterOutPagesNotAccessibleToSubsetsAndRole(ICollection<PageDescriptor> filteredPages, IEnumerable<PageDescriptor> pagesToFilter, Subset[] subsets)
        {
            if (pagesToFilter == null) return;

            foreach (var pageToFilter in pagesToFilter.Where(p => ((IMetadataEntity)p).Included(_userContext.Role, subsets)))
            {
                var filteredPage = (PageDescriptor)pageToFilter.Clone();
                filteredPages.Add(filteredPage);

                // Check if we need to filter child pages as well
                if (pageToFilter.ChildPages == null || pageToFilter.ChildPages.Count == 0)
                {
                    continue;
                }

                filteredPage.ChildPages = new List<PageDescriptor>();
                FilterOutPagesNotAccessibleToSubsetsAndRole(filteredPage.ChildPages, pageToFilter.ChildPages, subsets);
            }
        }

        private PageDescriptor GetPagePaneAndPartHierarchy(Subset[] subsets)
        {
            var (pages, reportSubPages) = GetUnfilteredPagesWithReportsRemoved();
            var filteredPagesWithChildren = new List<PageDescriptor>();
            var filteredReportSubPagesWithChildren = new List<PageDescriptor>();
            FilterOutPagesNotAccessibleToSubsetsAndRole(filteredPagesWithChildren, pages, subsets);
            FilterOutPagesNotAccessibleToSubsetsAndRole(filteredReportSubPagesWithChildren, reportSubPages, subsets);

            var pageHierarchy = new PageDescriptor { Name = "Root", ChildPages = filteredPagesWithChildren };
            var panesByPageName = _panesRepo.GetPanes().ToLookup(p => p.PageName);
            var partsByPaneId = _partsRepo.GetParts().ToLookup(p => p.PaneId);
            AddPanesAndPartsToSubHierarchy(panesByPageName, partsByPaneId, subsets, pageHierarchy);
            var allVueConfiguration = _allVueConfigurationRepository.GetConfigurationDetails();
            if (_productContext.IsAllVue)
            {
                var synthesizedPage = SynthesizeTabWithPane(allVueConfiguration, subsets);
                pageHierarchy.ChildPages.AddRange(synthesizedPage);
            }

            if ((_productContext.IsAllVue && allVueConfiguration.IsDataTabAvailable) ||
                (_userContext.IsSystemAdministrator && _initialWebAppConfig.LoadConfigFromSql) )
            {
                var hasCrossTabPane = panesByPageName.Any(g => g.Any(p => p.PaneType == CrossTabPaneType));
                bool hasStartPage = pageHierarchy.ChildPages.Any(p => p.StartPage);
                var crosstabPageDescriptor = SynthesizeCrosstabPageWithPane(subsets, _productContext.IsAllVue, hasStartPage);
                if (!hasCrossTabPane && ((IMetadataEntity)crosstabPageDescriptor).Included(_userContext.Role, subsets))
                {
                    pageHierarchy.ChildPages.Add(crosstabPageDescriptor);
                }
            }

            if (_productContext.IsAllVue)
            {
                if (allVueConfiguration.IsReportsTabAvailable)
                {
                    var reportsPane = panesByPageName.SingleOrDefault(g => g.Any(p => p.PaneType == ReportsTabPaneType));
                    var hasReportsPane = reportsPane != null;

                    var reportsPageDescriptor = hasReportsPane ?
                        filteredPagesWithChildren.SingleOrDefault(page => page.Name == reportsPane.Key) :
                        SynthesiseReportsPageWithPane(subsets);
                    if (reportsPageDescriptor != null && ((IMetadataEntity)reportsPageDescriptor).Included(_userContext.Role, subsets))
                    {
                        if (!hasReportsPane)
                        {
                            pageHierarchy.ChildPages.Add(reportsPageDescriptor);
                        }
                        foreach (var page in filteredReportSubPagesWithChildren)
                        {
                            AddPanesAndPartsToSubHierarchy(panesByPageName, partsByPaneId, subsets, page);
                        }
                        reportsPageDescriptor.ChildPages.AddRange(filteredReportSubPagesWithChildren);
                    }
                }
                var hasSettingsPane = panesByPageName.Any(g => g.Any(p => p.PaneType == SettingsTabPaneType));
                var settingsPageDescriptor = SynthesiseSettingsPageWithPane(subsets);
                if (!hasSettingsPane && ((IMetadataEntity)settingsPageDescriptor).Included(_userContext.Role, subsets))
                {
                    pageHierarchy.ChildPages.Add(settingsPageDescriptor);
                }
            }

            return pageHierarchy;
        }

        private (IEnumerable<PageDescriptor> pages, IEnumerable<PageDescriptor> reportSubPages) GetUnfilteredPagesWithReportsRemoved()
        {
            var allReports = _reportRepository.GetAll();
            var userAccessibleReports = allReports.Where(r => r.IsShared || r.CreatedByUserId == _userContext.UserId);
            var allReportPageIds = allReports.Select(r => r.ReportPageId).ToHashSet();
            var userAccessibleReportPageIds = userAccessibleReports.Select(r => r.ReportPageId).ToHashSet();

            var unfilteredPagesWithChildren = _pagesRepo.GetTopLevelPagesWithChildPages();
            var pagesWithReportsRemoved = unfilteredPagesWithChildren.Where(p => !allReportPageIds.Contains(p.Id));
            var reportPages = unfilteredPagesWithChildren.Where(p => userAccessibleReportPageIds.Contains(p.Id));

            return (pagesWithReportsRemoved, reportPages);
        }

        private static string[] DefaultRoles(string pageName) =>
            pageName switch
            {
                Crosstabbing => new[] {Roles.Administrator, Roles.SystemAdministrator, Roles.User, Roles.TrialUser},
                ReportsPageName => new[]
                    {Roles.Administrator, Roles.SystemAdministrator, Roles.User, Roles.ReportViewer, Roles.TrialUser},
                SettingsPageName => new[] {Roles.Administrator, Roles.SystemAdministrator, Roles.User, Roles.TrialUser},
                _ => throw new ArgumentOutOfRangeException(nameof(pageName), pageName, null)
            };

        private static IEnumerable<PageDescriptor> SynthesizeTabWithPane(AllVueConfigurationDetails productConfiguration, Subset[] subsets)
        {
            var result = new List<PageDescriptor>();
            foreach (var widget in productConfiguration.AdditionalUiWidgets)
            {

                if (widget.ReferenceType == CustomUIIntegration.IntegrationReferenceType.ReportVue)
                {
                    var reportVuePageDescriptor = SynthesizePageDescriptor(widget.Path,
                        "Standard", string.Empty, subsets,
                        displayName: widget.Name,
                        layout: AllVueLayoutType, roles: new[] { Roles.SystemAdministrator });
                    var reportVuePane = new PaneDescriptor
                    {
                        Id = widget.Path,
                        PageName = widget.Name,
                        Height = 500,
                        PaneType = ReportVueTabPaneType,
                        View = (int)ViewTypeEnum.SingleSurveyNav,
                        Parts = Array.Empty<PartDescriptor>()
                    };
                    reportVuePageDescriptor.Panes = reportVuePane.Yield().ToArray();
                    result.Add(reportVuePageDescriptor);
                }

                if (widget.ReferenceType == CustomUIIntegration.IntegrationReferenceType.Page)
                {
                    var reportVuePageDescriptor = SynthesizePageDescriptor(widget.Path,
                        "Standard", string.Empty, subsets,
                        displayName: widget.Name,
                        layout: AllVueLayoutType, roles: new[] { Roles.SystemAdministrator });
                    var reportVuePane = new PaneDescriptor
                    {
                        Id = widget.Path,
                        PageName = widget.Name,
                        Height = 500,
                        PaneType = AllVueWebPagePaneType,
                        View = (int)ViewTypeEnum.SingleSurveyNav,
                        Parts = Array.Empty<PartDescriptor>()
                    };
                    reportVuePageDescriptor.Panes = reportVuePane.Yield().ToArray();
                    result.Add(reportVuePageDescriptor);
                }
            }
            return result;
        }

        private static PageDescriptor SynthesizeCrosstabPageWithPane(Subset[] subsets, bool isSurveyVue, bool hasStartPage)
        {
            var crosstabPageDescriptor = SynthesizePageDescriptor(Crosstabbing, "Standard", string.Empty, subsets, displayName: CrosstabPageDisplayName, layout: AllVueLayoutType, roles: DefaultRoles(Crosstabbing),
                startPage: isSurveyVue && !hasStartPage);
            var crosstabPane = new PaneDescriptor
            {
                Id = Crosstabbing,
                PageName = Crosstabbing,
                Height = 500,
                PaneType = CrossTabPaneType,
                View =  isSurveyVue ? (int) ViewTypeEnum.SingleSurveyNav : (int) ViewTypeEnum.FullPage,
                Parts = Array.Empty<PartDescriptor>()
            };
            crosstabPageDescriptor.Panes = crosstabPane.Yield().ToArray();
            return crosstabPageDescriptor;
        }

        private static PageDescriptor SynthesiseReportsPageWithPane(Subset[] subsets)
        {
            var reportsPageDescriptor = SynthesizePageDescriptor(ReportsPageName, "Standard", string.Empty, subsets, layout: AllVueLayoutType, roles: DefaultRoles(ReportsPageName));
            var reportsPagePane = new PaneDescriptor
            {
                Id = ReportsPageName,
                PageName = ReportsPageName,
                Height = 500,
                PaneType = ReportsTabPaneType,
                View = (int)ViewTypeEnum.SingleSurveyNav,
                Parts = Array.Empty<PartDescriptor>()
            };
            reportsPageDescriptor.Panes = reportsPagePane.Yield().ToArray();
            return reportsPageDescriptor;
        }

        private PageDescriptor SynthesiseSettingsPageWithPane(Subset[] subsets)
        {
            var settingsPageDescriptor = SynthesizePageDescriptor(SettingsPageName, "Standard", string.Empty, subsets, layout: AllVueLayoutType, roles: DefaultRoles(SettingsPageName));
            var settingsPagePane = new PaneDescriptor
            {
                Id = SettingsPageName,
                PageName = SettingsPageName,
                Height = 500,
                PaneType = SettingsTabPaneType,
                View = (int)ViewTypeEnum.SingleSurveyNav,
                Parts = Array.Empty<PartDescriptor>()
            };
            settingsPageDescriptor.Panes = settingsPagePane.Yield().ToArray();

            var configurationPageDescriptor = PageDescriptor(subsets, ConfigurationPageName);
            settingsPageDescriptor.ChildPages.Add(configurationPageDescriptor);

            var surveyConfigurationPageDescriptor = PageDescriptor(subsets, SurveyConfigurationPageName);
            settingsPageDescriptor.ChildPages.Add(surveyConfigurationPageDescriptor);

            var usersPageDescriptor = PageDescriptor(subsets, UsersPageName);
            settingsPageDescriptor.ChildPages.Add(usersPageDescriptor);

            var weightingPageDescriptor = PageDescriptor(subsets, WeightingPageName);
            settingsPageDescriptor.ChildPages.Add(weightingPageDescriptor);

            var exportsPageDescriptor = PageDescriptor(subsets, ExportsPageName);
            settingsPageDescriptor.ChildPages.Add(exportsPageDescriptor);

            var featuresPageDescriptor = PageDescriptor(subsets, FeaturesPageName);
            settingsPageDescriptor.ChildPages.Add(featuresPageDescriptor);

            return settingsPageDescriptor;
        }

        private static PageDescriptor PageDescriptor(Subset[] subsets, string nameOfPage)
        {
            var exportsPageDescriptor = SynthesizePageDescriptor(nameOfPage, "Standard", string.Empty, subsets,
                layout: AllVueLayoutType, roles: DefaultRoles(SettingsPageName));
            var exportsPagePane = new PaneDescriptor
            {
                Id = nameOfPage,
                PageName = nameOfPage,
                PaneType = SettingsTabPaneType,
                View = (int)ViewTypeEnum.SingleSurveyNav,
                Parts = Array.Empty<PartDescriptor>()
            };
            exportsPageDescriptor.Panes = exportsPagePane.Yield().ToArray();
            return exportsPageDescriptor;
        }

        private static void GetUsedMeasuresForPageSubHierarchy(PageDescriptor page, HashSet<string> usedMeasureNames)
        {
            foreach (var part in page.Panes.SelectMany(p => p.Parts))
            {
                var namesUsedInThisPart = (part.Spec1 ?? "").Split('|').Where(s => !string.IsNullOrEmpty(s));
                usedMeasureNames.AddRange(namesUsedInThisPart);
            }

            if (page.ChildPages == null) return;

            foreach (var childPage in page.ChildPages)
            {
                GetUsedMeasuresForPageSubHierarchy(childPage, usedMeasureNames);
            }
        }

        private IReadOnlySet<string> GetUsedMeasureNamesForPageHierarchy(PageDescriptor pageHierarchy)
        {
            var usedMeasures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            GetUsedMeasuresForPageSubHierarchy(pageHierarchy, usedMeasures);
            return usedMeasures;
        }
    }
}
