using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.Dashboard
{
    public class PartsRepositorySql : IPartsRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;

        public PartsRepositorySql(IProductContext productContext, IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _productContext = productContext;
            _dbContextFactory = dbContextFactory;
        }

        public PartDescriptor GetById(int partId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var part = dbContext.Parts
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                .Single(p => p.Id == partId);
            return ConvertToPartDescriptor(part);
        }

        public IReadOnlyCollection<PartDescriptor> GetParts()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                return dbContext.Parts
                    .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                    .OrderBy(p => p.Id)
                    .Select(p => ConvertToPartDescriptor(p)).ToList();
            }
        }

        public void CreatePart(PartDescriptor part, MetaDataContext dbContext)
        {
            dbContext.Parts.Add(ConvertToDbPart(part));
        }

        public void CreatePart(PartDescriptor part)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                CreatePart(part, dbContext);
                dbContext.SaveChanges();
            }
        }

        public void CreateParts(IEnumerable<PartDescriptor> parts)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var dbParts = parts.Select(p => ConvertToDbPart(p)).ToArray();
            dbContext.Parts.AddRange(dbParts);
            dbContext.SaveChanges();
        }

        public void UpdatePart(PartDescriptor partDescriptor)
        {
            if (partDescriptor.Id <= 0)
            {
                throw new Exception("Can only update existing parts");
            }

            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var dbPart = dbContext.Parts
                    .AsNoTracking()
                    .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                    .Single(p => p.Id == partDescriptor.Id);

                var newDbPart = ConvertToDbPart(partDescriptor);
                newDbPart.Id = partDescriptor.Id;

                dbContext.Parts.Update(newDbPart);
                dbContext.SaveChanges();
            }
        }
        public void UpdateParts(IEnumerable<PartDescriptor> parts)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var partIds = parts.Select(p => p.Id).ToArray();

            if (partIds.Any(id => id <= 0))
            {
                throw new Exception("Can only update existing parts");
            }

            var matchedDbParts = dbContext.Parts.AsNoTracking()
                .Where(p => p.ProductShortCode == _productContext.ShortCode &&
                    p.SubProductId == _productContext.SubProductId &&
                    partIds.Contains(p.Id)).ToArray();

            if (matchedDbParts.Length != partIds.Length)
            {
                var matchedIds = matchedDbParts.Select(p => p.Id).ToArray();
                var missing = partIds.Where(id => !matchedIds.Contains(id));
                throw new Exception($"Unable to update missing parts: {string.Join(", ", missing)}");
            }

            var updatedDbParts = parts.Select(p =>
            {
                var newPart = ConvertToDbPart(p);
                newPart.Id = p.Id;
                return newPart;
            }).ToArray();
            dbContext.Parts.UpdateRange(updatedDbParts);
            dbContext.SaveChanges();
        }

        public void DeletePart(int partId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var dbPart = dbContext.Parts
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                .SingleOrDefault(p => p.Id == partId);
            if (dbPart != null)
            {
                dbContext.Parts.Remove(dbPart);
                dbContext.SaveChanges();
            }
        }

        public void DeletePartsForPane(string paneId, MetaDataContext dbContext)
        {
            var partsToDelete = dbContext.Parts.Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId && p.PaneId == paneId).ToArray();

            foreach (var partToDelete in partsToDelete)
            {
                dbContext.Parts.Remove(partToDelete);
            }
            dbContext.SaveChanges();
        }


        public void DeletePartsForPane(string paneId)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                DeletePartsForPane(paneId, dbContext);
            }
        }

        public void UpdateMultipleEntitySplitByAndMainForPart(IEnumerable<PartDescriptor> parts)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                foreach (var part in parts)
                {
                    var partToUpdate = dbContext.Parts
                        .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                        .Single(p => p.Id == part.Id);

                    partToUpdate.MultipleEntitySplitByAndMain = part.MultipleEntitySplitByAndFilterBy;
                    dbContext.Parts.Update(partToUpdate);
                }
                dbContext.SaveChanges();
            }
        }

        private DbPart ConvertToDbPart(PartDescriptor part)
        {
            if (part.Disabled)
            {
                throw new Exception("Storing disabled Parts in SQL is not currently supported.");
            }

            return new DbPart()
            {
                ProductShortCode = DbFieldConverter.EncodeString(_productContext.ShortCode),
                SubProductId = DbFieldConverter.EncodeString(_productContext.SubProductId),
                PaneId = DbFieldConverter.EncodeString(part.PaneId),
                PartType = DbFieldConverter.EncodeString(part.PartType),
                Spec1 = DbFieldConverter.EncodeString(part.Spec1),
                Spec2 = DbFieldConverter.EncodeString(part.Spec2),
                Spec3 = DbFieldConverter.EncodeString(part.Spec3),
                DefaultSplitBy = DbFieldConverter.EncodeString(part.DefaultSplitBy),
                HelpText = DbFieldConverter.EncodeString(part.HelpText),
                DefaultAverageId = DbFieldConverter.EncodeString(part.DefaultAverageId),
                AutoMetrics = DbFieldConverter.EncodeArrayOfStrings(part.AutoMetrics),
                AutoPanes = DbFieldConverter.EncodeArrayOfStrings(part.AutoPanes),
                Ordering = DbFieldConverter.EncodeArrayOfStrings(part.Ordering),
                OrderingDirection = DbFieldConverter.EncodeDataSortOrder(part.OrderingDirection),
                Colours = DbFieldConverter.EncodeArrayOfStrings(part.Colours),
                Filters = DbFieldConverter.EncodeArrayOfStrings(part.Filters),
                XRange = DbFieldConverter.EncodeAxisRange(part.XAxisRange),
                YRange = DbFieldConverter.EncodeAxisRange(part.YAxisRange),
                Sections = DbFieldConverter.EncodeArrayOfStringArrays(part.Sections),
                Breaks = part.Breaks ?? Array.Empty<CrossMeasure>(),
                OverrideReportBreaks = part.OverrideReportBreaks,
                ShowTop = part.ShowTop,
                MultipleEntitySplitByAndMain = part.MultipleEntitySplitByAndFilterBy,
                ReportOrder =  part.ReportOrder,
                BaseExpressionOverride = part.BaseExpressionOverride,
                Waves = part.Waves,
                SelectedEntityInstances = part.SelectedEntityInstances,
                AverageType = part.AverageTypes,
                MultiBreakSelectedEntityInstance = part.MultiBreakSelectedEntityInstance,
                DisplayMeanValues = part.DisplayMeanValues,
                DisplayStandardDeviation = part.DisplayStandardDeviation,
                CustomConfigurationOptions = part.CustomConfigurationOptions,
                ShowOvertimeData = part.ShowOvertimeData,
                HideDataLabels = part.HideDataLabels,
            };
        }

        private static PartDescriptor ConvertToPartDescriptor(DbPart dbPart)
        {
            return new PartDescriptor(dbPart.Id)
            {
                // fake id is a legacy of loading definitions from map files
                // it will be removed when map file configuration of pages/panes/parts is removed
                FakeId = Guid.NewGuid().ToString(),
                PaneId = dbPart.PaneId,
                PartType = dbPart.PartType,
                Spec1 = dbPart.Spec1 ?? string.Empty, // Front end relies on empty strings for spec columns
                Spec2 = dbPart.Spec2 ?? string.Empty,
                Spec3 = dbPart.Spec3 ?? string.Empty,
                DefaultSplitBy = dbPart.DefaultSplitBy ?? string.Empty,
                HelpText = dbPart.HelpText ?? string.Empty,
                DefaultAverageId = dbPart.DefaultAverageId,
                Disabled = false,
                AutoMetrics = DbFieldConverter.DecodeArrayOfStrings(dbPart.AutoMetrics),
                AutoPanes = DbFieldConverter.DecodeArrayOfStrings(dbPart.AutoPanes),
                Ordering = DbFieldConverter.DecodeArrayOfStrings(dbPart.Ordering),
                OrderingDirection = DbFieldConverter.DecodeDataSortOrder(dbPart.OrderingDirection),
                Colours = DbFieldConverter.DecodeArrayOfStrings(dbPart.Colours),
                Filters = DbFieldConverter.DecodeArrayOfStrings(dbPart.Filters),
                XAxisRange = DbFieldConverter.DecodeAxisRange(dbPart.XRange),
                YAxisRange = DbFieldConverter.DecodeAxisRange(dbPart.YRange),
                Sections = DbFieldConverter.DecodeArrayOfStringArrays(dbPart.Sections),
                Breaks = dbPart.Breaks ?? Array.Empty<CrossMeasure>(),
                OverrideReportBreaks = dbPart.OverrideReportBreaks,
                ShowTop = dbPart.ShowTop,
                MultipleEntitySplitByAndFilterBy = dbPart.MultipleEntitySplitByAndMain,
                ReportOrder = dbPart.ReportOrder,
                BaseExpressionOverride = dbPart.BaseExpressionOverride,
                Waves = dbPart.Waves,
                SelectedEntityInstances = dbPart.SelectedEntityInstances,
                AverageTypes = dbPart.AverageType,
                MultiBreakSelectedEntityInstance = dbPart.MultiBreakSelectedEntityInstance,
                DisplayMeanValues = dbPart.DisplayMeanValues,
                CustomConfigurationOptions = dbPart.CustomConfigurationOptions,
                ShowOvertimeData = dbPart.ShowOvertimeData,
                HideDataLabels = dbPart.HideDataLabels,
                DisplayStandardDeviation = dbPart.DisplayStandardDeviation
            };
        }
    }
}
