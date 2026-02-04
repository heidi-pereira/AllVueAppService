using BrandVue.SourceData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Utils
{
    internal class SequenceComparerTests
    {
        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("0", "0")]
        [TestCase("1", "1")]
        [TestCase("1,2", "1,2")]
        [TestCase("-3,4,1", "-3,4,1")]
        [TestCase("-3,_,1", "-3,_,1")]
        [TestCase(null, "")]
        [TestCase("", "_")]
        [TestCase("0", "1")]
        [TestCase("1", "1,2")]
        [TestCase("1", "1,_")]
        [TestCase("1,2", "1,2,0")]
        [TestCase("-3,4,1", "-3,4,1")]
        [TestCase("-3,4,1", "-3,_,1")]
        public void EqualityWorksConsistently(string a, string b)
        {
            bool strEqual = a == b;
            var arrayA = ParseNullableIntArray(a);
            var arrayB = ParseNullableIntArray(b);
            var comparer = SequenceComparer<int?>.ForArray();
            bool lr = comparer.Equals(arrayA, arrayB);
            bool rl = comparer.Equals(arrayB, arrayA);

            Assert.That(lr, Is.EqualTo(rl), "Equality should be the same regardless of argument order");
            Assert.That(lr, Is.EqualTo(strEqual), "Equality should be the same as string equality");
        }

        private static int?[] ParseNullableIntArray(string a)
        {
            if (a == "") return Array.Empty<int?>();
            return a?.Split(',').Select(x => int.TryParse(x, out int i) ? i : default(int?)).ToArray();
        }
    }
}
