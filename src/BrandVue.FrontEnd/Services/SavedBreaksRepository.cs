using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.Services
{
    public interface ISavedBreaksRepository
    {
        IReadOnlyCollection<SavedBreakCombination> GetForCurrentUser();
        IReadOnlyCollection<SavedBreakCombination> GetAllForSubProduct();
        SavedBreakCombination GetBreakByName(string name);
        void Create(SavedBreakCombination breaks);
        void Update(SavedBreakCombination breaks);
        void Delete(int savedBreaksId);
        SavedBreakCombination GetById(int savedBreaksId);
    }

    public class SavedBreaksRepository : ISavedBreaksRepository
    {
        private readonly IProductContext _productContext;
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IUserContext _userContext;

        public SavedBreaksRepository(
            IProductContext productContext,
            IDbContextFactory<MetaDataContext> dbContextFactory,
            IUserContext userContext)
        {
            _productContext = productContext;
            _dbContextFactory = dbContextFactory;
            _userContext = userContext;
        }

        private IQueryable<SavedBreakCombination> GetSavedBreaks(MetaDataContext context) =>
            context.SavedBreaks.Where(b =>
                b.ProductShortCode == _productContext.ShortCode &&
                b.SubProductId == _productContext.SubProductId);

        private IQueryable<SavedBreakCombination> GetSavedBreaksForCurrentUser(MetaDataContext context) =>
            GetSavedBreaks(context).Where(b =>
                (b.AuthCompanyShortCode == null || b.AuthCompanyShortCode == _userContext.AuthCompany) &&
                (b.IsShared || b.CreatedByUserId == _userContext.UserId));

        private IQueryable<SavedBreakCombination> GetSavedBreaksByName(MetaDataContext context, string name) =>
            GetSavedBreaks(context).Where(b => b.Name.ToLower() == name.ToLower());

        public IReadOnlyCollection<SavedBreakCombination> GetAllForSubProduct()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetSavedBreaks(dbContext).AsNoTracking().ToArray();
        }

        public IReadOnlyCollection<SavedBreakCombination> GetForCurrentUser()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetSavedBreaksForCurrentUser(dbContext).AsNoTracking().ToArray();
        }

        public SavedBreakCombination GetBreakByName(string name)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetSavedBreaksByName(dbContext, name).AsNoTracking().FirstOrDefault();
        }

        public void Create(SavedBreakCombination breaks)
        {
            Validate(breaks);
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.SavedBreaks.Add(breaks);
            dbContext.SaveChanges();
        }

        public void Update(SavedBreakCombination breaks)
        {
            Validate(breaks);
            using var dbContext = _dbContextFactory.CreateDbContext();

            var existing = GetSavedBreaks(dbContext).AsNoTracking().SingleOrDefault(b => b.Id == breaks.Id);
            if (existing == null)
            {
                throw new BadRequestException("Could not find matching Saved Breaks");
            }

            dbContext.SavedBreaks.Update(breaks);
            dbContext.SaveChanges();
        }

        public SavedBreakCombination GetById(int savedBreaksId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetSavedBreaks(dbContext).AsNoTracking().Single(b => b.Id == savedBreaksId);
        }

        public void Delete(int savedBreaksId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existing = GetSavedBreaks(dbContext).SingleOrDefault(b => b.Id == savedBreaksId);
            if (existing != null)
            {
                dbContext.SavedBreaks.Remove(existing);
                dbContext.SaveChanges();
            }
        }

        private void Validate(SavedBreakCombination savedBreaks)
        {
            savedBreaks.ProductShortCode = _productContext.ShortCode;
            savedBreaks.SubProductId = _productContext.SubProductId;
            if (string.IsNullOrWhiteSpace(savedBreaks.AuthCompanyShortCode))
            {
                savedBreaks.AuthCompanyShortCode = null;
            }
            if (savedBreaks.Breaks == null || savedBreaks.Breaks.Count == 0)
            {
                throw new BadRequestException("Cannot save empty set of breaks");
            }
            if (string.IsNullOrWhiteSpace(savedBreaks.Name))
            {
                throw new BadRequestException("Must set a name for the saved breaks");
            }
        }
    }
}