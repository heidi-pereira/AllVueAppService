using System;
using Aspose.Cells;

namespace MIG.SurveyPlatform.MapGeneration
{
    internal static class AsposeCellsHelper
    {
        private static License _CellsLicense;
        public static void SetCellsLicense()
        {
            if (_CellsLicense == null)
            {
                _CellsLicense = new License();
                _CellsLicense.SetLicense("Aspose.Total.lic");
            }
        }

        public static Workbook OpenWorkbook(string pFile, string pPassword = "")
        {
            SetCellsLicense();
            if (String.IsNullOrWhiteSpace(pPassword))
            {
                Workbook b = new Workbook(pFile);
                return b;
            }
            else
            {
                LoadOptions lo = new LoadOptions();
                lo.Password = pPassword;
                Workbook b = new Workbook(pFile, lo);
                return b;
            }
        }

        /// <summary>
        /// Starts a new workbook, license initiated, with sheets according to the | separated names
        /// </summary>
        /// <param name="pSheetNames"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Workbook StartWorkbook(string pSheetNames = "")
        {
            SetCellsLicense();
            Workbook b = new Workbook();
            if (!String.IsNullOrEmpty(pSheetNames))
            {
                b.Worksheets.Clear();
                string[] sh = pSheetNames.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in sh)
                {
                    Worksheet tSheet = b.Worksheets[b.Worksheets.Add(SheetType.Worksheet)];
                    tSheet.Name = s;
                }
            }
            else
                b.Worksheets.Clear();
            return b;
        }

        public static void AutoFitColumnsWithMax(this Worksheet worksheet, int maxWidth = 500)
        {
            worksheet.AutoFitColumns();
            var lastDataColumnIndex = worksheet.Cells.MaxDataColumn - 1;
            for (int c = 0; c <= lastDataColumnIndex; c++)
            {
                if (worksheet.Cells.GetColumnWidthPixel(c) > maxWidth)
                {
                    worksheet.Cells.SetColumnWidthPixel(c, maxWidth);
                }
            }
        }

        public static Worksheet AddNewSheetSafeName(this Workbook pW, string pName)
        {

            // Cap length
            string tName = pName;
            if (tName.Length > 30)
                tName = pName.Substring(0, 30);

            int ind = 0;
            string TestName = tName;
            while (pW.Worksheets[TestName] != null)
            {
                ind += 1;
                TestName = tName + ind;
            }
            return pW.Worksheets.Add(TestName);
        }

        public static void PopulateRow(this Worksheet pS, int pRow, params string[] parts)
        {
            var loopTo = parts.Length - 1;
            for (int i = 0; i <= loopTo; i++)
            {
                var cell = pS.Cells[pRow, i];
                string val = parts[i];
                if (!string.IsNullOrEmpty(val))
                {
                    if (int.TryParse(val, out var intVal))
                    {
                        // This overload allows it to be read back as an int at the other side, otherwise it gets an apostrophe in front to make it text
                        cell.PutValue(intVal);
                    }
                    else
                    {
                        cell.PutValue(val);
                    }
                }
            }
        }
    }
}