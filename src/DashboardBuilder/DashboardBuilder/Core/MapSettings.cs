using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Aspose.Cells;
using DashboardBuilder.AsposeHelper;
using DashboardMetadataBuilder.MapProcessing.Definitions;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardBuilder.Core
{
    internal enum DashboardBuildType
    {
        None = 0,
        BrandVueRepeatingSections,
        Other,
    }

    internal class BuildSettings
    {
        public DashboardBuildType BuildType { get; }
        public Specifics Specifics { get; }
        public bool IncludeData { get; }
        public bool IncludeDontKnowsForProfileFields { get; }
        public DateTime? StopBuildingDate;

        public BuildSettings(DashboardBuildType buildType, Specifics specifics, bool includeData, bool includeDontKnowsForProfileFields, DateTime? stopBuildingDate)
        {
            BuildType = buildType;
            Specifics = specifics;
            IncludeData = includeData;
            IncludeDontKnowsForProfileFields = includeDontKnowsForProfileFields;
            StopBuildingDate = stopBuildingDate;
        }
    }

    internal class MapSettings : IMapSettings
    {
        public Workbook Map { get; }
        public IReadOnlyDictionary<string, string> Settings { get; private set; }
        public string ShortCode { get; private set; }
        /// <summary>
        /// Enables fields to be generated for unused questions in brandvue and metrics to be generated for unused fields
        /// </summary>
        public bool GenerateFromAnswersTable { get; private set; }

        public string SourceDb { get; private set; }
        public bool IgnoreForAnyAction { get; set; }

        public IReadOnlyCollection<MapSubset> MapSubsets { get; private set; }

        public BuildSettings BuildSettings { get; private set; }
        public string MapFilePath { get; }

        private MapSettings(Workbook map, string mapFilePath)
        {
            MapFilePath = mapFilePath;
            Map = map;
        }

        public static MapSettings LoadFromFile(string mapFileName)
        {
            var map = AsposeCellsHelper.OpenWorkbook(mapFileName);
            var mapSettings = new MapSettings(map, mapFileName);
            mapSettings.Populate(map);
            return mapSettings;
        }

        public static MapSettings LoadMinimalSettings(string mapFileName)
        {
            var map = AsposeCellsHelper.OpenWorkbook(mapFileName);
            var mapSettings = new MapSettings(map, mapFileName);
            mapSettings.Populate(map);
            return mapSettings;
        }

        public static string GetMapFilePath(string directoryPath)
        {
            var overridePath = Path.Combine(directoryPath, "Map-override.xlsx");
            return File.Exists(overridePath) ? overridePath : Path.Combine(directoryPath, "Map.xlsx");
        }

        private void Populate(Workbook map)
        {
            Settings = new TypedWorksheet<Settings>(map).Rows.Where(r => string.IsNullOrEmpty(r.Environment))
                .ToDictionary(r => r.Setting, r => r.Value, StringComparer.OrdinalIgnoreCase);
            SourceDb = GetString("Source database");
            ShortCode = GetString("Short code").Replace(" ", "_").Replace("-", "_");
            GenerateFromAnswersTable = GetBoolean("GenerateFromAnswersTable", false);

            BuildSettings = new BuildSettings(DashboardBuildType.BrandVueRepeatingSections,
                GetEnum("DashboardBuilderSpecifics", Specifics.None),
                GetBoolean("DashboardBuilderIncludeData", true),
                GetBoolean("IncludeDontKnowsForProfileFields", false),
                GetOptionalDate("StopBuildingAfter"));
            if (TryGetSubSets(map, out var subsets))
            {
                AvailableSubsets = new List<SubsetsIdOnly>(subsets.Rows);
            }
            else
            {
                AvailableSubsets = new List<SubsetsIdOnly>();
            }

            MapSubsets = GetMapSubsets(map);
        }

        private static IReadOnlyCollection<MapSubset> GetMapSubsets(Workbook map)
        {
            bool hasSubsets = TypedWorksheet<SubsetsIdOnly>.TryGet(map, out var subsets);
            return hasSubsets
                ? subsets.Rows.Select(r => new MapSubset(r.Id)).ToArray()
                : new[] { MapSubset.SingleDefaultSubset };
        }

        private DateTime? GetOptionalDate(string key)
        {
            DateTime? date = null;
            if (Settings.TryGetValue(key, out var stringDate)
                && DateTime.TryParse(stringDate, out var parsedDate))
            {
                date = parsedDate;
            }
            return date;
        }

        private string GetString(string key, string defaultValue = "")
        {
            return Settings.TryGetValue(key, out var result) ? result : defaultValue;
        }

        private T GetEnum<T>(string settingName, T defaultValue)  where T : struct 
        {
            T result = defaultValue;
            if (Settings.TryGetValue(settingName, out var val))
            {
                if (!Enum.TryParse(val, true, out result))
                {
                    throw new InvalidEnumArgumentException($"Unable to convert '{val}' into Enum {typeof(T).Name}. Read from Setting {settingName}");
                }
            }
            return result;
        }

        private bool GetBoolean(string settingName, bool defaultValue)
        {
            bool result = defaultValue;
            if (Settings.TryGetValue(settingName, out var val))
            {
                if (!bool.TryParse(val, out result))
                {
                    result = defaultValue;
                }
            }
            return result;
        }

        private static bool TryGetSubSets(Workbook map, out TypedWorksheet<SubsetsIdOnly> subsets)
        {
            return TypedWorksheet<SubsetsIdOnly>.TryGet(map, out subsets);
        }

        public IEnumerable<SubsetsIdOnly> AvailableSubsets { get; private set; }
    }
}