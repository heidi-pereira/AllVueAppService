namespace BrandVue.MixPanel
{
    public static class VueEventsDictionary
    {
        public static Dictionary<VueEvents, VueEventProps> _eventsProperties = new()
        {
            { VueEvents.ChartDownload, new VueEventProps("Charts", "Buttons", "External") },
            { VueEvents.ExcelDownload, new VueEventProps("Charts", "Buttons", "External") },
            { VueEvents.CreatedNewMetric, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.DeletedMetric, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.UpdatedMetric, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.DisabledMetric, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.EditedHelpText, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.EditedMetricDisplayName, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.EditedVarCode, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.EnabledMetric, new VueEventProps("Configuration", "Metric Configuration", "Internal") },
            { VueEvents.CreateNewPage, new VueEventProps("Configuration", "Page Configuration", "Internal") },
            { VueEvents.DeletePage, new VueEventProps("Configuration", "Page Configuration", "Internal") },
            { VueEvents.UpdatePageConfiguration, new VueEventProps("Configuration", "Page Configuration", "Internal") },
            { VueEvents.CreateNewSubset, new VueEventProps("Configuration", "Subset Configuration", "Internal") },
            { VueEvents.UpdateSubsetConfiguration, new VueEventProps("Configuration", "Subset Configuration", "Internal") },
            { VueEvents.CreateBaseVariable, new VueEventProps("Configuration", "Variables", "Internal") },
            { VueEvents.CreateVariable, new VueEventProps("Configuration", "Variables", "Internal") },
            { VueEvents.UpdateBaseVariable, new VueEventProps("Configuration", "Variables", "Internal") },
            { VueEvents.UpdateVariable, new VueEventProps("Configuration", "Variables", "Internal") },
            { VueEvents.DeleteBaseVariable, new VueEventProps("Configuration", "Variables", "Internal") },
            { VueEvents.DeleteVariable, new VueEventProps("Configuration", "Variables", "Internal") },
        };
    }
}