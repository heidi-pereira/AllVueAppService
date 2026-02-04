namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal class DashPart
    {
        public string PaneId { get; set; }
        public string PartType { get; set; }
        public string Spec1 { get; set; }
        public string Spec2 { get; set; }
        public string Spec3 { get; set; }
        public string HelpText { get; set; }
        public bool Disabled { get; set; }
        public string Environment { get; set; }
        public string AutoMetrics { get; set; }
        public string AutoPanes { get; set; }
    }
}