namespace BrandVue.EntityFramework.MetaData
{
    public interface IBookmarkRepository
    {
        Uri GetRedirectUrl(string bookmarkGuid);
        string GenerateRedirectFromUrl(string appBase, string url, string userName, string requestIdAddress);
    }
}