using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VueReporting.Models;
using VueReporting.Services;

namespace VueReportingTests
{
    public class TestChartExporterService : IBrandVueService
    {
        public string[] GetBrandSetNames(string root, string subsetId)
        {
            throw new NotImplementedException();
        }

        public EntityInstance[] GetWholeMarket(string root, string subset)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<EntitySet> GetBrandSets(string root, string subsetId)
        {
            throw new NotImplementedException();
        }

        public EntitySet GetBrandSet(string brandSetName, string root, string subsetId)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> ExportChart(Uri url, string viewType, string name, int width, int height, string[] metrics, string root)
        {
            return await Task.FromResult(File.OpenRead("TestChart.png"));
        }

        public Uri GetUrlFromBookmark(string metaAppBase, string metaBookmark, string root)
        {
            throw new NotImplementedException();
        }

        public Uri UrlForBookmark(string appBase, string bookmark, string root)
        {
            throw new NotImplementedException();
        }

        public Uri AllVueUrlForBookmark(string appBase, string bookmark, string root)
        {
            throw new NotImplementedException();
        }
    }
}
