using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData.Dashboard
{
    [TestFixture]
    public class DbFieldConverterTests
    {
        private static FallbackSubsetRepository _mockSubsetRepository = new FallbackSubsetRepository();

        [TestCaseSource(nameof(ArrayOfStringArraysTestCaseSource))]
        public void DecodeArrayOfStringArraysTest((string[][], string) testData)
        {
            var (expectedResult, encodedArrayOfStringArrays) = testData;
            var actualResult = DbFieldConverter.DecodeArrayOfStringArrays(encodedArrayOfStringArrays);
            Assert.That(actualResult, Is.EquivalentTo(expectedResult));
        }

        [TestCaseSource(nameof(ArrayOfStringArraysTestCaseSource))]
        public void EncodeArrayOfStringArraysTest((string[][], string) testData)
        {
            var (arrayOfStringArrays, expectedResult) = testData;
            var actualResult = DbFieldConverter.EncodeArrayOfStringArrays(arrayOfStringArrays);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        public static IEnumerable<(string[][], string)> ArrayOfStringArraysTestCaseSource()
        {
            yield return (new string[][] { new[] { "aaa", "bbb" }, new[] { "ccc", "ddd" } }, "aaa+bbb|ccc+ddd");
            yield return (new string[][] { new[] { "aaa", "bbb" } }, "aaa+bbb");
            yield return (new string[][] { new[] { "aaa" }, new[] { "ccc" } }, "aaa|ccc");
            yield return (new string[][] { }, null);
        }

        [TestCaseSource(nameof(StringToArrayOfStringsTestCaseSource))]
        public void DecodeArrayOfStringsTest((string, string[]) testData)
        {
            var (encodedArrayOfStringArrays, expectedResult) = testData;
            var actualResult = DbFieldConverter.DecodeArrayOfStrings(encodedArrayOfStringArrays);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [TestCaseSource(nameof(ArrayOfStringsToStringTestCaseSource))]
        public void EncodeArrayOfStringsTest((string[], string) testData)
        {
            var (arrayOfStringArrays, expectedResult) = testData;
            var actualResult = DbFieldConverter.EncodeArrayOfStrings(arrayOfStringArrays);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        public static IEnumerable<(string[], string)> ArrayOfStringsToStringTestCaseSource()
        {
            yield return (new[] { "aaa", "bbb" }, "aaa|bbb");
            yield return (new[] { "aaa" }, "aaa");
            yield return (new string[] { }, null);
            yield return (null, null);
        }

        public static IEnumerable<(string, string[])> StringToArrayOfStringsTestCaseSource()
        {
            yield return ("aaa|bbb", new[] { "aaa", "bbb" });
            yield return ("aaa", new[] { "aaa" });
            yield return (string.Empty, null);
            yield return (null, null);
        }

        [TestCaseSource(nameof(AxisRangeTestCaseSource))]
        public void DecodeAxisRangeTest((AxisRange, string) testData)
        {
            var (expectedAxisRange, encodedString) = testData;
            var actualAxisRange = DbFieldConverter.DecodeAxisRange(encodedString);
            Assert.That(actualAxisRange, Is.EqualTo(expectedAxisRange));
        }

        [TestCaseSource(nameof(AxisRangeTestCaseSource))]
        public void EncodeAxisRangeTest((AxisRange, string) testData)
        {
            var (axisRange, expectedEncoding) = testData;
            var actualEncoding = DbFieldConverter.EncodeAxisRange(axisRange);
            Assert.That(actualEncoding, Is.EqualTo(expectedEncoding));
        }

        public static IEnumerable<(AxisRange, string)> AxisRangeTestCaseSource()
        {
            yield return (new AxisRange() { Min = 10, Max = 20 }, "10|20");
            yield return (new AxisRange() { Min = 10, Max = null }, "10|");
            yield return (new AxisRange() { Min = null, Max = 20 }, "|20");
            yield return (new AxisRange() { Min = null, Max = null}, null);
        }

        
        [TestCaseSource(nameof(DataSortOrderTestCaseSource))]
        public void EncodeDataSortOrderTest((DataSortOrder,string) testData)
        {
            var (dataSortOrder, expectedEncoding) = testData;
            var actualEncoding = DbFieldConverter.EncodeDataSortOrder(dataSortOrder);
            Assert.That(actualEncoding, Is.EqualTo(expectedEncoding));
        }

        [TestCaseSource(nameof(DataSortOrderTestCaseSource))]
        public void DecodeDataSortOrderTest((DataSortOrder, string) testData)
        {
            var (expectedDataSortOrder, encodedString) = testData;
            var actualDataSortOrder = DbFieldConverter.DecodeDataSortOrder(encodedString);
            Assert.That(actualDataSortOrder, Is.EqualTo(expectedDataSortOrder));
        }

        [TestCase(DataSortOrder.Ascending, "")]
        [TestCase(DataSortOrder.Ascending, "Asc")]
        [TestCase(DataSortOrder.Ascending, "Desc")]
        [TestCase(DataSortOrder.Ascending, "some junk text")]
        public void DecodeDataSortOrderDefaultToAscendingTest(DataSortOrder expectedDataSortOrder, string encodedString)
        {
            var actualDataSortOrder = DbFieldConverter.DecodeDataSortOrder(encodedString);
            Assert.That(actualDataSortOrder, Is.EqualTo(expectedDataSortOrder));
        }

        public static IEnumerable<(DataSortOrder, string)> DataSortOrderTestCaseSource()
        {
            yield return (DataSortOrder.Ascending, "Ascending");
            yield return (DataSortOrder.Descending, "Descending");
        }

        [TestCaseSource(nameof(EncodeSubsetTestCaseSource))]
        public void EncodeSubsetTests((Subset[], string) testData)
        {
            var (subsets, expectedEncoding) = testData;
            var actualEncoding = DbFieldConverter.EncodeSubsets(subsets);
            Assert.That(actualEncoding, Is.EqualTo(expectedEncoding));
        }

        [TestCaseSource(nameof(DecodeSubsetTestCaseSource))]
        public void DecodeSubsetTests((string, Subset[]) testData)
        {
            var (encodedString, expectedSubsets) = testData;
            var actualSubsets = DbFieldConverter.DecodeSubsets(_mockSubsetRepository, encodedString);
            Assert.That(actualSubsets, Is.EqualTo(expectedSubsets));
        }

        [TestCaseSource(nameof(DecodeSubsetHandlesJunkTextTestCaseSource))]
        public void DecodeSubsetHandlesJunkTextTest((string, Subset[]) testData)
        {
            var (encodedString, expectedSubsets) = testData;
            var actualSubsets = DbFieldConverter.DecodeSubsets(_mockSubsetRepository, encodedString);
            Assert.That(actualSubsets, Is.EqualTo(expectedSubsets));
        }

        public static IEnumerable<(Subset[], string)> EncodeSubsetTestCaseSource()
        {
            yield return (null, null);
            yield return (Array.Empty<Subset>(), null);
            yield return (_mockSubsetRepository.ToArray(), "UK|US");
            yield return (new Subset[] { _mockSubsetRepository.Get("UK") }, "UK");
        }

        public static IEnumerable<(string, Subset[])> DecodeSubsetTestCaseSource()
        {
            yield return (null, null);
            yield return (string.Empty, null);
            yield return ("UK|US", _mockSubsetRepository.ToArray());
            yield return ("UK", new Subset[] { _mockSubsetRepository.Get("UK") });
        }

        public static IEnumerable<(string, Subset[])> DecodeSubsetHandlesJunkTextTestCaseSource()
        {
            yield return (null, null);
            yield return ("||hjfd|hjjkfa|uhu", Array.Empty<Subset>());
            yield return ("||UK|hjjkfa|uhu", new Subset[] {_mockSubsetRepository.Get("UK")});
        }
    }
}
