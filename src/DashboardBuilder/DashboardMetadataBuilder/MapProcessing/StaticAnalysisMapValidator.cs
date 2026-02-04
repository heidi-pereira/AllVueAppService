using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Schema;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing
{
    public class StaticAnalysisMapValidator
    {
        private const string EntitySheetSuffix = "Entity";
        private readonly Workbook _map;

        private readonly Dictionary<SheetType, string[]> _sheetNamesForType = new Dictionary<SheetType, string[]>
        {
            {SheetType.Fields, new[] {SheetNames.ProfileFields, SheetNames.BrandFields, SheetNames.ProfilingFields, SheetNames.Fields}},
            {SheetType.EntityFields, new[] {SheetNames.BrandFields, SheetNames.Fields}},
            {SheetType.Metrics, new[] {SheetNames.Metrics, SheetNames.ProfileMetrics, SheetNames.HardCodedMetrics}},
            {SheetType.Parts, new[] {SheetNames.DashParts}},
            {SheetType.Panes, new[] {SheetNames.DashPanes}},
            {SheetType.Pages, new[] {SheetNames.DashPages}},
            {SheetType.JourneyStages, new[] {SheetNames.JourneyStages}},
            {SheetType.Dishes, new[] {SheetNames.Dishes}},
            {SheetType.Venues, new[] {SheetNames.Venues}},
            {SheetType.Settings, new[] {SheetNames.Settings}},
            {SheetType.Categories, new[] {SheetNames.Categories}},
            {SheetType.Subsets, new[] {SheetNames.Subsets}},
            {SheetType.Entities, new [] {SheetNames.Entities}}

        };

        private enum SheetType
        {
            Metrics,
            Fields,
            EntityFields,
            Parts,
            Panes,
            Pages,
            JourneyStages,
            Dishes,
            Venues,
            Settings,
            Categories,
            Subsets,
            Entities,
        }

        private IEnumerable<SmallAsposeWorksheet> SheetsForType(SheetType sheetType)
        {
            return _sheetNamesForType[sheetType]
                .Where(_workSheet.ContainsKey)
                .Select(n => _workSheet[n]);
        }

        private readonly Dictionary<string, SmallAsposeWorksheet> _workSheet;

        public static MapValidationResult Validate(Workbook map)
        {
            try
            {
                var staticAnalysisMapValidator = new StaticAnalysisMapValidator(map);
                return new MapValidationResult(staticAnalysisMapValidator.GetErrors(), staticAnalysisMapValidator.GetWarnings());
            }
            catch (Exception e)
            {
                return MapValidationResult.ThrewException("loading map file", e);
            }
        }

        private StaticAnalysisMapValidator(Workbook map)
        {
            _map = map;
            _workSheet = GetSheetNames(map)
                .ToDictionary(n => n, n => new SmallAsposeWorksheet(map, n));
        }

        private string[] GetSheetNames(Workbook map)
        {
            var sheetNamesToGet = new HashSet<string>(_sheetNamesForType.Values.SelectMany(x => x));
            return map.Worksheets.Select(x => x.Name).Where(sheetNamesToGet.Contains).ToArray();
        }

        private bool IsBrandVueMapFile
        {
            get
            {
                var dashboardBuilderType = _workSheet[SheetNames.Settings]
                    .GetDistinctValuesForColumns(row => row.ContainsKey("Setting") && row["Setting"] == "DashboardBuilderType", "Value").FirstOrDefault();
                return String.Compare(dashboardBuilderType, "BrandVue", StringComparison.CurrentCultureIgnoreCase) == 0;
            }
        }
        private bool IsNewBrandVueMapFile
        {
            get
            {

                var dashboardBuilderType = _workSheet[SheetNames.Settings]
                    .GetDistinctValuesForColumns(row => row.ContainsKey("Setting") && row["Setting"] == "DashboardBuilderType", "Value").FirstOrDefault();
                return String.Compare(dashboardBuilderType, "BrandVueRepeatingSections", StringComparison.CurrentCultureIgnoreCase) == 0;
            }
        }
        private bool IsValidationDisabled(string feature)
        {
            var featureValue = _workSheet[SheetNames.Settings]
                .GetDistinctValuesForColumns(row => row.ContainsKey("Setting") && row["Setting"] == feature, "Value").FirstOrDefault();
            return String.Compare(featureValue, "FALSE", StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        private List<string> GetWarnings()
        {
            var issues = new string[0]
                    .Concat(GetMultipleDefinitionsIssues(SheetNames.ProfileFields, "Name", "subset"))
                    .Concat(GetMultipleDefinitionsIssues(SheetNames.ProfilingFields, "Name", "subset"))
                ;
            return issues.ToList();
        }
        private List<string> GetErrors()
        {
            string GetRowValue(IDictionary<string, string> row, string key)
            {
                return row.ContainsKey(key) ? row[key] : null;
            }
            bool IsFieldText(IDictionary<string, string> row)
            {
                return GetRowValue(row, "calcType") == "text";
            }

            bool IsMeasureEnabled(IDictionary<string, string> row)
            {
                return GetRowValue(row, "disableMeasure") != "y" && GetRowValue(row, "disableFilter") != "y";
            }

            var skipSpecPartTypes = new[] {"RangeBar", "RangeBarSimple", "Admin"}; //Spec1 is just a title here
            var specialPartTypes = skipSpecPartTypes.Concat(new[] {"RangeBarJourney", "JourneyRank", "CommentsList", "Journey", "ClassChart", "Text", "DishPerformance", "Leaderboard", "Download", "PageLink" });
            var journeyRankValues = new []{"Strengths", "Stärken", "strengths", "Weaknesses", "Schwächen", "weaknesses"}; //dashpartMaker.ts:49
            var commentListValues = new []{"NPS", "Comments", "Kommentare", "Menu performance", "Sat", "Problem_resolution"}; //Unknown, characterizing current behaviour
            var hackedDivId = DistinctValuesForColumns("Title", SheetsForType(SheetType.Panes)).SelectMany(v => v.Split('\'')); //Just anywhere an html div's ids have been hacked in
            var hardCodedMetricIndexes = DistinctValuesForColumns("Index", SheetsForType(SheetType.Metrics))
                .SelectMany(v => v.Split('\'')).ToList();
            var allowedAutoPanes = new[] { 1, 2, 3, 4, 5 }.Select(x => x.ToString()).ToArray();
            var issues =  new string[0]
                .Concat(GetSchemaIssues())
                .Concat(GetReferenceIssues(SheetType.Metrics, "Name", SheetType.Parts, row => !specialPartTypes.Contains(row["PartType"]) && !hardCodedMetricIndexes.Contains(row["Spec1"]), "Spec1"))
                .Concat(GetReferenceIssues(SheetType.JourneyStages, "StageId", SheetType.Parts, row => row["PartType"] == "RangeBarJourney", "Spec2"))
                .Concat(GetReferenceIssues(journeyRankValues, SheetType.Parts, row => row["PartType"] == "JourneyRank", "Spec1"))
                .Concat(GetReferenceIssues(commentListValues, SheetType.Parts, row => row["PartType"] == "CommentsList", "Spec1"))
                .Concat(GetReferenceIssues(SheetType.Parts, "Name", SheetType.Parts, row => row["PartType"] == "Journey" && !hackedDivId.Contains(row["Spec1"]), "Spec1"))
                .Concat(GetReferenceIssues(allowedAutoPanes, SheetType.Parts, row => true, "Autopanes"))
                .Concat(GetReferenceIssues(SheetType.Metrics, "Name", SheetType.Parts, "autometrics"))
                .Concat(GetReferenceIssues(SheetType.Panes, "Id", SheetType.Parts, "PaneId"))
                .Concat(GetReferenceIssues(SheetType.Pages, "Name", SheetType.Panes, "PageName"))
                .Concat(GetReferenceIssues(SheetType.Metrics, "Name", SheetType.Metrics, "MarketAverageBaseMeasureName"))
                .Concat(GetMultipleDefinitionsIssues(SheetNames.Metrics, "name", "subset"))
                .Concat(GetMultipleDefinitionsIssues(SheetNames.DashPages, "name"))
                .Concat(GetUniquenessIssues("id"));
            if (IsBrandVueMapFile || IsNewBrandVueMapFile)
            {
                issues = issues
                    .Concat(GetReferenceIssues(SheetType.Fields, "Name", SheetType.Metrics,IsMeasureEnabled, "Field"))
                    .Concat(GetReferenceIssues(SheetType.Fields, "Name", SheetType.Metrics,IsMeasureEnabled, "Field2"))
                    .Concat(GetReferenceIssues(SheetType.Fields, "Name", SheetType.Metrics,IsMeasureEnabled, "BaseField"))
                    .Concat(GetMultipleSubsetDefinitionsIssues(SheetType.EntityFields, "Name"))
                    .Concat(GetMultipleDefinitionsIssues(SheetNames.Entities, "Type"))
                    .Concat(GetColumnValuesMustExistIssues(SheetType.Entities, "Type", "DisplayNameSingular", "DisplayNamePlural"))
                    .Concat(GetMetricValIssues("trueVals", true))
                    .Concat(GetMetricValIssues("baseVals", false))
                    .Concat(EnsureThatColumnIsNotEmpty(SheetType.Parts, row => !string.IsNullOrWhiteSpace(row["autometrics"]), "autopanes"))
                ;
            }

            if (IsNewBrandVueMapFile)
            {
                issues = issues
                    .Concat(GetReferenceIssues(SheetType.Fields, "Name", SheetType.Categories, row => !IsFieldText(row) && IsMeasureEnabled(row), "FieldName"))
                    .Concat(GetTabReferenceIssues(name => name.EndsWith(EntitySheetSuffix), name => name.Substring(0, name.Length - 6), SheetType.Entities, "Type"))
                    ;

            }

            issues = issues
                .Concat(GetBrandFieldTypeIssues())
                .Concat(GetProfileFieldTypeIssues());

            return issues.ToList();
        }

        private IEnumerable<string> GetMetricValIssues(string metricValColumnName, bool splitPipeSupported)
        {
            var errorsFound = new List<string>();
            const string sheetName = "Metrics";
            if (_workSheet.ContainsKey(sheetName))
            {
                var sheet = _workSheet[sheetName];
                if (sheet.HasColumn(metricValColumnName))
                {
                    var allowedSplitPipes = splitPipeSupported ? 1 : 0;
                    foreach (var row in sheet.Rows)
                    {
                        var metricVal = row[metricValColumnName];
                        var metricValSplitBySplitPipe = metricVal?.Split('¦');
                        if (metricValSplitBySplitPipe != null && (metricValSplitBySplitPipe.Length > allowedSplitPipes + 1 || metricValSplitBySplitPipe.Count(MetricValueInvalid) > 0))
                        {
                            errorsFound.Add($"Sheet {sheetName} column {metricValColumnName} row {row.Values.First()} value '{metricVal} 'is not a valid value");
                        }
                    }
                }
            }

            return errorsFound;
        }

        private static bool MetricValueInvalid(string splitMetricVal) =>
            !string.IsNullOrEmpty(splitMetricVal) && !IsArrayOfIntegers(splitMetricVal) && !IsNumber(splitMetricVal) && !IsRangeOfIntegers(splitMetricVal);

        private static bool IsNumber(string item) =>
            int.TryParse(item, out _);

        private static bool IsRangeOfIntegers(string item)
        {
            var range = item.Split('>');
            return range.Length == 2 && range.All(IsNumber);
        }

        private static bool IsArrayOfIntegers(string item) => 
            item.Split(new [] {'|'}, StringSplitOptions.RemoveEmptyEntries).All(IsNumber);

        IEnumerable<string> GetBrandFieldTypeIssues()
        {
            List<string> errorsFound = new List<string>();

            var brandFieldsSheetExists = TypedWorksheet<BrandFields>.TryGet(_map, out var brandFieldsSheet);

            if (brandFieldsSheetExists)
            {
                foreach (var row in brandFieldsSheet.Rows)
                {
                    if (row.Type == null)
                    {
                        errorsFound.Add($"{row.Name} has null {nameof(row.Type)}");
                    }
                    else if (Enum.TryParse<BrandFieldType>(row.Type, true, out _) == false)
                    {
                        errorsFound.Add($"Brand field {row.Name} has incorrect {nameof(row.Type).ToLower()} of {row.Type}, valid types are {BrandFieldTypeHelper.EnumsToString()}");
                    }
                }
            }
            return errorsFound;
        }

        IEnumerable<string> GetProfileFieldTypeIssues()
        {
            List<string> errorsFound = new List<string>();

            var profileFieldsSheetExists = TypedWorksheet<ProfilingFields>.TryGet(_map, out var profilingFieldsSheet);

            if (profileFieldsSheetExists)
            {
                foreach (var row in profilingFieldsSheet.Rows)
                {
                    if (row.Type == null)
                    {
                        errorsFound.Add($"{row.Name} has null {nameof(row.Type)}");
                    }
                    else if (Enum.TryParse<ProfileFieldType>(row.Type, true, out _) == false)
                    {
                        errorsFound.Add($"Profile field {row.Name} has incorrect {nameof(row.Type).ToLower()} of {row.Type}, valid types are {string.Join(", ", Enum.GetNames(typeof(ProfileFieldType)))}");
                    }
                }
            }
            return errorsFound;
        }

        private IEnumerable<string> GetSchemaIssues()
        {
            return new string[0]
                .Concat(GetSheetSchemaIssues<BrandFields>())
                .Concat(GetSheetSchemaIssues<BrandMap>())
                .Concat(GetSheetSchemaIssues<CommentFields>())
                .Concat(GetSheetSchemaIssues<CommentSource>())
                .Concat(GetSheetSchemaIssues<Countries>())
                .Concat(GetSheetSchemaIssues<Dimensions>())
                .Concat(GetSheetSchemaIssues<HoverTags>())
                .Concat(GetSheetSchemaIssues<InteractionMap>())
                .Concat(GetSheetSchemaIssues<MealScores>())
                .Concat(GetSheetSchemaIssues<ProfilingFields>())
                .Concat(GetSheetSchemaIssues<Settings>())
                .Concat(GetSheetSchemaIssues<Segments>())
                .Concat(GetSheetSchemaIssues<Fields>())
                .Concat(OneOftheseSheetsMustExist<Fields, ProfilingFields>())
                ;
        }

        private IReadOnlyCollection<string> GetSheetSchemaIssues<TSheetRow>() where TSheetRow : SheetRow, new()
        {
            var sheetType = typeof(TSheetRow);
            var sheetMustExist = SheetMustExist<TSheetRow>(sheetType);
            try
            {
                if (!sheetMustExist)
                {
                    var unused = TypedWorksheet<TSheetRow>.TryGet(_map, out var unused2);
                }
                else
                {
                    var unused = new TypedWorksheet<TSheetRow>(_map);
                }

                return new string [0];
            }
            catch (Exception e)
            {
                return new[] {$"Error reading sheet {sheetType.GetCustomAttribute<SheetAttribute>().SheetName}: {e.Message}"};
            }
        }

        private IReadOnlyCollection<string> OneOftheseSheetsMustExist<TSheetRow1, TSheetRow2>()
            where TSheetRow1 : SheetRow,new () where TSheetRow2 : SheetRow, new()
        {
            var sheetType1 = typeof(TSheetRow1);
            var sheetType2 = typeof(TSheetRow2);
            try
            {
                var doesSheet1Exist = TypedWorksheet<TSheetRow1>.TryGet(_map, out var unused2);
                var doesSheet2Exist = TypedWorksheet<TSheetRow2>.TryGet(_map, out var unused3);
                if (!doesSheet1Exist && !doesSheet2Exist)
                {
                    return new [] {$"Error either sheet '{sheetType1.GetCustomAttribute<SheetAttribute>().SheetName}' or  '{sheetType2.GetCustomAttribute<SheetAttribute>().SheetName}' must exist"};
                }

                return new string[0];
            }
            catch (Exception e)
            {
                return new[] { $"Error reading sheet {sheetType1.GetCustomAttribute<SheetAttribute>().SheetName}#{sheetType2.GetCustomAttribute<SheetAttribute>().SheetName}: {e.Message}" };
            }
        }

        private static bool SheetMustExist<TSheetRow>(Type sheetType) where TSheetRow : SheetRow, new()
        {
            return sheetType.GetCustomAttribute<SheetAttribute>().MustExist;
        }

        private IEnumerable<string> GetUniquenessIssues(string columnName)
        {
            return _workSheet.Values.Where(s => s.HasColumn(columnName)).SelectMany(sheet => GetUniquenessIssues(sheet, columnName));
        }

        private static IEnumerable<string> GetUniquenessIssues(SmallAsposeWorksheet sheet, string columnName)
        {
            return sheet.GetColumnValues(columnName).GroupBy(s => s).Where(s => s.Count() > 1)
                .Select(s => $"{sheet.Name}.{columnName} contains a duplicate of '{s.Key}");
        }

        private IEnumerable<string> GetReferenceIssues(IReadOnlyCollection<string> acceptableValues, SheetType referencingType, Func<IDictionary<string, string>, bool> rowsWhere, params string[] referencingColumns)
        {
            return GetReferenceIssues(acceptableValues, "[" + string.Join(", ", acceptableValues) + "]", referencingType, rowsWhere, referencingColumns);
        }
        private IEnumerable<string> EnsureThatColumnIsNotEmpty(SheetType referencingType, Func<IDictionary<string, string>, bool> rowsWhere, string referencingColumns)
        {
            var items = SheetsForType(referencingType).ToArray();
            var result = items.SelectMany(referencingSheet =>
            {
                var dataRowsWithPossibleIssues = referencingSheet.Rows.Where(rowsWhere).ToList();
                var badData = new List<string>();
                foreach (var dataRow in dataRowsWithPossibleIssues)
                {
                    if (string.IsNullOrWhiteSpace(dataRow[referencingColumns]))
                    {
                        badData.Add($"Column for {referencingColumns} has no items for {string.Join(",",dataRow)}");
                    }
                }
                return badData;
            }).ToArray();
            return result;
        }

        private IEnumerable<string> GetReferenceIssues(SheetType keyType, string keyColumn, SheetType referencingType, params string[] referencingColumns)
        {
            return GetReferenceIssues(keyType, keyColumn, referencingType, _ => true, referencingColumns);
        }

        private IEnumerable<string> GetReferenceIssues(SheetType keyType, string keyColumn, SheetType referencingType, Func<IDictionary<string, string>, bool> rowsWhere, params string[] referencingColumns)
        {
            var sheetsForType = SheetsForType(keyType).ToList();
            var validIds = DistinctValuesForColumns(keyColumn, sheetsForType);
            var keyColumnDescription = string.Join(" or ", sheetsForType.Select(s => s.Name + "." + keyColumn));
            return GetReferenceIssues(validIds, keyColumnDescription, referencingType, rowsWhere, referencingColumns);
        }

        private IEnumerable<string> GetMultipleDefinitionsIssues(string sheetName, string keyColumn, string categoryName = null)
        {
            var result = new string[0];
            if (_workSheet.ContainsKey(sheetName))
            {
                var sheet = _workSheet[sheetName];
                if (sheet.HasColumn(keyColumn))
                {
                    var useCategoryName = categoryName != null ? sheet.HasColumn(categoryName) : false;
                    var currentItems = new Dictionary<string, List<string>>();
                    var duplicateIds = sheet.GetDistinctValuesForColumns(row =>
                    {
                        var categoryValue = useCategoryName ? row[categoryName] : "";
                        if (!currentItems.ContainsKey(categoryValue))
                        {
                            currentItems[categoryValue] = new List<string>();
                        }

                        var value = row[keyColumn];
                        var alreadyExists = currentItems[categoryValue].Contains(value);
                        currentItems[categoryValue].Add(value);
                        return alreadyExists;
                    }, keyColumn);
                    if (duplicateIds.Any())
                    {
                        result = duplicateIds.Select(x => $"{sheetName} found multiple definition for '{x}'").ToArray();
                    }
                }
                else
                {
                    result = new []{ $"No column '{keyColumn}' found in sheet '{sheet.Name}'" };
                }
            }
            return result;
        }
        private IEnumerable<string> GetMultipleSubsetDefinitionsIssues(SheetType type, string keyColumn)
        {
            var result = new List<string>();
            foreach (var sheetName in _sheetNamesForType[type])
            {
                result.AddRange(GetMultipleSubsetDefinitionsIssues(sheetName, keyColumn));
            }

            return result.ToArray();
        }

        private IEnumerable<string> GetMultipleSubsetDefinitionsIssues(string sheetName, string keyColumn)
        {
            string subsetColumnName = "subset";
            var result = new string[0];
            if (_workSheet.ContainsKey(sheetName))
            {
                var sheet = _workSheet[sheetName];
                if (sheet.HasColumn(keyColumn))
                {
                    var useSubsetName = sheet.HasColumn(subsetColumnName) ;
                    var currentItems = new Dictionary<string, List<string>>();
                    var duplicateIds = sheet.GetDistinctValuesForColumns(row =>
                    {
                        var subsetValues = useSubsetName ? row[subsetColumnName] : "";
                        var subsets = subsetValues.Split('|');
                        var alreadyExists = false;
                        foreach (var subset in subsets)
                        {
                            var value = row[keyColumn];
                            if (!currentItems.ContainsKey(value))
                            {
                                currentItems[value] = new List<string>();
                            }
                            alreadyExists |= currentItems[value].Contains(subset);
                            if (string.IsNullOrEmpty(subset))
                            {
                                alreadyExists |= currentItems[value].Any();
                            }
                            else
                            {
                                alreadyExists |= currentItems[value].Contains(string.Empty);
                            }
                            currentItems[value].Add(subset);
                        }

                        return alreadyExists;
                    }, keyColumn);
                    if (duplicateIds.Any())
                    {
                        result = duplicateIds.Select(x => $"{sheetName} found multiple definition for '{x}'").ToArray();
                    }
                }
                else
                {
                    result = new[] { $"No column '{keyColumn}' found in sheet '{sheet.Name}'" };
                }
            }
            return result;
        }

        private IEnumerable<string> GetTabReferenceIssues(Func<string, bool> filter, Func<string, string> selection, SheetType sheetType, string columnName)
        {
            var entityTypes = _map.Worksheets.Select(x => x.Name).Where(filter).Select(selection);
            foreach (var sheet in SheetsForType(sheetType).ToList())
            {
                if (sheet.HasColumn(columnName))
                {
                    return sheet
                        .GetColumnValues(columnName)
                        .Where(x => !entityTypes.Contains(x))
                        .Select(x =>
                            $"the column {columnName} in {sheet.Name} sheet reference to {x} that has no sheet.");
                }
                else
                {
                    return new[] { $"No column '{columnName}' found in sheet '{sheet.Name}'" };
                }
            }
            return new string[0];
        }

        private IEnumerable<string> GetColumnValuesMustExistIssues(SheetType sheetType, params string[] columnNames)
        {
            foreach (var worksheet in SheetsForType(sheetType))
            {
                foreach (var columnName in columnNames)
                {
                    if (!worksheet.HasColumn(columnName))
                    {
                        yield return $"Column '{columnName}' is missing in sheet '{worksheet.Name}'";
                    }
                    else if (worksheet.GetColumnValues(columnName).Any(v => string.IsNullOrEmpty(v)))
                    {
                        yield return $"Column '{columnName}' has {worksheet.GetColumnValues(columnName).Count(string.IsNullOrEmpty) } empty values in sheet '{worksheet.Name}'   ";
                    }
                }
            }
        }

        private static IEnumerable<string> DistinctValuesForColumns(string keyColumn, IEnumerable<SmallAsposeWorksheet> sheetsForType)
        {
            return sheetsForType.SelectMany(sheet => sheet.GetDistinctValuesForColumns(keyColumn));
        }

        private IEnumerable<string> GetReferenceIssues(IEnumerable<string> validIds, string validIdSourceDescription,
            SheetType referencingType, Func<IDictionary<string, string>, bool> rowsWhere, string[] referencingColumns)
        {
            return SheetsForType(referencingType)
                .SelectMany(referencingSheet =>
                {
                    var distinctValuesForColumns = referencingSheet.GetDistinctValuesForColumns(rowsWhere, referencingColumns);
                    var referenceColumnDescription = string.Join(" or ",
                        referencingColumns.Select(refColumn => referencingSheet.Name + "." + refColumn));
                    return GetInvalidReferences(referenceColumnDescription, validIdSourceDescription, distinctValuesForColumns,
                        new HashSet<string>(validIds));
                });
        }

        private static IEnumerable<string> GetInvalidReferences(string referenceDescription, string keyColumnDescription, IEnumerable<string> allForeignKeyReferences, HashSet<string> allValidKeys)
        {
            if (!allValidKeys.Any()) return new string[0]; //Old maps are missing some pages and have hard coded values instead
            return allForeignKeyReferences.Where(foreignKeyRef => !allValidKeys.Contains(foreignKeyRef))
                .Select(key => $"{referenceDescription} contains '{key}', the closest valid option in {keyColumnDescription} is '{allValidKeys.OrderBy(option => Distance(key, option)).First()}'");
        }

        private static int Distance(string key, string option)
        {
            return MinimumEditDistance.Levenshtein.CalculateDistance(key, option, 1);
        }
    }
}
