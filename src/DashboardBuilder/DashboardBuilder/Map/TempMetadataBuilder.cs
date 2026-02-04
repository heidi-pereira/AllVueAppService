using System.IO;
using System.Threading.Tasks;
using DashboardBuilder.Core;
using DashboardMetadataBuilder;
using Microsoft.Extensions.Logging;

namespace DashboardBuilder.Map
{
    internal class TempMetadataBuilder : ITempMetadataBuilder
    {
        private readonly ILoggerFactory _loggerFactory;

        public TempMetadataBuilder(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public async Task BuildToTempMetadataFolder(string mapFilePath, DirectoryInfo outputPath)
        {
            var egnyteRoot = new FileInfo(mapFilePath).Directory.Parent.FullName;
            var actions = DashboardActions.Create("1.0.0-temp", new TempMetadataAppSettings(egnyteRoot, outputPath.FullName),
                _loggerFactory);
            await actions.Build(mapFilePath);
        }

        public bool IsBrandVue(string mapFilePath)
        {
            return MapSettings.LoadMinimalSettings(mapFilePath).BuildSettings.BuildType == DashboardBuildType.BrandVueRepeatingSections;
        }

        private class TempMetadataAppSettings : IAppSettings
        {
            public TempMetadataAppSettings(string egnyteReadOnlyRoot, string overrideOutputPath)
            {
                EgnyteReadOnlyRoot = egnyteReadOnlyRoot;
                OverrideOutputPath = overrideOutputPath;
            }

            public string EgnyteReadOnlyRoot { get; }
            public string OverrideOutputPath { get; }
            public bool PackageOutput { get; } = false;
        }
    }
}