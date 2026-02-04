using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class DashPaneSerializationInfo : ISerializationInfo<DashPane>
    {
        public string SheetName { get; } = "DashPanes";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(DashPane.Id), nameof(DashPane.PageName), nameof(DashPane.Height), nameof(DashPane.PaneType), nameof(DashPane.Spec), nameof(DashPane.View)
        };

        public string[] RowData(DashPane p)
        {
            return new[]
            {
                p.Id, p.PageName, p.Height.ToString(), p.PaneType, p.Spec, p.View
            };
        }

        public IEnumerable<DashPane> OrderForOutput(IEnumerable<DashPane> metricDefinitions)
        {
            return metricDefinitions;
        }
    }
}