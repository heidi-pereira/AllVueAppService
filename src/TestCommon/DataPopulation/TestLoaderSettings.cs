using BrandVue.EntityFramework;
using BrandVue.SourceData.Import;

namespace TestCommon.DataPopulation
{
    public static class TestLoaderSettings
    {
        public static ConfigurationSourcedLoaderSettings Default { get; } = WithProduct("Test.barometer");

        public static ConfigurationSourcedLoaderSettings EatingOut { get; } = WithProduct("Test.Eatingout");

        private static AppSettings WithProduct(this AppSettings appSettings, string productName)
        {
            appSettings.AppSettingsCollection["ProductsToLoadDataFor"] = productName;
            return new AppSettings(appSettingsCollection: appSettings.AppSettingsCollection);
        }
        /// <summary>
        /// Loads CSVs from productName di
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        public static ConfigurationSourcedLoaderSettings WithProduct(string productName)
        {
            return new ConfigurationSourcedLoaderSettings(new AppSettings().WithProduct(productName));
        }
    }
}
