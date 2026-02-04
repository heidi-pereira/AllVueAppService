using System.Collections.Generic;
using System.Linq;
using Aspose.Cells;

namespace MIG.SurveyPlatform.MapGeneration.Serialization
{
    internal class SerializableEnumerable<TTypeToSerialize> : ISerializableEnumerable
    {
        public SerializableEnumerable(IEnumerable<TTypeToSerialize> unsortedRecords, ISerializationInfo<TTypeToSerialize> serializationInfo)
        {
            UnsortedRecords = unsortedRecords;
            SerializationInfo = serializationInfo;
        }

        public IEnumerable<TTypeToSerialize> UnsortedRecords { get; private set; }
        public ISerializationInfo<TTypeToSerialize> SerializationInfo { get; private set; }

        public Worksheet AddSheet(Workbook workbook, bool force)
        {
            if (force) workbook.Worksheets.RemoveAt(SerializationInfo.SheetName);
            var newSheet = workbook.AddNewSheetSafeName(SerializationInfo.SheetName);
            var sortedRecords = SerializationInfo.OrderForOutput(UnsortedRecords).ToList();
            newSheet.PopulateRow(0, SerializationInfo.ColumnHeadings);
            for (int i = 0; i < sortedRecords.Count; i++)
            {
                var profileField = sortedRecords[i];
                newSheet.PopulateRow(i + 1, SerializationInfo.RowData(profileField));
            }
            return newSheet;
        }
    }
}