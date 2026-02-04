using System;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using Microsoft.Extensions.Logging;

namespace TestCommon.DataPopulation
{
    public class TestLazyDataLoaderFactory : ILazyDataLoaderFactory
    {
        private readonly ILazyDataLoader _dataLoader;
        private readonly IBrandVueDataLoaderSettings _settings;

        public TestLazyDataLoaderFactory(IBrandVueDataLoaderSettings settings, ILazyDataLoader dataLoader = null)
        {
            _settings = settings;
            _dataLoader = dataLoader;
        }

        public ILazyDataLoader Build(DateTimeOffset? lastSignOffDate, ILogger logger, IProductContext productContext, int[] surveyIds)
        {
            return _dataLoader ?? new FileLazyDataLoader(_settings);
        }

    }
}