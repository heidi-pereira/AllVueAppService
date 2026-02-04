using Aspose.Cells;

namespace MIG.SurveyPlatform.MapGeneration.Serialization
{
    internal interface ISerializableEnumerable
    {
        Worksheet AddSheet(Workbook workbook, bool force);
    }
}