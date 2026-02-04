using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.Dashboard
{
    public class PanesRepositorySql : IPanesRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IPartsRepository _partsRepository;
        private readonly PartsRepositorySql _partsRepositorySql;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IProductContext _productContext;

        public PanesRepositorySql(IProductContext productContext, IDbContextFactory<MetaDataContext> dbContextFactory,
            IPartsRepository partsRepository, ISubsetRepository subsetRepository)
        {
            _productContext = productContext;
            _dbContextFactory = dbContextFactory;
            _partsRepository = partsRepository;
            _partsRepositorySql = _partsRepository as PartsRepositorySql;
            _subsetRepository = subsetRepository;
        }

        public IReadOnlyCollection<PaneDescriptor> GetPanes()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                return dbContext.Panes
                    .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                    .OrderBy(p => p.Id)
                    .Select(p => ConvertToPaneDescriptor(p, _subsetRepository)).ToList();
            }
        }

        public void CreatePane(PaneDescriptor pane, MetaDataContext dbContext)
        {
            dbContext.Panes.Add(ConvertToDbPane(pane));
            foreach (var part in pane.Parts)
            {
                if (_partsRepositorySql != null)
                {
                    _partsRepositorySql.CreatePart(part, dbContext);
                }
                else
                {
                    _partsRepository.CreatePart(part);
                }
            }
        }

        public void CreatePane(PaneDescriptor pane)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                using var transaction = dbContext.Database.BeginTransaction();

                CreatePane(pane, dbContext);
                dbContext.SaveChanges();
                transaction.Commit();
            }
        }

        public void UpdatePane(PaneDescriptor pane)
        {
            var paneToUpdate = ConvertToDbPane(pane);
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                using var transaction = dbContext.Database.BeginTransaction();

                var existingPane = dbContext.Panes.Single(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId && p.PaneId == pane.Id);
                dbContext.Entry(existingPane).State = EntityState.Detached;
                paneToUpdate.Id = existingPane.Id;
                dbContext.Update(paneToUpdate);
                dbContext.SaveChanges();
                if (_partsRepositorySql != null)
                {
                    _partsRepositorySql.DeletePartsForPane(pane.Id, dbContext);
                }
                else
                {
                    _partsRepository.DeletePartsForPane(pane.Id);
                }

                foreach (var part in pane.Parts)
                {
                    if (_partsRepositorySql != null)
                    {
                        _partsRepositorySql.CreatePart(part);
                    }
                    else
                    {
                        _partsRepository.CreatePart(part);
                    }
                }
                transaction.Commit();
            }
        }

        public void DeletePane(string paneId, MetaDataContext dbContext)
        {
            if (_partsRepositorySql != null)
            {
                _partsRepositorySql.DeletePartsForPane(paneId, dbContext);
            }
            else
            {
                _partsRepository.DeletePartsForPane(paneId);
            }
            // First() and not Single() here because there is no unique constraint on the PaneId column
            var paneToDelete = dbContext.Panes.First(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId && p.PaneId == paneId);
            dbContext.Panes.Remove(paneToDelete);
            dbContext.SaveChanges();
        }
        public void DeletePane(string paneId)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                using var transaction = dbContext.Database.BeginTransaction();
                DeletePane(paneId, dbContext);
                transaction.Commit();
            }
        }

        private DbPane ConvertToDbPane(PaneDescriptor pane)
        {
            return new DbPane
            {
                ProductShortCode = DbFieldConverter.EncodeString(_productContext.ShortCode),
                SubProductId = DbFieldConverter.EncodeString(_productContext.SubProductId),
                PaneId = DbFieldConverter.EncodeString(pane.Id),
                PageName = DbFieldConverter.EncodeString(pane.PageName),
                Height = pane.Height,
                PaneType = DbFieldConverter.EncodeString(pane.PaneType),
                Spec = DbFieldConverter.EncodeString(pane.Spec),
                Spec2 = DbFieldConverter.EncodeString(pane.Spec2),
                View = (pane.View == int.MinValue || pane.View == 0) ? null : (int?)pane.View,
                Subset = DbFieldConverter.EncodeSubsets(pane.Subset)
            };
        }
        // Making a static method to resolve error:
        // The client projection contains a reference to a constant expression of 'BrandVue.SourceData.Dashboard.PanesRepositorySql' through the
        // instance method 'ConvertToPaneDescriptor'. This could potentially cause a memory leak; consider making the method static
        // so that it does not capture constant in the instance. See https://go.microsoft.com/fwlink/?linkid=2103067 for more information.
        private static PaneDescriptor ConvertToPaneDescriptor(DbPane dbPane, ISubsetRepository subsetRepository)
        {
            return new PaneDescriptor
            {
                Id = dbPane.PaneId,
                PageName = dbPane.PageName,
                Height = dbPane.Height,
                PaneType = dbPane.PaneType,
                Spec = dbPane.Spec ?? string.Empty,
                Spec2 = dbPane.Spec2 ?? string.Empty,
                View = dbPane.View ?? int.MinValue,
                Subset = DbFieldConverter.DecodeSubsets(subsetRepository, dbPane.Subset),
            };
        }
    }
}
