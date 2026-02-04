using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aspose.Cells;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization
{
    internal class ExcelSerializer
    {
        public static void Save(IEnumerable<ISerializableEnumerable> sheetsToAdd, string filePath, bool force)
        {
            var workbook = File.Exists(filePath) ? AsposeCellsHelper.OpenWorkbook(filePath) : AsposeCellsHelper.StartWorkbook();
            var sheetsAdded = sheetsToAdd.Select(s => WithFormattedHeadings(s.AddSheet(workbook, force))).ToArray();
            workbook.Save(filePath);
        }

        private static Worksheet WithFormattedHeadings(Worksheet sheet)
        {
            sheet.AutoFitColumnsWithMax();
            var style = new CellsFactory().CreateStyle();
            style.Font.IsBold = true;
            sheet.Cells.Rows[0].ApplyStyle(style, new StyleFlag() {FontBold = true});
            sheet.FreezePanes(1, 1, 1, 1);
            sheet.AutoFilter.SetRange(0, 0, sheet.Cells.MaxDataColumn);
            return sheet;
        }
    }
}