using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using NUnit.Framework;


[TestFixture]
public class DiscreteSetUnionToLookupTests
{
    private static readonly LambdaComparer<(int, int)> CompareFirst = new LambdaComparer<(int, int)>(
        x => x.Item1.GetHashCode(),
        (x, y) => x.Item1 == y.Item1
    );

    private static readonly LambdaComparer<(int, int)> CompareSecond = new LambdaComparer<(int, int)>(
        x => x.Item2.GetHashCode(),
        (x, y) => x.Item2 == y.Item2
    );

    /// <summary>
    /// Example:
    /// A groups like:  [(1,2), (1,3)], [(2,3)], [(4,3), (4,5)]
    /// B groups like:  [(1,2)], [(1,3), (2,3), (4,3)], [(4,5)]
    /// Algorithm makes first element of each group the "parent" of all others for group A
    /// Then loop through each element per B group, setting their "parent" to the biggest parent group of either element.
    /// </summary>
    [Test]
    public void EverythingUnioned_AllElementsInOneGroup()
    {
        // All elements are connected directly or indirectly
        var universe = new[] { (1, 2), (1, 3), (2, 3), (4, 3), (4, 5) };
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        // All should be in a single group
        Assert.That(groups.Length, Is.EqualTo(1));
        Assert.That(groups[0], Is.EquivalentTo(universe));
    }

    [Test]
    public void DisconnectedPairs_SeparateGroups()
    {
        // No overlap between pairs
        var universe = new[] { (1, 2), (3, 4), (5, 6) };
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        // Each pair should be its own group
        Assert.That(groups.Length, Is.EqualTo(3));
        Assert.That(groups, Is.EquivalentTo(new[] { new[] { (1, 2) }, new[] { (3, 4) }, new[] { (5, 6) } }));
    }

    [Test]
    public void OverlappingGroups_MergesCorrectly()
    {
        var universe = new[] { (1, 2), (1, 3), (4, 5) };
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        var expectedGroup1 = new[] { (1, 2), (1, 3) };
        var expectedGroup2 = new[] { (4, 5) };
        Assert.That(groups, Is.EquivalentTo(new[]{expectedGroup1, expectedGroup2}));
    }

    [Test]
    public void SingleElementGroup_HandledCorrectly()
    {
        var universe = new[] { (1, 2) };
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        Assert.That(groups.Length, Is.EqualTo(1));
        Assert.That(groups[0], Is.EquivalentTo(universe));
    }

    [Test]
    public void ChainMerges_SeparateGroupsByTuple()
    {
        // Each tuple is only grouped by its item1 and item2, not as edges
        var universe = new[] { (1, 2), (2, 3), (3, 4) };
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        var expected = new[] { new[] { (1, 2) }, new[] { (2, 3) }, new[] { (3, 4) } };
        Assert.That(groups.Length, Is.EqualTo(3));
        Assert.That(groups, Is.EquivalentTo(expected));
    }

    [Test]
    public void EmptyInput_ReturnsEmpty()
    {
        var universe = new (int, int)[0];
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        Assert.That(groups, Is.Empty);
    }

    [Test]
    public void SelfLoop_HandledAsSingleGroup()
    {
        var universe = new[] { (1, 1), (2, 2), (1, 2) };
        var groups = universe.ToDisjointGroups(CompareFirst, CompareSecond).ToArray();
        // All should be in a single group
        Assert.That(groups.Length, Is.EqualTo(1));
        Assert.That(groups[0], Is.EquivalentTo(universe));
    }


    class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, int> _getHashCode;
        private readonly Func<T, T, bool> _equals;

        public LambdaComparer(Func<T, int> getHashCode, Func<T, T, bool> equals)
        {
            _getHashCode = getHashCode;
            _equals = equals;
        }

        public bool Equals(T x, T y) => _equals(x, y);
        public int GetHashCode(T obj) => _getHashCode(obj);
    }

}
