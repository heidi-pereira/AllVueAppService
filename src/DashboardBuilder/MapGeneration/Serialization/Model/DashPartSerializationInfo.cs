using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class DashPartSerializationInfo : ISerializationInfo<DashPart>
    {
        public string SheetName { get; } = "DashParts";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(DashPart.PaneId), nameof(DashPart.PartType), nameof(DashPart.Spec1), nameof(DashPart.Spec2), nameof(DashPart.Spec3), nameof(DashPart.HelpText), nameof(DashPart.Disabled), nameof(DashPart.Environment), nameof(DashPart.AutoMetrics), nameof(DashPart.AutoPanes)
        };

        public string[] RowData(DashPart p)
        {
            return new[]
            {
                p.PaneId, p.PartType, p.Spec1, p.Spec2, p.Spec3, p.HelpText, (p.Disabled ? "x" : ""), p.Environment, p.AutoMetrics, p.AutoPanes
            };
        }

        public IEnumerable<DashPart> OrderForOutput(IEnumerable<DashPart> metricDefinitions)
        {
            return metricDefinitions;
        }
    }
}