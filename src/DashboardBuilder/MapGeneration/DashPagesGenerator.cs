using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration
{
    internal class DashPagesGenerator
    {
        private const string AllPanes = "1|2|3|4|5";

        public static DashPage CreateRootPage(IReadOnlyCollection<MetricDefinition> metricDefinitions)
        {
            return CreatePage(metricDefinitions, "", "Root", new[] { "Root", "Standard", "SubPage", "MinorPage" }, c => c.SectionName, c => c.PageName, c => c.GetHumanName(": "));
        }

        /// <summary>
        /// Each recursive call nests its pages within the previous
        /// </summary>
        private static DashPage CreatePage(IReadOnlyCollection<MetricDefinition> metricDefinitions, string pageTitle, string parentPageTitle, IReadOnlyCollection<string> pageTypes, params Func<FieldContext, string>[] groupKeys)
        {
            if (metricDefinitions.Count > 1)
            {
                var remainingGroupKeys = groupKeys.ToList();
                var nextLevelMetrics = GroupMetrics(metricDefinitions, ref remainingGroupKeys);
                if (nextLevelMetrics.Count > 5 && nextLevelMetrics.All(m => m.Count() == 1))
                {
                    return CreateDashPage(pageTitle, parentPageTitle, pageTypes.First(), CreateAutoMetricsDashPane(pageTitle, metricDefinitions).ToArray());
                }
                var children = nextLevelMetrics
                    .Select(g => CreatePage(g.ToList(), g.Key, pageTitle, pageTypes.Skip(1).ToList(), remainingGroupKeys.ToArray()));
                
                return CreateDashPage(pageTitle, parentPageTitle, pageTypes.First(), pages: children);
            }

            var metricDefinition = metricDefinitions.Single();
            return CreateDashPage(metricDefinition.Name, parentPageTitle, pageTypes.First(), CreateDashPanes(metricDefinition).ToArray());
        }

        private static DashPane[] CreateAutoMetricsDashPane(string pageTitle, IReadOnlyCollection<MetricDefinition> metricDefinitions)
        {
            var metrics = metricDefinitions.Select(x => x.Name).ToArray();
            var dashPane = CreateDashPane(pageTitle, 0, metrics);
            return new [] {dashPane};
        }

        /// <summary>
        /// Skip any keys that don't provide any extra grouping without going down a layer in the page hierarchy
        /// </summary>
        private static ILookup<string, MetricDefinition> GroupMetrics(IReadOnlyCollection<MetricDefinition> metricDefinitions, ref List<Func<FieldContext, string>> getKeys)
        {
            var getKey = getKeys.FirstOrDefault() ?? throw new NotImplementedException($"Cannot group metrics to this level, add a new key to group on for {string.Join(", ", metricDefinitions.Select(m => m.Name))}");
            getKeys.RemoveAt(0);
            var childGroups = metricDefinitions.ToLookup(metric => getKey(metric.GeneratedFrom.Context).Humanize());
            return childGroups.Count > 1 ? childGroups : GroupMetrics(metricDefinitions,  ref getKeys);
        }

        private static IEnumerable<DashPane> CreateDashPanes(MetricDefinition metric)
        {
            return Enumerable.Range(1, 5).Select(i => CreateDashPane(metric.Name, i, metric.Name));
        }

        private static DashPage CreateDashPage(string pageName, string parentPageTitle, string pageType, IEnumerable<DashPane> panes = null, IEnumerable<DashPage> pages = null)
        {
            return new DashPage(panes ?? new DashPane[0], pages ?? new DashPage[0])
            {
                Name = pageName,
                MenuIcon = pageType == "Standard" ? "fa-line-chart" : "",
                PageType = pageType,
                HelpText = pageName,
                MinUserLevel = "100",
                StartPage = "",
                Layout = "",
                PageTitle = parentPageTitle,
                Disabled = "",
                Subset = "",
                Environment = "",
                Roles = ""
            };
        }

        private static DashPane CreateDashPane(string pageName, int i, params string[] metrics)
        {
            var paneId = pageName + "_" + i;
            var metricString = string.Join("|", metrics);
            var isAutoPane = i == 0;
            var dashPart = new DashPart()
            {
                PaneId = paneId,
                PartType = GetPartType(i),
                Spec1 = metricString,
                Spec2 = "",
                Spec3 = "",
                HelpText = "",
                Disabled = false,
                Environment = "",
                AutoMetrics = isAutoPane ? metricString : "",
                AutoPanes = isAutoPane ? AllPanes : ""
                
            };
            return new DashPane(new[]{dashPart})
            {
                Id = paneId,
                PageName = pageName,
                Height = 500,
                PaneType = "Standard",
                Spec = "",
                View = isAutoPane ? "2" : i.ToString()
            };
        }

        private static string GetPartType(int viewType)
        {
            switch (viewType)
            {
                case 0: return "MultiMetrics";
                case 1: return "BoxChartTall";
                case 2: return "ColumnChart";
                case 3: return "ProfileChart";
                case 4: return "ProfileChartOverTime";
                case 5: return "RankingTable";
                default: throw new ArgumentOutOfRangeException(nameof(viewType), viewType, null);
            }
        }
    }
}