using System;
using System.Collections.Generic;
using Aspose.Cells;

namespace DashboardBuilder.AsposeHelper
{
    internal static class AsposeCellsHelper
    {
        private static License _CellsLicense;
        public static void SetCellsLicense()
        {
            if (_CellsLicense == null)
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                _CellsLicense = new License();
                _CellsLicense.SetLicense("Aspose.Total.lic");
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

        /// <summary>
        /// Starts a new workbook, license initiated, with sheets according to the | separated names
        /// </summary>
        /// <param name="pSheetNames"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Worksheet StartWorkbook_FirstSheet(string pSheetNames = "")
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
                    tSheet.Name = SafeSheetName(b, s);
                }
            }

            return b.Worksheets[0];
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

        public static void PopulateRow(this Worksheet pS, int pRow, params string[] parts)
        {
            for (int i = 0; i <= parts.Length - 1; i++)
            {
                if (!String.IsNullOrEmpty(parts[i]))
                    pS.Cells[pRow, i].PutValue(parts[i]);
            }
        }

        public static int FindTextInCol(Worksheet pS, int pTitleRow, int pStartCol, string pText, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            for (int c = pStartCol; c <= pS.Cells.MaxDataColumn; c++)
                if (String.IsNullOrEmpty(pText))
                {
                    if (String.IsNullOrWhiteSpace(pS.Cells[pTitleRow, c].StringValue))
                    {
                        return c;
                    }
                }
                else if (pS.Cells[pTitleRow, c].StringValue.Equals(pText, stringComparison))
                {
                    return c;
                }

            return -1;
        }

        public static string SafeSheetName(Workbook pW, string pName)
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

            return TestName;
        }

        /// <returns>A dictionary from <paramref name="keyColumn "/>'s integer values, to the corresponding first row integer values, or an empty dictionary if the sheet/column are not present</returns>
        public static Dictionary<int, int> GetIdMap(Workbook workbook, string sheetName, string keyColumn)
        {
            Dictionary<int, int> d = new Dictionary<int, int>();
            var sheet = workbook.Worksheets[sheetName];
            if (sheet == null) return d;
            var col = FindTextInCol(sheet, 0, 0, keyColumn);
            if (col == -1) return d;
            for (var r = 1; r <= sheet.Cells.MaxDataRow; r++)
                if (!String.IsNullOrWhiteSpace(sheet.Cells[r, 0].StringValue))
                    if (!String.IsNullOrWhiteSpace(sheet.Cells[r, col].StringValue))
                    {
                        var destId = Convert.ToInt32(sheet.Cells[r, 0].StringValue);
                        var srcId = Convert.ToInt32(sheet.Cells[r, col].StringValue);
                        if (!d.ContainsKey(srcId))
                            d.Add(srcId, destId);
                    }

            return d;
        }
    }
}