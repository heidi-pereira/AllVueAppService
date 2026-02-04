using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BrandVueBuilder;
using DashboardBuilder.Core;
using DashboardBuilder.Map;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace DashboardBuilder
{
    internal class DashboardActions
    {
        private readonly Packager _packager;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IAppSettings _appSettings;

        public static DashboardActions Create(string packageVersion, IAppSettings appSettings, ILoggerFactory loggerFactory)
        {
            return new DashboardActions(packageVersion, appSettings, loggerFactory);
        }

        public DashboardActions(string packageVersion, IAppSettings appSettings, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _appSettings = appSettings;
            if (_appSettings.PackageOutput)
            {
                _packager = new Packager(packageVersion, loggerFactory);
            }

            _logger = loggerFactory.CreateLogger<DashboardActions>();
        }

        public async Task<string> Build(string fullPathToMapFile)
        {
            var mapSettings = MapSettings.LoadFromFile(fullPathToMapFile);
            var settings = new DashboardBuildSettings(_appSettings.EgnyteReadOnlyRoot, fullPathToMapFile);

            switch (mapSettings.BuildSettings.BuildType)
            {
                case DashboardBuildType.BrandVueRepeatingSections:
                    var outputPath = new OutputPaths(mapSettings, _appSettings.OverrideOutputPath, _appSettings.PackageOutput);
                    return await BuildBrandVueRepeatingSections(BrandVueProductSettings.FromMapSettings(mapSettings), outputPath, mapSettings, settings);
                default:
                    _logger.LogInformation("Ignoring Dashboard: " + fullPathToMapFile);
                    break;
            }
            return "";
        }

        private async Task<string> BuildBrandVueRepeatingSections(IBrandVueProductSettings brandVueProductSettings, OutputPaths outputPath, MapSettings mapSettings, DashboardBuildSettings buildSettings)
        {
            var timer = Stopwatch.StartNew();
            _logger.LogInformation("Dashboard (BrandVue) V2 build started");

            if (!MapFileValidation.IsStaticallyValid(brandVueProductSettings.Map, _logger))
            {
                return "Map file invalid";
            }
            
            var brandVueBuilderAppSettings = new BrandVueBuilderAppSettings
                (new BrandViewMetaBuilderAppSettings(outputPath.Config, 
                    outputPath.Metadata, 
                    buildSettings.SourceFolder, 
                    buildSettings.BaseFolder, 
                    mapSettings.GenerateFromAnswersTable));

            var brandVueBuilder = new BrandVueBuilder.BrandVueBuilder(brandVueBuilderAppSettings, brandVueProductSettings, _loggerFactory);

            brandVueBuilder.Build();

            await CreateAndPushPackage(outputPath, mapSettings.BuildSettings.Specifics);

            var time = timer.ElapsedMilliseconds;
            var timeFormat = Strings.Format(time / 1000, "#,##0") + "s";
            _logger.LogInformation("Dashboard V2 build finished: " + timeFormat);

            return "";
        }

        private async Task CreateAndPushPackage(OutputPaths outputPaths, Specifics specifics)
        {
            if (_packager != null)
            {
                await _packager.CreateAndPush(new DirectoryInfo(outputPaths.General), specifics == Specifics.BrandVue);
            }
        }

        public void LogError(string message, string stackTrace, string specificDashboard, string description)
        {
            _logger.LogError($"{specificDashboard} {description}:\r\n{message}\r\n{stackTrace}");
        }
    }
}
