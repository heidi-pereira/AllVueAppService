using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DashboardBuilder.Core;
using DashboardBuilder.Helper;
using DashboardBuilder.Map;
using DashboardMetadataBuilder;
using Microsoft.Extensions.Logging;
using Savanta.Logging;
using static System.String;

namespace DashboardBuilder
{
    internal class ByMapFile : IComparer<MapSettings>
    {
        readonly CaseInsensitiveComparer _caseiComp = new CaseInsensitiveComparer();

        private static readonly Dictionary<DashboardBuildType, int> BuildTypePriorities = new Dictionary<DashboardBuildType, int>
        {
            { DashboardBuildType.BrandVueRepeatingSections, 2 },
            { DashboardBuildType.Other, 4 },
            { DashboardBuildType.None, 5 }
        };

        public int Compare(MapSettings x, MapSettings y)
        {
            if (x == null || y == null)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                if (x == null)
                    return -1;
                return 1;
            }
            //In general myVue should go first
            //Because some myVue builds upload data to clients (eg Prezzo and these need to go at a specific time....
            //
            if (x.BuildSettings.BuildType == y.BuildSettings.BuildType)
            {
                if (x.AvailableSubsets != null && y.AvailableSubsets != null)
                {
                    var compare = x.AvailableSubsets.Count() - y.AvailableSubsets.Count();
                    if (compare != 0)
                        return compare;
                }
                return _caseiComp.Compare(x.ShortCode, y.ShortCode);
            }
            return BuildTypePriorities[x.BuildSettings.BuildType] - BuildTypePriorities[y.BuildSettings.BuildType];
        }
    }

    public partial class Program
    {
        private const string PackageVersionSpecifier = "-PackageVersion=";

        public static async Task<int> Main(string[] args)
        {
            using (var loggerFactory = SavantaLogging.CreateFactory())
            {
                var logger = loggerFactory.CreateLogger<Program>();
                try
                {
                    if (args.Length < 1)
                    {
                        throw new DisplayHelpException();
                    }

                    switch (args[0].ToLowerInvariant())
                    {
                        case "list":
                            return ListMapFiles(loggerFactory);

                        case "build":
                            return await BuildMapFilesByShortCode(args, loggerFactory);

                        case "validate":
                            return await ValidateMapFile(args, loggerFactory);

                        case "packagemapdirectories":
                            return await PackageMapDirectories(args, loggerFactory);

                        case "custom":
                            return CustomAction(args);

                        case "help":
                        case "usage":
                        case "/?":
                            throw new DisplayHelpException();
                        default:
                            throw new DisplayHelpException($"Unknown command '{args[0]}'");
                    }
                }
                catch (DisplayHelpException ex)
                {
                    DisplayHelp(ex, loggerFactory);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception on main path for command: {1}", Join(" ", args));
                }
                return -1;
            }
        }

        private static int ListMapFiles(ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Program.ListMapFiles");
            var shortCodes = CreateLookupFromShortCodeToMapFilePath(new AppSettings(), loggerFactory);
            foreach (var mapFile in new SortedSet<MapSettings>(shortCodes.Values, new ByMapFile()))
            {
                logger.LogInformation($"Shortcode '{mapFile.ShortCode}'=>{mapFile.MapFilePath} ({mapFile.AvailableSubsets.Count()})");
            }
            return 0;
        }


        private static async Task<int> ValidateMapFile(string[] args, ILoggerFactory loggerFactory)
        {
            if (args.Length < 2)
                throw new DisplayHelpException();

            var mapfilePath = args[1];
            if (File.Exists(mapfilePath))
            {
                var tempMetadataBuilder = new TempMetadataBuilder(loggerFactory);
                var isStaticallyValid = await MapFileValidation.ValidateMetadataBuild(mapfilePath, tempMetadataBuilder, loggerFactory.CreateLogger("MapValidation"));
                return isStaticallyValid ? 0 : -1;
            }
            throw new DisplayHelpException($"File {mapfilePath} does not exist");
        }


        private static async Task<int> PackageMapDirectories(string[] args, ILoggerFactory loggerFactory)
        {
            if (args.Length > 4)
                throw new DisplayHelpException();

            string commaSeparatedEgnyteBearerTokens = args.ElementAtOrDefault(1) ?? ConfigurationManager.AppSettings["Egnyte.BearerToken"];
            string outputBaseDirectory = args.ElementAtOrDefault(2) ?? "../../../LatestMetadata";
            string packageId = args.ElementAtOrDefault(3) ?? "DashboardBuilder.Metadata";

            var tempMetadataBuilder = new TempMetadataBuilder(loggerFactory);

            var smtpClientCredentials = new NetworkCredential(Environment.GetEnvironmentVariable("PackageMapDirectories.ExchangeUser"), Environment.GetEnvironmentVariable("PackageMapDirectories.ExchangePassword"));
            var dispatcher = new Dispatcher(loggerFactory, tempMetadataBuilder, smtpClientCredentials);

            await dispatcher.CreateFromEgnyte(commaSeparatedEgnyteBearerTokens, outputBaseDirectory, packageId);

            return 0;
        }

        private static Dictionary<string, MapSettings> CreateLookupFromShortCodeToMapFilePath(AppSettings appSettings, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Program.CreateLookupFromShortCodeToMapFilePath");
            var result = new Dictionary<string, MapSettings>(StringComparer.OrdinalIgnoreCase);

            foreach (var mapFile in MapFileLocator.LocateMapFiles(appSettings.EgnyteReadOnlyRoot))
            {
                var mapSettings = LoadMinimalMapSettingsOrNull(mapFile, logger);
                if (mapSettings == null) continue;
                switch (mapSettings.BuildSettings.BuildType)
                {
                    case DashboardBuildType.BrandVueRepeatingSections:
                        if (mapSettings.BuildSettings.StopBuildingDate.HasValue &&
                            mapSettings.BuildSettings.StopBuildingDate.Value.AddDays(1) < DateTime.Now)
                        {
                            logger.LogInformation(
                                $"Not including '{mapSettings.ShortCode}', because the map file {mapFile} settings page specifies not to build after {mapSettings.BuildSettings.StopBuildingDate.Value.Date:yy-MM-dd}");
                            mapSettings.IgnoreForAnyAction = true;
                        }

                        result[mapSettings.ShortCode] = mapSettings;
                        break;
                    default:
                        logger.LogWarning(
                            $"Ignoring map file {mapFile} as it has type {mapSettings.BuildSettings.BuildType}");
                        break;
                }
            }
            return result;
        }

        private static MapSettings LoadMinimalMapSettingsOrNull(string mapFile, ILogger logger)
        {
            try
            {
                return MapSettings.LoadMinimalSettings(mapFile);
            }
            catch (Exception e)
            {
                logger.LogError($"Error accessing map '{mapFile}', skipping", e);
                return null;
            }
        }

        private static async Task<int> ActionWithMapFile(string[] args, Func<SortedSet<MapSettings>, string, AppSettings, ILoggerFactory, Task<int>> func, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Program.BuildMapFilesByShortCode");
            string packageVersion = "1.0.0-dev";
            var sortedMapFiles = new SortedSet<MapSettings>(new ByMapFile());
            var appSettings = new AppSettings();
            var mapShortCodeToMapFile = CreateLookupFromShortCodeToMapFilePath(appSettings, loggerFactory);
            for (var index = 1; index < args.Length; index++)
            {
                var param = args[index];
                if (param.StartsWith(PackageVersionSpecifier, StringComparison.InvariantCultureIgnoreCase))
                    packageVersion = param.Substring(PackageVersionSpecifier.Length);
                else if (param == "all")
                {
                    foreach (var lookup in mapShortCodeToMapFile)
                    {
                        sortedMapFiles.Add(lookup.Value);
                    }
                }
                else
                {
                    if (mapShortCodeToMapFile.ContainsKey(param))
                    {
                        if(!mapShortCodeToMapFile[param].IgnoreForAnyAction)
                            sortedMapFiles.Add(mapShortCodeToMapFile[param]);
                    }
                    else
                    {
                        logger.LogError($"Ignoring Invalid ShortCode {param} ");
                    }
                }
            }
            return await func(sortedMapFiles, packageVersion, appSettings, loggerFactory);
        }
        private static Task<int> BuildMapFilesByShortCode(string[] args, ILoggerFactory loggerFactory)
        {
            return ActionWithMapFile(args, BuildMapFiles, loggerFactory);
        }

        private static void DisplayHelp(DisplayHelpException ex, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Program.DisplayHelp");
            if (!IsNullOrWhiteSpace(ex.Message))
            {
                logger.LogError(ex.Message);
            }
            Console.WriteLine("Usage examples:");
            Console.WriteLine("DashboardBuilder.exe build EatingOut\t\tBuilds config/data for a BrandVue product dashboard by short code");
            Console.WriteLine("DashboardBuilder.exe build all\t\t\tBuilds all products found in the 'Egnyte.Dashboards.LocalReadOnly' directory specified in app.config");
            Console.WriteLine("DashboardBuilder.exe list\t\t\tLists all the products found in the 'Egnyte.Dashboards.LocalReadOnly' directory specified in app.config");
            Console.WriteLine("DashboardBuilder.exe validate \"c:\\map.xlsx\"\tValidates a map file at the given file path");
            Console.WriteLine("DashboardBuilder.exe ConvertBrandsTabToBrandEntityTab \"c:\\map.xlsx\"\tConverts brands tab to BrandEntity tab");
            Console.WriteLine("DashboardBuilder.exe ConvertBrandsTabToReferenceBrandEntityTab \"c:\\map.xlsx\"\tConverts brands tab to reference BrandEntity tab");
            Console.WriteLine("All build commands use the 'Short code' defined in their map file settings sheet - this is also the same name used in URLs for dashboards");
        }

        private static async Task<int> BuildMapFiles(IEnumerable<MapSettings> mapFiles, string packageVersion, AppSettings appSettings, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Program.BuildMapFiles");
            bool completeSuccess = true;
            foreach (var mapFile in mapFiles)
            {
                var dashboardActions = DashboardActions.Create(packageVersion, appSettings, loggerFactory);
                var mapFilePath = mapFile.MapFilePath;
                try
                {
                    var errorResult = await dashboardActions.Build(mapFilePath);
                    var buildSucceeded = IsNullOrWhiteSpace(errorResult);

                    if (!buildSucceeded)
                    {
                        completeSuccess = false;
                        dashboardActions.LogError(errorResult, "", mapFilePath, "Error building dashboard: ");
                    }

                    NotifyMonitor(mapFile, buildSucceeded, logger);
                }
                catch (Exception ex)
                {
                    completeSuccess = false;
                    try
                    {
                        dashboardActions.LogError(ex.Message, ex.StackTrace, mapFilePath, "Error building dashboard: ");
                        NotifyMonitor(mapFile, false, logger);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed in error handling");
                    }
                }
            }
            return completeSuccess ? 0 : -1;
        }

        private static void NotifyMonitor(MapSettings mapFile, bool success, ILogger logger)
        {
            var tags = new List<string>() { "#BuildFinished", $"#{mapFile.ShortCode}" };
            tags.Add(success ? "#Success" : "#Fail");

            logger.LogInformation(Join(" ", tags));
        }

        private static int CustomAction(string[] args)
        {
            switch (args[1].ToLowerInvariant())
            {
                case "registersnitch": MonitorHelper.RegisterSnitch(args[2], args[3]); break;
                default:
                    throw new ArgumentException($"unknown action: args[1].ToLowerInvariant()");
            }
            return 0;
        }

    }
}
