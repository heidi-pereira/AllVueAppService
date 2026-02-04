using BenchmarkDotNet.Attributes;
using BrandVue.SourceData.Utils;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Jobs;
using NUnit.Framework;

namespace Test.BrandVue.Benchmarks
{
#if DEBUG // Too long for local debug loop
    [Explicit]
#endif
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class ArrayNullableSequenceComparerBenchmarks
    {
        private readonly ArrayStructuralComparer<int?> _baselineComparer = new();
        private readonly IEqualityComparer<int?[]> _customComparer = SequenceComparer<int?>.ForArray();
        private readonly int?[] _toCompare1 = new int?[]{1,2,3};
        private readonly int?[] _toCompare2 = new int?[]{1,2,4};

        [Benchmark(Baseline = true)]
        public bool EntityFrameworkArrayStructuralComparer() => _baselineComparer.Equals(_toCompare1, _toCompare2);

        [Benchmark]
        public bool ArraySequenceComparer() => _customComparer.Equals(_toCompare1, _toCompare2);
    }
}
