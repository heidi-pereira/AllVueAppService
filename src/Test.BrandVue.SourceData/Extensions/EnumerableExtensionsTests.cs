using NUnit.Framework;
using System;
using BrandVue.EntityFramework;
using System.Linq;

namespace Test.BrandVue.SourceData.Extensions
{
    [TestFixture(false)]
    [TestFixture(true)]
    public class EnumerableExtensionsTests
    {
        private readonly bool _arrayImplementation;
        public EnumerableExtensionsTests(bool arrayImplementation) => _arrayImplementation = arrayImplementation;

        [TestCase]
        [TestCase(new int[0])]
        [TestCase(new int[0], new int[0])]
        [TestCase(new int[0], new[]{1})]
        [TestCase(new[]{1,2}, new[]{3}, new[]{4}, new int[0])]
        public void ShouldReturnEmptySet(params int[][] sets) =>
            Assert.That(CartesianProduct(sets), Is.EquivalentTo(Array.Empty<int[]>()));

        [TestCase(new[]{1}, new[]{2}, ExpectedResult = new[]{"1,2"})]
        [TestCase(new[]{1,2}, new[]{3}, ExpectedResult = new[]{"1,3", "2,3"})]
        [TestCase(new[]{1}, new[]{2}, new[]{3}, ExpectedResult = new[]{"1,2,3"})]
        [TestCase(new[]{1,2}, new[]{3}, new[]{4}, ExpectedResult = new[]{"1,3,4", "2,3,4"})]
        [TestCase(new[]{1,2}, new[]{3}, new[]{4}, new[]{5}, ExpectedResult = new[]{"1,3,4,5", "2,3,4,5"})]
        [TestCase(new[]{1}, new[]{2,3}, new[]{4,5}, new[]{6}, ExpectedResult = new[]{"1,2,4,6", "1,2,5,6", "1,3,4,6", "1,3,5,6"})]
        public string[] ShouldReturn(params int[][] sets) =>
            CartesianProduct(sets).Select(a => string.Join(",", a)).ToArray();

        private int[][] CartesianProduct(int[][] sets) =>
            _arrayImplementation ? EnumerableExtensions.CartesianProduct(sets) : sets.CartesianProduct().Select(x => x.ToArray()).ToArray();
    }
}
