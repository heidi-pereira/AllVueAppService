namespace BrandVue.EntityFramework.MetaData.Reports
{
    public class DefaultReportFilter
    {
        public string MeasureName { get; set; }
        public List<DefaultReportFilterInstance> Filters { get; set; }
    }

    public class DefaultReportFilterInstance
    {
        public Dictionary<string, int[]> EntityInstances { get; set; }
        public int[] Values { get; set; }
        public bool Invert { get; set; }
        public bool TreatPrimaryValuesAsRange { get; set; }
    }
}
