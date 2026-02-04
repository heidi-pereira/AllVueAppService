using System.Linq;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.Weightings;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData
{
    public class WeightingPlanRepository : IWeightingPlanRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

        public WeightingPlanRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        private static IQueryable<WeightingPlanConfiguration> WeightingPlansInContext(MetaDataContext metaDataContext, string product, string subProductIdOrNull) =>
            metaDataContext.WeightingPlanConfigurations
                .Include(wp => wp.ChildTargets)
                .Where(wp => wp.ProductShortCode.Equals(product) && wp.SubProductId.Equals(subProductIdOrNull));

        public IReadOnlyCollection<WeightingPlanConfiguration> GetWeightingPlans(string product,
            string subProductIdOrNull)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            return WeightingPlansInContext(metaDataContext, product, subProductIdOrNull)
                .Include(wp => wp.ChildTargets)
                .ThenInclude(wt => wt.ResponseWeightingContext).ToArray();
        }

        public IReadOnlyCollection<(string subsetId, IReadOnlyCollection<WeightingPlanConfiguration> plans)> GetWeightingPlansBySubsetId(string product, string subProductIdOrNull)
        {
            var allPlans = GetWeightingPlans(product, subProductIdOrNull);
            Dictionary<string, List<WeightingPlanConfiguration>> rootPlansPerSubset = new Dictionary<string, List<WeightingPlanConfiguration>>();
            foreach (var plan in allPlans)
            {
                if (plan.ParentTarget == null)
                {
                    if (!rootPlansPerSubset.ContainsKey(plan.SubsetId))
                    {
                        rootPlansPerSubset[plan.SubsetId] = new List<WeightingPlanConfiguration>();
                    }
                    rootPlansPerSubset[plan.SubsetId].Add(plan);
                }
            }
            return rootPlansPerSubset.Select(x => (subsetId: x.Key, plans: (IReadOnlyCollection<WeightingPlanConfiguration>)(x.Value.AsReadOnly()))).ToList();
        }

        public IReadOnlyCollection<WeightingPlanConfiguration> GetWeightingPlansForSubset(string product, string subProductIdOrNull, string subsetId)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            return WeightingPlansInContext(metaDataContext, product, subProductIdOrNull)
                .Include(wp => wp.ChildTargets)
                .ThenInclude(wt => wt.ResponseWeightingContext)
                .Where(wp => wp.SubsetId == subsetId).ToList();
        }

        public IReadOnlyCollection<WeightingPlanConfiguration> GetLoaderWeightingPlansForSubset(string product, string subProductIdOrNull, string subsetId)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            //This is a hack to allow UAT to load samsung data which takes a long time
            //https://savanta.uat.all-vue.com/survey/samsung-eu-campaigns/ui/
            //need to investigate why it takes so long, eg missing index etc.
            //Also investigate why there are two ToList() calls here as that seems unnecessary

            metaDataContext.Database.SetCommandTimeout(180);
            var plans = WeightingPlansInContext(metaDataContext, product, subProductIdOrNull)
                .Include(wp => wp.ChildTargets)
                .ThenInclude(wt => wt.ResponseWeightingContext)
                .ThenInclude(rwc => rwc.ResponseWeights)
                .Where(wp => wp.SubsetId == subsetId).ToList().Where(wp => wp.ParentWeightingTargetId == null).ToList();
            return plans;
        }

        public void CreateWeightingPlan(string product, string subProductIdOrNull,
            WeightingPlanConfiguration weightingPlanConfiguration)
        {
            ValidateWeightingPlan(weightingPlanConfiguration, product, subProductIdOrNull);
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            metaDataContext.WeightingPlanConfigurations.Add(weightingPlanConfiguration);
            metaDataContext.SaveChanges();
        }
        private void ListOfSubPlans(List<WeightingPlanConfiguration> plans, WeightingPlanConfiguration root)
        {
            plans.Add(root);
            foreach (var target in root.ChildTargets)
            {
                if (target.ChildPlans != null)
                {
                    foreach (var plan in target.ChildPlans)
                    {
                        ListOfSubPlans(plans, plan);
                    }
                }
            }
        }

        public void UpdateWeightingPlanForSubset(string product, string subProductIdOrNull, string subset, IReadOnlyCollection<WeightingPlanConfiguration> dbPlans)
        {
            var existingPlans = GetWeightingPlansForSubset(product, subProductIdOrNull, subset).ToList();

            using var metaDataContext = _dbContextFactory.CreateDbContext();
            foreach (var plan in dbPlans)
            {
                if (plan.Id == 0)
                {
                    metaDataContext.WeightingPlanConfigurations.Add(plan);
                }
                else
                {
                    var subPlans = new List<WeightingPlanConfiguration>();
                    ListOfSubPlans(subPlans, plan);
                    existingPlans.RemoveAll(planToRemove => subPlans.Any(x => x.Id == planToRemove.Id) );
                    metaDataContext.WeightingPlanConfigurations.Update(plan);
                }
            }
            foreach (var plan in existingPlans)
            {
                metaDataContext.WeightingPlanConfigurations.Remove(plan);
            }
            metaDataContext.SaveChanges();
        }

        public void UpdateWeightingPlan(string product, string subProductIdOrNull, WeightingPlanConfiguration weightingPlanConfiguration)
        {
            ValidateWeightingPlan(weightingPlanConfiguration, product, subProductIdOrNull);
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            metaDataContext.WeightingPlanConfigurations.Update(weightingPlanConfiguration);
            metaDataContext.SaveChanges();
        }

        public void UpdateAllWeightingPlans(string product, string subProductIdOrNull, IList<WeightingPlanConfiguration> newWeightingPlanConfigurations)
        {
            var itemsToUpdate = newWeightingPlanConfigurations.Where(x => !string.IsNullOrEmpty(x.VariableIdentifier)).ToList();
            ValidateWeightingPlans(itemsToUpdate, product, subProductIdOrNull);
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            var existingWeightings = WeightingPlansInContext(metaDataContext, product, subProductIdOrNull);
            metaDataContext.RemoveRange(existingWeightings);
            metaDataContext.SaveChanges();

            metaDataContext.WeightingPlanConfigurations.AddRange(itemsToUpdate);
            metaDataContext.SaveChanges();
        }

        public void DeleteWeightingPlan(string product, string subProductIdOrNull, int weightingPlanId)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            var weightingPlans = WeightingPlansInContext(metaDataContext, product, subProductIdOrNull).Include(wp => wp.ChildTargets).ToList();
            var planToRemove = weightingPlans.SingleOrDefault(wp => wp.Id == weightingPlanId);
            if (planToRemove is null)
                throw new BadRequestException("Weighting plan not found");

            RemovePlansAndChildPlansFromContext(metaDataContext, planToRemove);
            planToRemove.ParentTarget = null;
            planToRemove.ParentWeightingTargetId = null;

            metaDataContext.SaveChanges();
        }

        private void RemovePlansAndChildPlansFromContext(MetaDataContext metaDataContext, WeightingPlanConfiguration parentPlan)
        {
            foreach (var target in parentPlan.ChildTargets)
            {
                if (target.ChildPlans != null)
                {
                    foreach (var childPlan in target.ChildPlans)
                    {
                        RemovePlansAndChildPlansFromContext(metaDataContext, childPlan);
                    }
                }
            }
            metaDataContext.WeightingPlanConfigurations.Remove(parentPlan);
        }

        public void DeleteWeightingTarget(string subsetId, string product, string subProductIdOrNull,
            int weightingTargetId)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();

            DeleteChildPlans(subsetId, product, subProductIdOrNull, weightingTargetId, metaDataContext);

            var weightingTargetToRemove = metaDataContext.WeightingTargetConfigurations
                .Include(wt => wt.ChildPlans)
                .Where(wp => wp.ProductShortCode.Equals(product) && wp.SubProductId.Equals(subProductIdOrNull)).Single(wt => wt.Id == weightingTargetId);

            metaDataContext.WeightingTargetConfigurations.Remove(weightingTargetToRemove);
            metaDataContext.SaveChanges();
        }

        public void DeleteWeightingChildPlansForTarget(string subsetId, string product, string subProductIdOrNull, int weightingTargetId)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            
            DeleteChildPlans(subsetId, product, subProductIdOrNull, weightingTargetId, metaDataContext);
            metaDataContext.SaveChanges();
        }

        private void DeleteChildPlans(string subsetId, string product, string subProductIdOrNull, int weightingTargetId,
            MetaDataContext metaDataContext)
        {
            var weightingPlansToRemove = WeightingPlansInContext(metaDataContext, product, subProductIdOrNull)
                .Include(wp => wp.ChildTargets)
                .Where(wp => wp.SubsetId == subsetId).ToList().Where(wp => wp.ParentWeightingTargetId == weightingTargetId);
            if (weightingPlansToRemove != null)
            {
                foreach (var wp in weightingPlansToRemove)
                {
                    RemovePlansAndChildPlansFromContext(metaDataContext, wp);
                    metaDataContext.WeightingPlanConfigurations.Remove(wp);
                }
            }
        }

        public void DeleteWeightingPlanForSubset(string product, string subProductIdOrNull, string subsetId)
        {
            using var metaDataContext = _dbContextFactory.CreateDbContext();
            var weightingPlansToRemove = WeightingPlansInContext(metaDataContext, product, subProductIdOrNull)
                .Include(wp => wp.ChildTargets)
                .Where(wp => wp.SubsetId == subsetId).ToList();

            if (!weightingPlansToRemove.Any())
            {
                return;
            }
            foreach (var plan in weightingPlansToRemove)
            {
                metaDataContext.WeightingPlanConfigurations.Remove(plan);
            }
            SetAllReportsToBeNotWeighted(subProductIdOrNull, metaDataContext);

            metaDataContext.SaveChanges();
        }

        private static void SetAllReportsToBeNotWeighted(string subProductIdOrNull, MetaDataContext metaDataContext)
        {
            var weightedReportsForSubProduct = metaDataContext.SavedReports.Where(r => r.SubProductId == subProductIdOrNull && r.IsDataWeighted);
            foreach (var report in weightedReportsForSubProduct)
            {
                report.IsDataWeighted = false;
            }
        }

        private static void ValidateWeightingPlans(IEnumerable<WeightingPlanConfiguration> newWeightingPlanConfigurations,
            string product, string subProductIdOrNull)
        {
            foreach (var weightingPlan in newWeightingPlanConfigurations)
            {
                ValidateWeightingPlan(weightingPlan, product, subProductIdOrNull);
            }
        }

        private static void ValidateWeightingPlan(WeightingPlanConfiguration weightingPlanConfiguration, string product, string subProductIdOrNull)
        {
            EnsureProductSubProductIsSet(product, subProductIdOrNull, weightingPlanConfiguration);
            // Every weighting plan should have targets
            if (weightingPlanConfiguration.ChildTargets is null)
                ThrowValidationException("Weighting must contain targets");
        }

        private static void EnsureProductSubProductIsSet(string product, string subProductIdOrNull, WeightingPlanConfiguration weightingPlanConfiguration)
        {
            weightingPlanConfiguration.ProductShortCode = product;
            weightingPlanConfiguration.SubProductId = subProductIdOrNull;
        }

        private static void ThrowValidationException(string message) => throw new BadRequestException(message);

    }
}