using System.Collections.Generic;
using Aspose.Cells;

namespace BrandVueBuilder
{
    public interface IBrandVueProductSettings
    {
        Workbook Map { get; }
        string ShortCode { get; }
        IEnumerable<string> SubsetIds { get; }
    }
}