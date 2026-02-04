using System;

namespace DashboardMetadataBuilder.MapProcessing.Typed
{
    [AttributeUsage(AttributeTargets.Class)]
    class CsvFileAttribute : Attribute
    {
        public int FirstDataRow { get; }

        public CsvFileAttribute(int firstDataRow = 1)
        {
            FirstDataRow = firstDataRow;
        }
    }
}