using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.AutoGeneration.Buckets;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.AutoGeneration
{
    internal class BucketTestParameterEnumerator : IEnumerable
    {
        private readonly BucketTestParameters _singleBucket = new()
        {
            Min = 1,
            Max = 10,
            NoBuckets = 1,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 1, MaximumInclusive = 10}
            }
        };
        
        private readonly BucketTestParameters _tenBuckets = new()
        {
            Min = 1,
            Max = 10,
            NoBuckets = 10,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 1, MaximumInclusive = 1},
                new() {MinimumInclusive = 2, MaximumInclusive = 2},
                new() {MinimumInclusive = 3, MaximumInclusive = 3},
                new() {MinimumInclusive = 4, MaximumInclusive = 4},
                new() {MinimumInclusive = 5, MaximumInclusive = 5},
                new() {MinimumInclusive = 6, MaximumInclusive = 6},
                new() {MinimumInclusive = 7, MaximumInclusive = 7},
                new() {MinimumInclusive = 8, MaximumInclusive = 8},
                new() {MinimumInclusive = 9, MaximumInclusive = 9},
                new() {MinimumInclusive = 10, MaximumInclusive = 10}
            }
        };
        
        private readonly BucketTestParameters _fiveBuckets = new()
        {
            Min = 1,
            Max = 10,
            NoBuckets = 5,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 1, MaximumInclusive = 2},
                new() {MinimumInclusive = 3, MaximumInclusive = 4},
                new() {MinimumInclusive = 5, MaximumInclusive = 6},
                new() {MinimumInclusive = 7, MaximumInclusive = 8},
                new() {MinimumInclusive = 9, MaximumInclusive = 10}
            }
        };
        
        private readonly BucketTestParameters _fourBuckets = new()
        {
            Min = 1,
            Max = 10,
            NoBuckets = 4,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 1, MaximumInclusive = 3},
                new() {MinimumInclusive = 4, MaximumInclusive = 5},
                new() {MinimumInclusive = 6, MaximumInclusive = 8},
                new() {MinimumInclusive = 9, MaximumInclusive = 10}
            }
        };
        
        private readonly BucketTestParameters _invertedFiveBuckets = new()
        {
            Min = 10,
            Max = 1,
            NoBuckets = 5,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 9, MaximumInclusive = 10},
                new() {MinimumInclusive = 7, MaximumInclusive = 8},
                new() {MinimumInclusive = 5, MaximumInclusive = 6},
                new() {MinimumInclusive = 3, MaximumInclusive = 4},
                new() {MinimumInclusive = 1, MaximumInclusive = 2}
            }
        };
        
        private readonly BucketTestParameters _invertedFourBuckets = new()
        {
            Min = 10,
            Max = 1,
            NoBuckets = 4,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 8, MaximumInclusive = 10},
                new() {MinimumInclusive = 6, MaximumInclusive = 7},
                new() {MinimumInclusive = 3, MaximumInclusive = 5},
                new() {MinimumInclusive = 1, MaximumInclusive = 2}
            }
        };
        
        private readonly BucketTestParameters _noughtToTen = new()
        {
            Min = 0,
            Max = 10,
            NoBuckets = 11,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 0, MaximumInclusive = 0},
                new() {MinimumInclusive = 1, MaximumInclusive = 1},
                new() {MinimumInclusive = 2, MaximumInclusive = 2},
                new() {MinimumInclusive = 3, MaximumInclusive = 3},
                new() {MinimumInclusive = 4, MaximumInclusive = 4},
                new() {MinimumInclusive = 5, MaximumInclusive = 5},
                new() {MinimumInclusive = 6, MaximumInclusive = 6},
                new() {MinimumInclusive = 7, MaximumInclusive = 7},
                new() {MinimumInclusive = 8, MaximumInclusive = 8},
                new() {MinimumInclusive = 9, MaximumInclusive = 9},
                new() {MinimumInclusive = 10, MaximumInclusive = 10}
            }
        };

        private readonly BucketTestParameters _oneToMillion = new()
        {
            Min = 0,
            Max = 1_000_000-1,
            NoBuckets = 10,
            ExpectedBuckets = new List<IntegerInclusiveBucket>
            {
                new() {MinimumInclusive = 0, MaximumInclusive = 100_000-1},
                new() {MinimumInclusive = 100_000, MaximumInclusive = 200_000-1},
                new() {MinimumInclusive = 200_000, MaximumInclusive = 300_000-1},
                new() {MinimumInclusive = 300_000, MaximumInclusive = 400_000-1},
                new() {MinimumInclusive = 400_000, MaximumInclusive = 500_000-1},
                new() {MinimumInclusive = 500_000, MaximumInclusive = 600_000-1},
                new() {MinimumInclusive = 600_000, MaximumInclusive = 700_000-1},
                new() {MinimumInclusive = 700_000, MaximumInclusive = 800_000-1},
                new() {MinimumInclusive = 800_000, MaximumInclusive = 900_000-1},
                new() {MinimumInclusive = 900_000, MaximumInclusive = 1_000_000-1}
            }
        };

        public IEnumerator GetEnumerator()
        {
            yield return GetTest(_singleBucket, "Check single bucket creation");
            yield return GetTest(_tenBuckets, "Check multiple bucket creation");
            yield return GetTest(_fiveBuckets, "Check larger bucket creation");
            yield return GetTest(_fourBuckets, "Check larger bucket creation when rounding is needed");
            yield return GetTest(_invertedFiveBuckets, "Check inverse min max");
            yield return GetTest(_invertedFourBuckets, "Check inverse min max when rounding is needed");
            yield return GetTest(_noughtToTen, "Check naught to ten is correct");
            yield return GetTest(_oneToMillion, "Check 1 to 1 million is correct");
            
        }
        
        private class BucketTestParameters 
        { 
            public int Min { get; set; } 
            public int Max { get; set; }
            public int NoBuckets { get; set; }
            public IList<IntegerInclusiveBucket> ExpectedBuckets { get; set; }
        }

        private TestCaseData GetTest(BucketTestParameters parameters, string testDescription)
        {
            string expected = String.Join("", parameters.ExpectedBuckets.Select(b => $"({b.MinimumInclusive}-{b.MaximumInclusive})"));
            return new TestCaseData(parameters.Min, parameters.Max, parameters.NoBuckets, parameters.ExpectedBuckets)
                .SetName($"Min: {parameters.Min}, Max: {parameters.Max}, NoBuckets: {parameters.NoBuckets}, expected: {expected}")
                .SetDescription(testDescription);
        }
    }


    [TestFixture]
    public class IntegerInclusiveBucketTests
    {
        [Test]
        [TestCaseSource(typeof(BucketTestParameterEnumerator))]
        [Parallelizable(ParallelScope.All)]
        public void CreateBuckets_WithMinMaxAndNoBuckets_CreatesCorrectBuckets(int min, int max, int noBuckets,
            IList<IntegerInclusiveBucket> expectedBuckets)
        {
            IntegerInclusiveBucketCreator<IntegerInclusiveBucket> bucketCreator = new();

            var buckets = bucketCreator.CreateBuckets(min, max, noBuckets).ToList();

            bool isInverted = max < min;
            int smallestBucketVal = isInverted ? buckets.Last().MinimumInclusive : buckets.First().MinimumInclusive;
            int? largestBucketVal = isInverted ? buckets.First().MaximumInclusive : buckets.Last().MaximumInclusive;

            Assert.That(buckets.Count, Is.EqualTo(noBuckets));
            Assert.That(smallestBucketVal, Is.EqualTo(isInverted ? max : min));
            Assert.That(largestBucketVal, Is.EqualTo(isInverted ? min : max));

            for (int i = 0; i < expectedBuckets.Count; i++)
            {
                Assert.That(buckets[i].MinimumInclusive, Is.EqualTo(expectedBuckets[i].MinimumInclusive));
                Assert.That(buckets[i].MaximumInclusive, Is.EqualTo(expectedBuckets[i].MaximumInclusive));
            }
        }

        [Test]
        public void CreateBuckets_WithInvalidNoOfBuckets_ThrowsError()
        {
            IntegerInclusiveBucketCreator<IntegerInclusiveBucket> bucketCreator = new();

            Assert.Throws<ArgumentOutOfRangeException>(() => { bucketCreator.CreateBuckets(1, 10, -5); });
        }
    }
}