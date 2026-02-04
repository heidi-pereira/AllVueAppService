using System.Collections.Immutable;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public class ResultsProviderParameters
    {
        public Subset Subset { get; set; }
        public Measure PrimaryMeasure { get; set; }
        public ImmutableArray<EntityInstance> EntityInstances => RequestedInstances.OrderedInstances;
        public TargetInstances RequestedInstances { get; set; }
        public TargetInstances[] FilterInstances {get; set; }
        public CompositeFilterModel FilterModel { get; set; }
        public IGroupedQuotaCells QuotaCells { get; set; }
        public CalculationPeriod CalculationPeriod { get; set; }
        public AverageDescriptor Average { get; set; }
        public AverageType AverageType { get; set; }
        public IReadOnlyCollection<Measure> Measures { get; set; }

        public bool IncludeSignificance { get; set; }
        public SigConfidenceLevel SigConfidenceLevel { get; set; } = SigConfidenceLevel.NinetyFive;

        public IEntityRepository EntityRepository { get; set; }

        public bool DoMeasuresIncludeMarketMetric { get; set; }
        public bool CompetitorsContainsActiveBrand { get; set; }

        public int? SampleSizeEntityInstanceId { get; set; }
        public int? LowSampleEntityInstanceId { get; set; }
        public int? FocusEntityInstanceId { get; set; }


        /// Relies on results being returned in order of ascending entity id
        public int MultiMetricEntityInstanceIndex { get; set; }
        public int FunnelEntityInstanceIndex { get; set; }
        public int ScorecardEntityInstanceIndex { get; set; }
        public MainQuestionType QuestionType { get; set; }
        public EntityMeanMap EntityMeanMaps { get; set; }

        public bool AreMeasuresOrderedByResult
        {
            get
            {
                var categories = new Dictionary<string, List<string>>();
                foreach (var metric in Measures)
                {
                    var items = metric.Name.Split(':');
                    if (items.Any())
                    {
                        string firstItem = items[0];
                        if (!categories.ContainsKey(firstItem))
                        {
                            categories.Add(firstItem, new List<string>());
                        }
                        if (items.Length >= 2)
                        {
                            categories[firstItem].Add(items[1]);
                        }
                    }
                }

                //True if we only have a single category || all our categories have no items (ie not categorized)
                //This is subtly/minorly different to getCategoryHierarchy in Multimetric.tsx
                return categories.Keys.Count == 1 || categories.Values.All(x => x.Count == 0);
            }
        }

        public Break[] Breaks { get; set; }
    }
}