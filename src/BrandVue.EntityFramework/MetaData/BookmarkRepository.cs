using System.Linq;
using Microsoft.EntityFrameworkCore;
using shortid;
using shortid.Configuration;

namespace BrandVue.EntityFramework.MetaData
{
    public class BookmarkRepository : IBookmarkRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

        public BookmarkRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public Uri GetRedirectUrl(string bookmarkGuid)
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            var bookmark = ctx.Bookmarks.Find(bookmarkGuid);

            if (bookmark == null)
            {
                return null;
            }

            bookmark.DateLastUsed = DateTime.UtcNow;
            bookmark.UseCount++;
            ctx.SaveChanges();

            return new Uri(bookmark.AppBase + bookmark.Url);
        }

        public string GenerateRedirectFromUrl(string appBase, string url, string userName, string requestIdAddress)
        {
            using var ctx = _dbContextFactory.CreateDbContext();
            var bookmark = ctx.Bookmarks.SingleOrDefault(b=>b.AppBase == appBase && b.Url == url);

            var now = DateTime.UtcNow;

            if (bookmark == null)
            {
                bookmark = new Bookmark
                {
                    Id = ShortId.Generate(new GenerationOptions(){UseNumbers = true, UseSpecialCharacters = false}),
                    AppBase = appBase,
                    Url = url,
                    DateCreated = now,
                    CreatedByUserName = userName,
                    CreatedByIpAddress = requestIdAddress
                };
                ctx.Bookmarks.Add(bookmark);
            }

            bookmark.GenerationCount++;
            bookmark.DateLastGenerated = now;
            ctx.SaveChanges();

            return bookmark.Id;
        }
    }
}