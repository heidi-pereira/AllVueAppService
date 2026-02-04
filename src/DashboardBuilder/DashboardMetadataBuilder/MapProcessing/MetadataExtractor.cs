using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Schema;

namespace DashboardMetadataBuilder.MapProcessing
{
    public class MetadataExtractor
    {
        private static readonly string[] BrandsHeadingsToLowerCase = {"id","brand", "color", "logo"};

        public void SaveAllCsvsWithMostlyLowercaseColumnNames(Workbook workBook, string outPath)
        {
            foreach (Worksheet s in workBook.Worksheets)
                SaveSheetWithMostlyLowercaseColumnNames(s, outPath, s.Name);
        }

        private static void SaveSheetWithMostlyLowercaseColumnNames(Worksheet s, string outDirectory, string outFilenameWithoutExtension)
        {
            LowercaseMostColumnHeadings(s);
            SaveSheetAsCsv(s, outDirectory, EnsureFirstLower(outFilenameWithoutExtension) + ".csv");
        }

        /// <summary>
        /// Todo: Remove this lowercasing business https://app.clubhouse.io/mig-global/story/11541/eliminate-field-recasing-behaviour-in-dashboard-builder
        /// </summary>
        private static void LowercaseMostColumnHeadings(Worksheet s)
        {
            for (var c = 0; c <= s.Cells.MaxDataColumn; c++)
            {
                var currentHeading = s.Cells[0, c].StringValue;
                if (!String.IsNullOrWhiteSpace(currentHeading) && !IsProfilesHeading(s) && !IsCustomBrandGroup(s, currentHeading))
                {
                    s.Cells[0, c].PutValue(EnsureFirstLower(currentHeading));
                }
            }
        }

        private static bool IsProfilesHeading(Worksheet sheet)
        {
            return sheet.Name == "Profiles";
        }

        private static bool IsCustomBrandGroup(Worksheet sheet, string currentHeading)
        {
            return sheet.Name == SheetNames.Brands && !BrandsHeadingsToLowerCase.Contains(currentHeading, StringComparer.OrdinalIgnoreCase);
        }

        private static void RemoveCommentedOutLines(Worksheet worksheet)
        {
            var rowsToDelete = new List<int>();
            for (int row = 0; row < worksheet.Cells.MaxDataRow; row++)
            {
                if (worksheet.Cells[row, 0].StringValue.StartsWith("//"))
                {
                    rowsToDelete.Add(row);
                }
            }
            rowsToDelete.Reverse();
            foreach (var index in rowsToDelete)
            {
                worksheet.Cells.DeleteRow(index);
            }
        }
        internal static void SaveSheetAsCsv(Worksheet s, string outDirectoryPath, string outFilename)
        {
            outFilename = outFilename.ReplaceInvalidFilenameCharacters("_");

            s.Workbook.Worksheets.ActiveSheetIndex = s.Index;
            s.Cells.DeleteBlankRows();
            s.RemoveAutoFilter();
            s.Cells.DeleteBlankColumns();
            RemoveCommentedOutLines(s);
            string outPath = Path.Combine(outDirectoryPath, outFilename);
            Directory.CreateDirectory(outDirectoryPath);
            s.Workbook.CalculateFormula();
            s.Workbook.Save(outPath, new TxtSaveOptions(SaveFormat.CSV){ Encoding = Encoding.UTF8 });
        }

        private static string EnsureFirstLower(string pText)
        {
            if (string.IsNullOrEmpty(pText))
                return "";
            if (pText.Length == 1)
                return pText.ToLower();
            return pText[0].ToString().ToLower() + pText.Substring(1);
        }

    }
}