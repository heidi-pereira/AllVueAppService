using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization
{
    internal interface ISerializationInfo<TTypeToSerialize>
    {
        string SheetName { get; }
        string[] ColumnHeadings { get; }
        string[] RowData(TTypeToSerialize instance);
        IEnumerable<TTypeToSerialize> OrderForOutput(IEnumerable<TTypeToSerialize> profileFields);
    }
}