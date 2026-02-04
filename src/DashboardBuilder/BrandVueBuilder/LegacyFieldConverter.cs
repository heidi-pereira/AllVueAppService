using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;
using static System.String;

namespace BrandVueBuilder
{
    internal class LegacyFieldConverter
    {
        private readonly Workbook _mapFile;
        

        public LegacyFieldConverter(Workbook mapFile)
        {
            _mapFile = mapFile;
        }
        public void ConvertFieldsToBrandAndProfileFields()
        {
            var fieldNameToCategory = new TypedWorksheet<FieldCategories>(_mapFile).Rows.ToArray();
            var profilingFields = new List<ProfilingFields>();
            var brandFields = new List<BrandFields>();

            var fields = new TypedWorksheet<Fields>(_mapFile).Rows;

            foreach (var field in fields)
            {
                if (field.IsProfileField())
                {
                    profilingFields.AddField(field, fieldNameToCategory);
                }
                else if (field.IsBrandField())
                {
                    brandFields.AddField(field, fieldNameToCategory);
                }
            }
            GenerateWorkSheet(profilingFields);
            GenerateWorkSheet(brandFields);
        }

        private void GenerateWorkSheet<T>(List<T> fields)
        {
            var sheetType = typeof(T);
            var tableName = sheetType.GetCustomAttribute<SheetAttribute>().SheetName;
            Worksheet result = _mapFile.Worksheets.Add(tableName);
            result.Cells.ImportArray(Headers<T>(),0,0);
            foreach (var field in fields)
            {
                var rowId = result.Cells.MaxRow + 1;
                result.Cells.ImportArray(Data(field), rowId, 0);
            }
        }

        private string[,] Headers<T>()
        {
            var sheetType = typeof(T);
            var columnNames = new List<string>();
            foreach (var fieldInfo in sheetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.SetMethod != null))
            {
                columnNames.Add(fieldInfo.Name);
            }

            var result = new string[1, columnNames.Count];
            for (int i = 0; i < columnNames.Count; i++)
                result[0,i] = columnNames[i];
            return result;
        }
        private string[,] Data<T>(T field)
        {
            var sheetType = typeof(T);
            var data = new List<string>();
            foreach (var fieldInfo in sheetType.GetProperties(BindingFlags.Public|BindingFlags.NonPublic | BindingFlags.Instance).Where(x=>x.SetMethod != null))
            {
                var value = fieldInfo.GetValue(field) as String;
                data.Add(value);
            }

            var result = new string[1, data.Count];
            for (int i = 0; i < data.Count; i++)
                result[0,i] = data[i];
            return result;
        }
    }

    public static class FieldsExtension
    {
        private static readonly Regex _entityRegex = new Regex(@"\{([^value]\w+)\}");

        private static bool TryParseColumn(string value, out string result)
        {
            result = null;
            if (IsNullOrEmpty(value))
            {
                return false;
            }

            var match = _entityRegex.Match(value);
            if (match.Success)
            {
                result = match.Groups[1].Value;
                return true;
            }
            return false;
        }

        private static HashSet<string> GetValues(this Fields field)
        {
            var result = new HashSet<string>();
            PossiblyAddEntity(field.CH1, result);
            PossiblyAddEntity(field.CH2, result);
            PossiblyAddEntity(field.Text, result);
            PossiblyAddEntity(field.optValue, result);
            PossiblyAddEntity(field.varCode, result);
            return result;
        }

        private static void PossiblyAddEntity(string fieldCh1, HashSet<string> result)
        {
            if (TryParseColumn(fieldCh1, out var value))
                result.Add(value);
        }

        public static bool IsProfileField(this Fields field)
        {
            return !GetValues(field).Any();
        }
        public static bool IsBrandField(this Fields field)
        {
            return GetValues(field).All(x=> Compare(x, "Brand", StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        public static void AddField(this IList<ProfilingFields> fields, Fields field, FieldCategories [] fieldCategories )
        {
            bool hasAdded = false;
            foreach (var fieldCategory in fieldCategories)
            {
                if (Compare(fieldCategory.FieldName, field.Name,StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    fields.Add(ProfilingFields.LegacyConstuctor(field.Name, fieldCategory.Categories, fieldCategory.Subsets, field.Question, field.ScaleFactor, field.PreScaleLowPassFilterValue));
                    hasAdded = true;
                }
            }

            if (!hasAdded)
            {
                fields.Add(ProfilingFields.LegacyConstuctor(field.Name, "", "", "", "", ""));
            }
        }
        public static void AddField(this IList<BrandFields> fields, Fields field, FieldCategories[] fieldCategories)
        {
            bool hasAdded = false;
            foreach (var fieldCategory in fieldCategories)
            {
                if (Compare(fieldCategory.FieldName, field.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    fields.Add(BrandFields.LegacyConstuctor(field.Name, fieldCategory.Categories));
                    hasAdded = true;
                }
            }
            if (!hasAdded)
            {
                fields.Add(BrandFields.LegacyConstuctor(field.Name, ""));
            }
        }
    }
}