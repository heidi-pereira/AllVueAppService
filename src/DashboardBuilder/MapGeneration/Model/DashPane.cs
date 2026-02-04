using System.Collections.Generic;

namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal class DashPane
    {
        public IReadOnlyCollection<DashPart> Parts { get; }

        public DashPane(IReadOnlyCollection<DashPart> parts)
        {
            Parts = parts;
        }

        public string Id { get; set; }
        public string PageName { get; set; }
        public int Height { get; set; }
        public string PaneType { get; set; }
        public string Spec { get; set; }
        public string View { get; set; }
    }
}