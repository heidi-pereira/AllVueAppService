using ClosedXML.Excel;
using OpenEnds.BackEnd.Model;
using System.Text;

namespace OpenEnds.BackEnd.Library;

public class ExportService
{
    private const int AnswerTextColumnWidth = 60;

    public enum ExportFormat
    {
        TabularXLSX,
        TabularCSV,
        CodebookXLSX
    }

    public byte[] GenerateExportFromSummary(OpenEndQuestionSummaryResult summary, ExportFormat exportFormat)
    {
        var sortedThemesList = summary.Themes.OrderBy(t => t.ThemeIndex).ToList();

        switch (exportFormat)
        {
            case ExportFormat.TabularCSV:
                return BuildTabularCSV(sortedThemesList, summary.TextThemes);
            case ExportFormat.TabularXLSX:
                return BuildTabularXLSX(sortedThemesList, summary.TextThemes);
            case ExportFormat.CodebookXLSX:
                return BuildCodebookXLSX(sortedThemesList, summary.TextThemes);
            default:
                throw new Exception("format not supported");
        }
    }

    public static string GetExportFileExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.TabularCSV => "csv",
            ExportFormat.TabularXLSX => "xlsx",
            ExportFormat.CodebookXLSX => "xlsx",
            _ => "dat"
        };
    }

    private static byte[] BuildTabularCSV(List<OpenEndTheme> themeList, IEnumerable<TextTheme> textThemes)
    {
        var sb = new StringBuilder();

        sb.Append("ResponseId,Text");
        foreach (var theme in themeList)
        {
            sb.Append($",{EscapeCsv(theme.ThemeText)}");
        }
        sb.AppendLine();

        foreach (var textTheme in textThemes)
        {
            var responseId = ExtractResponseIdFromTextThemeId(textTheme.Id);
            sb.Append($"{responseId},{EscapeCsv(textTheme.Text)}");
            var textThemeIndices = textTheme.Themes ?? new List<int>();
            foreach (var theme in themeList)
            {
                sb.Append(",");
                if (textThemeIndices.Contains(theme.ThemeIndex))
                {
                    sb.Append("1");
                }
            }
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] BuildTabularXLSX(List<OpenEndTheme> themeList, IEnumerable<TextTheme> textThemes)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        worksheet.Column(2).Width = AnswerTextColumnWidth;
        worksheet.Cell(1, 1).Value = "Response Id";
        worksheet.Cell(1, 2).Value = "Text";

        for (int i = 0; i < themeList.Count; i++)
        {
            worksheet.Cell(1, i + 3).Value = themeList[i].ThemeText;
        }

        worksheet.Row(1).Style.Font.SetBold();

        int row = 2;
        foreach (var textTheme in textThemes)
        {
            var responseId = ExtractResponseIdFromTextThemeId(textTheme.Id);
            worksheet.Cell(row, 1).Value = responseId;
            worksheet.Cell(row, 2).Value = textTheme.Text;
            var textThemeIndices = textTheme.Themes ?? new List<int>();
            for (int col = 0; col < themeList.Count; col++)
            {
                if (textThemeIndices.Contains(themeList[col].ThemeIndex))
                {
                    worksheet.Cell(row, col + 3).Value = 1;
                }
            }
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildCodebookXLSX(List<OpenEndTheme> themeList, IEnumerable<TextTheme> textThemes)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        worksheet.Column(2).Width = AnswerTextColumnWidth;
        worksheet.Cell(1, 1).Value = "Response Id";
        worksheet.Cell(1, 2).Value = "Text";

        var highestThemeCount = textThemes.Select(tt => tt.Themes.Count).DefaultIfEmpty(0).Max();
        for (int i = 1; i <= highestThemeCount; i++)
        {
            worksheet.Cell(1, i + 2).Value = $"Code {i}";
        }

        worksheet.Row(1).Style.Font.SetBold();

        int row = 2;
        foreach (var textTheme in textThemes)
        {
            var responseId = ExtractResponseIdFromTextThemeId(textTheme.Id);
            worksheet.Cell(row, 1).Value = responseId;
            worksheet.Cell(row, 2).Value = textTheme.Text;
            for (int col = 0; col < textTheme.Themes.Count; col++)
            {
                worksheet.Cell(row, col + 3).Value = textTheme.Themes[col];
            }
            row++;
        }

        var summaryWorksheet = workbook.Worksheets.Add("Summary");
        summaryWorksheet.Cell(1, 1).Value = "ID";
        summaryWorksheet.Cell(1, 2).Value = "Description";

        for (int i = 0; i < themeList.Count; i++)
        {
            summaryWorksheet.Cell(i + 2, 1).Value = themeList[i].ThemeIndex;
            summaryWorksheet.Cell(i + 2, 2).Value = themeList[i].ThemeText;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "";
        }
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static int? ExtractResponseIdFromTextThemeId(string input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        var parts = input.Split('_');
        if (parts.Length == 0)
            return null;

        if (int.TryParse(parts[0], out int result))
            return result;

        return null;
    }
}