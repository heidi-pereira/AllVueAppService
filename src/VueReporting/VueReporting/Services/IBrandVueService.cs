using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VueReporting.Models;

namespace VueReporting.Services
{
    public interface IBrandVueService
    {
        EntityInstance[] GetWholeMarket(string root, string subset);
        IReadOnlyCollection<EntitySet> GetBrandSets(string root, string subsetId);
        Task<Stream> ExportChart(Uri url, string viewType, string name, int width, int height, string[] metrics, string root);
        Uri GetUrlFromBookmark(string appBase, string bookmark, string root);
        Uri UrlForBookmark(string appBase, string bookmark, string root);
        Uri AllVueUrlForBookmark(string appBase, string bookmark, string root);
    }
}
