using System.IO;

namespace DashboardBuilder.Core
{
    internal class OutputPaths
    {
        private readonly IMapSettings _mapSettings;

        public OutputPaths(IMapSettings mapSettings, string overrideOutputPath,
            bool appSettingsPackageOutput)
        {
            _mapSettings = mapSettings;

            General = GetOutPath(overrideOutputPath);
            Metadata = !appSettingsPackageOutput
                ? General
                : Path.Combine(General, "metadata");
            Config = Path.Combine(Metadata, "config");
            Directory.CreateDirectory(General);
            Directory.CreateDirectory(Config);
        }

        public string General { get; set; }
        public string Metadata { get; set; }
        public string Config { get; }
        private string GetOutPath(string overrideOutputPath)
        {
            var outPath = !string.IsNullOrWhiteSpace(overrideOutputPath) 
                ? overrideOutputPath 
                : Path.Combine(Path.GetTempPath(), "DashboardBuilder", Path.GetRandomFileName().Substring(0, 8));
            return Path.Combine(outPath, _mapSettings.ShortCode);
        }
    }
}