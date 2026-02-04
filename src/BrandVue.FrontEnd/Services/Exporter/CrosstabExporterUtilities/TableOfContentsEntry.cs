using OfficeOpenXml;

namespace BrandVue.Services.CrosstabExporterUtilities
{
    public class TableOfContentsEntry
    {
        public string Name { get; }
        public int TableNumber { get; }
        public ExcelWorksheet ExcelWorksheet { get; }
        public string BaseDescription { get; }
        public string HelpText { get; }
        public int Row { get; }
        public string ErrorDescription { get; }

        public TableOfContentsEntry(string name, string helpText, int tableNumber, ExcelWorksheet worksheet,
            string baseDescription, int row) : this(name, helpText, tableNumber, worksheet, baseDescription, row, string.Empty)
        {

        }
        public TableOfContentsEntry(string name, string helpText, int tableNumber, ExcelWorksheet worksheet, string baseDescription, int row, string errorDescription)
        {
            Name = name;
            HelpText = helpText;
            TableNumber = tableNumber;
            ExcelWorksheet = worksheet;
            BaseDescription = baseDescription;
            Row = row;
            ErrorDescription = errorDescription;

        }
    }
}
