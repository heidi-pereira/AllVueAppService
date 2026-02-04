using System.Collections.Generic;
using Aspose.Cells;

namespace DashboardBuilder.Core
{
    public interface IMapSettings
    {
        Workbook Map { get; }
        IReadOnlyDictionary<string, string> Settings { get; }
        string ShortCode { get; }
    }
}