using Aspose.Slides;
using OfficeOpenXml;

namespace BrandVue.Services.Exporter
{
    public static class ExportHelper
    {
        private static License _asposeSlidesLicense;

        public static void SetAsposeSlidesLicense()
        {
            if (_asposeSlidesLicense == default)
            {
                _asposeSlidesLicense = new License();
                _asposeSlidesLicense.SetLicense("Aspose.Slides.2023.lic");
            }
        }

        public static class MimeTypes
        {
            public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string Csv = "text/csv";
            public const string Json = "application/json";
            public const string PowerPoint = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        }

        public static int GetNextRowIndex(this ExcelWorksheet sheet)
        {
            if (sheet.Dimension == null)
            {
                return 1;
            }

            return sheet.Dimension.End.Row + 1;
        }

        public static ExcelWorksheet AddWorkSheet(this ExcelPackage excelPackage, string name)
        {
            var sheet = excelPackage.Workbook.Worksheets.Add(name);
            sheet.Cells.StyleName = "Standard";
            sheet.View.ShowGridLines = false;
            return sheet;
        }
    }
}