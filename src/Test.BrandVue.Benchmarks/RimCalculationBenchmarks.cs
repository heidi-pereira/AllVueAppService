using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BrandVue.SourceData.Weightings.Rim;
using TestCommon.Weighting;

namespace Test.BrandVue.Benchmarks
{
#if DEBUG // Too long for local debug loop
        [NUnit.Framework.Explicit]
#endif
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class RimCalculationBenchmarks
    {
        private IRimWeightingCalculator _rimWeightingCalculator;
        public IEnumerable<RimTestData> TestCaseData => RimWeightingTestDataProvider.GetRawTestData();

        [GlobalSetup]
        public void ArrangeInputData()
        {
            _rimWeightingCalculator = new RimWeightingCalculator();
        }

        [ParamsSource(nameof(TestCaseData))] 
        public RimTestData TestCase { get; set; }

        [Benchmark]
        public void RimBenchmark()
        {
            _rimWeightingCalculator.Calculate(TestCase.QuotaCellSampleSizesInIndexOrder, TestCase.RimDimensions, true);
        }
    }
}
