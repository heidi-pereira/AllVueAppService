using System;
using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing
{
    public static class ExcelWorkbookExtensions
    {
        static ExcelWorkbookExtensions()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            new License().SetLicense("Aspose.Total.lic");
        }

        public static Workbook LoadWorkbook(string filePath)
        {
            return new Workbook(filePath);
        }

        public static int GetHeadingColumnIndex(this Worksheet sheet, string requiredHeadingText, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            var firstRow = sheet.Cells.Rows[0];
            if (firstRow.LastDataCell == null)
            {
                return -1;
            }
            for (int c = 0; c <= firstRow.LastDataCell.Column; c++)
            {
                var currentHeading = firstRow.GetCellOrNull(c)?.StringValue ?? "";
                if (string.Equals(requiredHeadingText, currentHeading, stringComparison))
                {
                    return c;
                }
            }

            return -1;
        }

        public static TypedWorksheet<TSheetRow> Get<TSheetRow>(this Workbook workbook) where TSheetRow : SheetRow, new()
        {
            return new TypedWorksheet<TSheetRow>(workbook);
        }
    }
}
