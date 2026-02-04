using System.Collections.Generic;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class DashPageSerializationInfo : ISerializationInfo<DashPage>
    {
        public string SheetName { get; } = "DashPages";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(DashPage.Name), nameof(DashPage.MenuIcon), nameof(DashPage.PageType), nameof(DashPage.HelpText), nameof(DashPage.MinUserLevel), nameof(DashPage.StartPage), nameof(DashPage.Layout), nameof(DashPage.PageTitle), nameof(DashPage.Disabled), nameof(DashPage.Subset), nameof(DashPage.Environment), nameof(DashPage.Roles)
        };

        public string[] RowData(DashPage p)
        {
            return new[]
            {
                p.Name, p.MenuIcon, p.PageType, p.HelpText, p.MinUserLevel, p.StartPage, p.Layout, p.PageTitle, p.Disabled, p.Subset, p.Environment, p.Roles
            };
        }

        public IEnumerable<DashPage> OrderForOutput(IEnumerable<DashPage> metricDefinitions)
        {
            return metricDefinitions;
        }
    }
}