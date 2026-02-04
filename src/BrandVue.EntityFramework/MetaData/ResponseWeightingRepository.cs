using BrandVue.EntityFramework.MetaData.Weightings;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using EFCore.BulkExtensions;

namespace BrandVue.EntityFramework.MetaData
{
    public class ResponseWeightingRepository : IResponseWeightingRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;

        public ResponseWeightingRepository(IDbContextFactory<MetaDataContext> dbContextFactory, IProductContext productContext)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
        }
        
        public bool AreThereAnyRootResponseWeights(string subsetId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            return dbContext.ResponseWeightingContexts
                .Any(rwc => rwc.ProductShortCode == _productContext.ShortCode &&
                                       rwc.SubProductId == _productContext.SubProductId &&
                                       rwc.SubsetId == subsetId &&
                                       rwc.WeightingTargetId == null);
        }

        public ResponseWeightingContext GetRootResponseWeightingContextWithWeightsForSubset(string subsetId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.ResponseWeightingContexts
                .Include(rwc => rwc.ResponseWeights)
                .FirstOrDefault(rwc => rwc.ProductShortCode == _productContext.ShortCode &&
                                       rwc.SubProductId == _productContext.SubProductId &&
                                       rwc.SubsetId == subsetId &&
                                       rwc.WeightingTargetId == null);
        }

        public bool CreateResponseWeightsForRoot(string subsetId, IList<ResponseWeightConfiguration> weights)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            using var transaction = dbContext.Database.BeginTransaction();

            if (AreThereAnyRootResponseWeights(subsetId))
            {
                DeleteResponseWeights(subsetId);
            }

            var responseWeightingContext = new ResponseWeightingContext
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                SubsetId = subsetId,
                Context = ""
            };

            dbContext.ResponseWeightingContexts.Add(responseWeightingContext);
            dbContext.SaveChanges();
            foreach (var responseWeight in weights)
            {
                responseWeight.ResponseWeightingContextId= responseWeightingContext.Id;
            }
            dbContext.BulkInsert(weights);
            
            transaction.Commit();
            return true;
        }

        private WeightingTargetConfiguration NavigateToWeightingTargetViaPath(string subsetId, int depth, 
            IList<TargetInstance> pathOfTargetInstances,
            IReadOnlyCollection<WeightingPlanConfiguration> root)
        {
            IReadOnlyCollection<WeightingPlanConfiguration> plans = root;
            while (depth < pathOfTargetInstances.Count)
            {
                var instance = pathOfTargetInstances[depth];
                var target = plans.Where(p => p.VariableIdentifier == instance.FilterMetricName)
                    .SelectMany(p => p.ChildTargets)
                    .FirstOrDefault(t => t.EntityInstanceId == instance.FilterInstanceId);
                if (target == null)
                {
                    throw new KeyNotFoundException(
                        $"No target found for subset {subsetId}, variable {instance.FilterMetricName}, entityInstanceId {instance.FilterInstanceId}");
                }
                depth++;
                if (depth >= pathOfTargetInstances.Count)
                {
                    return target;
                }
                plans = target.ChildPlans;
            }
            return null;
        }
        public int CreateResponseWeights(string subsetId,
            IReadOnlyCollection<WeightingPlanConfiguration> plans, 
            IList<TargetInstance> pathOfTargetInstances, 
            IEnumerable<ResponseWeightConfiguration> weights)
        {
            var target = NavigateToWeightingTargetViaPath(subsetId, 0, pathOfTargetInstances, plans);
            if (target == null)
            {
                throw new Exception($"Inserting into the root is not valid call {nameof(CreateResponseWeightsForRoot)}");
            }
            using var dbContext = _dbContextFactory.CreateDbContext();
            using var transaction = dbContext.Database.BeginTransaction();
            if (target.ResponseWeightingContext is not null)
            {
                DeleteResponseWeights(subsetId, target.Id, dbContext);
            }

            var responseWeightingContext = new ResponseWeightingContext
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                SubsetId = subsetId,
                WeightingTargetId = target.Id,
                Context = target.Id.ToString(),
            };

            responseWeightingContext.ResponseWeights.AddRange(weights);
            dbContext.ResponseWeightingContexts.Add(responseWeightingContext);
            dbContext.SaveChanges();
            transaction.Commit();
            return responseWeightingContext.Id;
        }

        public void DeleteResponseWeights(string subsetId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var weights = dbContext.ResponseWeightingContexts.Where(rwc => rwc.ProductShortCode == _productContext.ShortCode &&
                                                                           rwc.SubProductId == _productContext.SubProductId &&
                                                                           rwc.SubsetId == subsetId);
            if (weights != null)
            {
                dbContext.ResponseWeightingContexts.RemoveRange(weights);
                dbContext.SaveChanges();
            }
        }

        public void DeleteResponseWeightsForTarget(string subsetId, int weightingTargetId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (DeleteResponseWeights(subsetId, weightingTargetId, dbContext))
            {
                dbContext.SaveChanges();
            }
        }

        private bool DeleteResponseWeights(string subsetId, int weightingTargetId, MetaDataContext dbContext)
        {
            var hasDeleted = false;
            var weights = dbContext.ResponseWeightingContexts.SingleOrDefault(rwc =>
                rwc.ProductShortCode == _productContext.ShortCode &&
                rwc.SubProductId == _productContext.SubProductId &&
                rwc.SubsetId == subsetId &&
                rwc.WeightingTargetId == weightingTargetId);
            if (weights != null)
            {
                dbContext.ResponseWeightingContexts.Remove(weights);
                hasDeleted = true;
            }
            return hasDeleted;
        }
    }
}
