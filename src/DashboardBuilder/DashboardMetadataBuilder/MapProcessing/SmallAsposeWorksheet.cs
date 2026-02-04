using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Aspose.Cells;

namespace DashboardMetadataBuilder.MapProcessing
{
    internal class SmallAsposeWorksheet
    {
        private readonly Worksheet _worksheet;
        private readonly Dictionary<string, int> _columnHeadingToIndex;
        private readonly Lazy<List<IDictionary<string, string>>> _dataRows;
        private List<IDictionary<string, string>> DataRows => _dataRows.Value;
        public string Name => _worksheet.Name;
        
        public SmallAsposeWorksheet(Workbook workbook, string worksheetName)
        {
            if (workbook == null) throw new ArgumentNullException(nameof(workbook));
            if (worksheetName == null) throw new ArgumentNullException(nameof(worksheetName));
            _worksheet = workbook.Worksheets[worksheetName] ?? throw new ArgumentOutOfRangeException(nameof(worksheetName), worksheetName, $"No sheet called {worksheetName}");
            _columnHeadingToIndex = CreateColumnHeadingToIndex(_worksheet);
            _dataRows = new Lazy<List<IDictionary<string, string>>>(() => CreateDataRowDictionary().ToList());
        }

        public bool HasColumn(string name) => _columnHeadingToIndex.ContainsKey(name.ToLower());
        public List<IDictionary<string, string>> Rows => DataRows;
        public HashSet<string> GetDistinctValuesForColumns(params string[] colNames)
        {
            return GetDistinctValuesForColumns(_ => true, colNames);
        }

        public HashSet<string> GetDistinctValuesForColumns(Func<IDictionary<string, string>, bool> rowsWhere, params string[] colNames)
        {
            var relevantDataRows = DataRows.Where(rowsWhere).ToList();
            var selectMany = colNames.SelectMany(colName => GetValuesForColumn(relevantDataRows, colName));
            return new HashSet<string>(selectMany);
        }

        public IEnumerable<string> GetColumnValues(string columnName)
        {
            return DataRows.Select(r => r[columnName]);
        }

        public static IEnumerable<string> GetValuesForColumn(IEnumerable<IDictionary<string, string>> measureMetadata, string key)
        {   //Note the Split('|') which is valid for lots of columns we use. If an id included a pipe character this would pretty much go to hell.
            return measureMetadata.SelectMany(row => row.TryGetValue(key, out var value) ? value.Split('|') : new string[0]).Where(s => !String.IsNullOrWhiteSpace(s));
        }

        private IEnumerable<IDictionary<string, string>> CreateDataRowDictionary()
        {
            for (int row = 1; row <= _worksheet.Cells.MaxDataRow; row++)
            {
                yield return _columnHeadingToIndex.ToDictionary(kvp => kvp.Key,
                    kvp => _worksheet.Cells[row, kvp.Value].StringValueWithoutFormat, StringComparer.OrdinalIgnoreCase);
            }
        }

        private static Dictionary<string, int> CreateColumnHeadingToIndex(Worksheet worksheet)
        {
            var orderedColumnHeadingCells = worksheet.Cells.Rows[0].Cast<Cell>().Where(c => !String.IsNullOrWhiteSpace(c.StringValueWithoutFormat)).ToList();

            var duplicateColumnNames = orderedColumnHeadingCells
                .ToLookup(c => c.StringValueWithoutFormat.ToLower(), c => c.Column)
                .Where(columnName => columnName.Count() > 1).Select(c => $"'{c.Key}'").ToList();
            if (duplicateColumnNames.Any())
            {
                throw new DuplicateNameException($"In sheet {worksheet.Name}, duplicate column names detected: {string.Join(",", duplicateColumnNames)}");
            }

            return orderedColumnHeadingCells.ToDictionary(c => c.StringValueWithoutFormat.ToLower(), c => c.Column);
        }
    }
}