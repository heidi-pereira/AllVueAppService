using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Aspose.Cells;

namespace DashboardMetadataBuilder.MapProcessing.Typed
{
    /// <summary>
    /// Provides a typed view over a worksheet based on a schema
    /// </summary>
    /// <remarks>To use this for a new sheet, just copy an existing implementation of <seealso cref="SheetRow"/> and change the field names/types.</remarks>
    /// <typeparam name="TSheetRow">A type describing the sheet schema</typeparam>
    public class TypedWorksheet<TSheetRow> where TSheetRow : SheetRow, new()
    {
        public TypedWorksheet(Workbook workbook, bool sheetMustExist = true, string sheetNameOverride = null)
        {
            if (workbook == null) throw new ArgumentNullException(nameof(workbook));

            var sheetType = typeof(TSheetRow);
            var sheetInfo = sheetType.GetCustomAttribute<SheetAttribute>();
            var csvInfo = sheetType.GetCustomAttribute<CsvFileAttribute>();
            var sheetName = sheetNameOverride ?? sheetInfo?.SheetName;
            if (sheetName == null && csvInfo == null)
            {
                throw new NotImplementedException($"{sheetType.Name} not implemented - must have [Sheet] or [CsvFile] attribute");
            }

            sheetName ??= workbook.Worksheets.First().Name;

            Worksheet = workbook.Worksheets[sheetName];

            if (Worksheet != null)
            {
                var firstDataRow = sheetInfo?.FirstDataRow ?? csvInfo?.FirstDataRow ?? 1;
                Rows = Initialize(Worksheet, sheetType, firstDataRow);
            }
            else if (sheetMustExist)
            {
                throw new InvalidDataException(
                    $"{workbook.FileName} must contain sheet {sheetName}.\r\n"
                    + "If this error seems incorrect, set ParentSheetMustExist to false, and call TryGet instead of this constructor.");
            }
        }

        public TypedWorksheet(Workbook workbook) : this(workbook, true)
        {
        }

        /// <summary>
        /// Prefer using the constructor whenever the sheet must exist in the context you're calling this from.
        /// When using this you absolutely must check the return value.
        /// </summary>
        public static bool TryGet(Workbook workbook, out TypedWorksheet<TSheetRow> typedWorksheet)
        {
            typedWorksheet = new TypedWorksheet<TSheetRow>(workbook, false);
            var sheetType = typeof(TSheetRow);
            var sheetInfo = sheetType.GetCustomAttribute<SheetAttribute>();
            if (sheetInfo.MustExist)
            {
                throw new InvalidOperationException("Must use constructor directly when ParentSheetMustExist is true.");
            }
            return typedWorksheet.Worksheet != null;
        }

        public IReadOnlyCollection<TSheetRow> Rows { get; }

        public Worksheet Worksheet { get; }

        public Cells Cells => Worksheet.Cells;


        private static readonly Type IntType = typeof(int);
        private static readonly Type ShortType = typeof(short);
        private static readonly Type StringType = typeof(string);
        private static readonly Type NullableDateTimeType = typeof(DateTime?);
        private Dictionary<string, int> m_ColIndexFromRawPropertyName;
        private Dictionary<string, int> m_ColIndexFromForExtraPropertyName;

        private List<TSheetRow> Initialize(Worksheet worksheet, Type sheetType, int firstDataRow)
        {
            var typedColumns = sheetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && (p.PropertyType == ShortType || p.PropertyType == IntType || p.PropertyType == StringType || p.PropertyType == NullableDateTimeType || p.PropertyType.IsEnum))
                .Select(p => new
                {
                    Property = p,
                    Index = GetIndex(worksheet, p),
                    HasDefaultValue = p.GetCustomAttribute<DefaultValueAttribute>() != null,
                    DefaultValue = p.GetCustomAttribute<DefaultValueAttribute>()?.Value,
                    Name = GetColumnName(p)
                })
                .OrderBy(col => col.Index)
                .ToList();

            m_ColIndexFromRawPropertyName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            m_ColIndexFromForExtraPropertyName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int titleRow = firstDataRow - 1;
            if (titleRow >= 0)
            {
                for (int columnIndex = 0;
                    columnIndex <= worksheet.Cells.MaxDataColumn &&
                    !string.IsNullOrWhiteSpace(worksheet.Cells[titleRow, columnIndex].StringValue);
                    columnIndex++)
                {
                    var colName = worksheet.Cells[titleRow, columnIndex].StringValue;
                    m_ColIndexFromRawPropertyName[colName] = columnIndex;
                    if (!typedColumns.Any(x =>
                        string.Equals(x.Name, colName.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                    {
                        m_ColIndexFromForExtraPropertyName[colName.Trim()] = columnIndex;
                    }
                }
            }

            var missingColNames = typedColumns.Where(c => c.Index == -1 && !c.HasDefaultValue).Select(c => c.Name).ToList();
            if (missingColNames.Any())
            {
                throw new InvalidDataException($"{sheetType.Name} {worksheet.Name} must contain column {string.Join(", ", missingColNames)}");
            }

            var rows = new List<TSheetRow>();
            for (int r = firstDataRow; r <= worksheet.Cells.MaxDataRow && !string.IsNullOrWhiteSpace(worksheet.Cells[r, 0].StringValue); r++)
            {
                if (worksheet.Cells[r, 0].StringValue.StartsWith("//"))
                {
                    continue;
                }

                var row = new TSheetRow();
                foreach (var extraData in m_ColIndexFromForExtraPropertyName)
                {
                    row.ExtraColumns[extraData.Key] = worksheet.Cells[r, extraData.Value].DisplayStringValue;
                }
                foreach (var typedColumn in typedColumns)
                {
                    if (typedColumn.Index == -1)
                    {
                        typedColumn.Property.SetValue(row, typedColumn.DefaultValue);
                    }
                    else
                    {
                        var worksheetCell = worksheet.Cells[r, typedColumn.Index];
                        object value = null;
                        if (typedColumn.Property.PropertyType == StringType)
                        {
                            value = worksheetCell.StringValue;
                        }
                        else if (string.IsNullOrWhiteSpace(worksheetCell.StringValue) && typedColumn.HasDefaultValue)
                        {
                            value = typedColumn.DefaultValue;
                        }
                        else if (typedColumn.Property.PropertyType == ShortType)
                        {
                            value = ParseShort(worksheetCell.StringValue, worksheet, typedColumn.Name, r);
                        }
                        else if (typedColumn.Property.PropertyType == IntType)
                        {
                            value = ParseInt(worksheetCell.StringValue, worksheet, typedColumn.Name, r);
                        }
                        else if (typedColumn.Property.PropertyType == NullableDateTimeType)
                        {
                            value = ParseDate(worksheetCell, worksheet, typedColumn.Name, r);
                        }
                        else if (typedColumn.Property.PropertyType.IsEnum)
                        {
                            value = ParseEnum(typedColumn.Property.PropertyType, worksheetCell.StringValue, worksheet, typedColumn.Name, r);
                        }
                        typedColumn.Property.SetValue(row, value);
                    }
                }
                rows.Add(row);
            }
            return rows;
        }

        private static object ParseDate(Cell worksheetCell, Worksheet worksheet, string typedColumnName, int row)
        {
            object value = null;
            if (!string.IsNullOrEmpty(worksheetCell.StringValue))
            {
                if (worksheetCell.Type == CellValueType.IsDateTime) //Ideally the user was configured the data to be a date..
                {
                    value = worksheetCell.DateTimeValue;
                }
                else if (DateTime.TryParse(worksheetCell.StringValue, out var myDateTime)) //OK well guess at the locale - assume it's me
                {
                    value = myDateTime;
                }
                else
                {
                    throw new InvalidDataException($"{worksheet.Name}: On row {row} of {typedColumnName}, `{worksheetCell.StringValue}` could not be parsed as an DateTime");
                }
            }
            return value;
        }

        private static int ParseInt(string stringValue, Worksheet worksheet, string typedColumnName, int row)
        {
            return int.TryParse(stringValue, out int intVal) ? intVal : throw new InvalidDataException($"{worksheet.Name}: On row {row} of {typedColumnName}, `{stringValue}` could not be parsed as an integer");
        }

        private static short ParseShort(string stringValue, Worksheet worksheet, string typedColumnName, int row)
        {
            return short.TryParse(stringValue, out short shortVal) ? shortVal : throw new InvalidDataException($"{worksheet.Name}: On row {row} of {typedColumnName}, `{stringValue}` could not be parsed as a short");
        }

        private static object ParseEnum(Type propertyType, string stringValue, Worksheet worksheet, string typedColumnName, int row)
        {
            try
            {
                return Enum.Parse(propertyType, stringValue, true);
            }
            catch (Exception e)
            {
                throw new InvalidDataException(
                    $"{worksheet.Name}: On row {row} of {typedColumnName}, `{stringValue}` could not be parsed as an enum", e);
            }
        }

        private static int GetIndex(Worksheet worksheet, PropertyInfo p)
        {
            var column = p.GetCustomAttribute<ColumnAttribute>();
            return column?.Index ?? worksheet.GetHeadingColumnIndex(GetColumnName(p));
        }

        private static string GetColumnName(PropertyInfo p)
        {
            var column = p.GetCustomAttribute<ColumnAttribute>();
            return column?.Name ?? p.Name;
        }
    }
}
