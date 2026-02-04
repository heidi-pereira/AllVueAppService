using System.IO;

namespace BrandVue.SourceData.Import
{
    public class MetadataPaths
    {
        private readonly AppSettings _appSettings;

        public MetadataPaths(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public string Base => _appSettings.GetRootedPathWithProductNameReplaced(nameof(Base) + "metadataPath");
        public string DashPages => GetMetadataPath(nameof(DashPages));
        public string DashPanes => GetMetadataPath(nameof(DashPanes));
        public string Subsets => GetMetadataPath(nameof(Subsets));
        public string Surveys => GetMetadataPath(nameof(Surveys));
        public string Averages => GetMetadataPath(nameof(Averages));
        public string Measures => GetMetadataPath(nameof(Measures));
        public string Settings => GetMetadataPath(nameof(Settings));
        public string Filters => GetMetadataPath(nameof(Filters));
        public string ProfilingFields => GetMetadataPath(nameof(ProfilingFields));
        public string DashParts => GetMetadataPath(nameof(DashParts));
        public string ResponseEntityTypes => GetMetadataPath(nameof(ResponseEntityTypes));
        public string FieldDefinitions => GetMetadataPath(nameof(FieldDefinitions));

        private string GetMetadataPath(string settingPrefix)
        {
            return Path.Combine(Base, _appSettings.GetSetting($"{settingPrefix}metadataFilename"));
        }
    }

}