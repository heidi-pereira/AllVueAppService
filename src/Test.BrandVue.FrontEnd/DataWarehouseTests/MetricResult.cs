using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    public class MetricResults
    {
        public string[] HeaderRecords { get; set; }
        public MetricResult[] Results { get; set; }
    }

    public class MetricResult
    {
        [CanBeNull] public Dictionary<string,int> Ids { get; set; }
        public DateTime EndDate { get; set; }
        public double Value { get; set; }
        public uint SampleSize { get; set; }
    }
}