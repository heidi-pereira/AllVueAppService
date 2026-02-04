using System;

namespace DashboardMetadataBuilder.MapProcessing.Typed
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SheetAttribute : Attribute
    {
        public string SheetName { get; }

        /// <summary>
        /// If there are map files that don't require this sheet, set to false. You'll also need to use the TryCreate static factory method on TypedWorksheet
        /// If you set this to false, you'll be able to construct a TypedWorksheet for a missing sheet, but you really really should check its Exists property right afterwards and do something sensible
        /// </summary>
        public bool MustExist { get; }
        public int FirstDataRow { get; }

        public SheetAttribute(string sheetName, bool mustExist = true, int firstDataRow = 1)
        {
            SheetName = sheetName;
            MustExist = mustExist;
            FirstDataRow = firstDataRow;
        }
    }
}