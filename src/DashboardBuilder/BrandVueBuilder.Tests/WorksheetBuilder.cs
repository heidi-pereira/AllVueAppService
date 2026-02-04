using Aspose.Cells;
using DashboardBuilder.AsposeHelper;

namespace BrandVueBuilder.Tests
{
    public class WorksheetBuilder
    {
        public static Worksheet CreateFieldWorksheet(string[] entityTypes)
        {
            var workbook = AsposeCellsHelper.StartWorkbook();
            foreach (var entityType in entityTypes)
            {
                var entitySheet = workbook.Worksheets.Add($"{entityType}Entity");
                entitySheet.Cells.ImportArray(new [,] {{"Id", "Name", "Aliases"}}, 0, 0);
            }
            var fieldWorksheet = workbook.Worksheets.Add("Fields");
            fieldWorksheet.Cells.ImportArray(new[,] {{"Name", "VarCode", "CH1", "CH2", "optValue", "Text", "RelatedField", "StartDate"}}, 0, 0);
            return fieldWorksheet;
        }
        public static Worksheet CreateCategoriesWorksheet(Workbook workbook)
        {
            var fieldWorksheet = workbook.Worksheets.Add("Categories");
            fieldWorksheet.Cells.ImportArray(new[,] { { "FieldName", "Categories", "Subsets"} }, 0, 0);
            return fieldWorksheet;
        }

    }
}