using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;

namespace BrandVue.EntityFramework.Answers.Model;
//id, name, count in debuggerdisplay using nameof on each property in interpolated string
[DebuggerDisplay($"{{{nameof(ChoiceSetId)}}}: {{{nameof(Name)}}} ({{{nameof(Choices)}?.{nameof(Choices.Count)}}} choices)")]


public class ChoiceSet
{
    private static readonly IEqualityComparer<SortedSet<int>> SetComparer = SortedSet<int>.CreateSetComparer();

    private IReadOnlyDictionary<int, Choice> _choiceDictionary;
    private int? _cachedSurveyChoiceIdsHash;

    private int? _cachedRootAncestorIdsHash;
    private IReadOnlyList<ChoiceSet> _cachedAncestors;
    private IReadOnlySet<int> _cachedRootAncestorIds;

    public int ChoiceSetId { get; set; }

    [Required]
    public int SurveyId { get; set; }
    [Required, MaxLength(500)]
    public string Name { get; set; }

    public ChoiceSet ParentChoiceSet1 { get; set; }
    public ChoiceSet ParentChoiceSet2 { get; set; }

    public IList<Choice> Choices { get; set; }
    public int? ParentChoiceSet1Id { get; set; }
    public int? ParentChoiceSet2Id { get; set; }
    public ICollection<ChoiceSet> DirectDescendants { get; set; }
    public ICollection<ChoiceSet> AddedDescendants { get; set; }

    [NotMapped]
    public IReadOnlyDictionary<int, Choice> ChoicesBySurveyChoiceId => _choiceDictionary ??= Choices.ToDictionary(c => c.SurveyChoiceId);

    [NotMapped]
    public int SurveyChoiceIdsHash => _cachedSurveyChoiceIdsHash ??= SetComparer.GetHashCode([.. Choices.Select(c => c.SurveyChoiceId)]);

    [NotMapped]
    public int RootAncestorIdsHash => _cachedRootAncestorIdsHash ??= SetComparer.GetHashCode([.. RootAncestorIds]);

    [NotMapped]
    public IReadOnlyList<ChoiceSet> Ancestors =>_cachedAncestors ??= this.FollowMany(t => [t.ParentChoiceSet1, t.ParentChoiceSet2]).ToList();

    [NotMapped]
    public IReadOnlySet<int> RootAncestorIds => _cachedRootAncestorIds ??= CalculateRootAncestorIds();

    private IReadOnlySet<int> CalculateRootAncestorIds()
    {
        // A "root" choice set is one with no parents that statically defines its choices rather than inherit them
        if (ParentChoiceSet1 == null)
        {
            Debug.Assert(ParentChoiceSet2 == null, "ParentChoiceSet2 should never be set when ParentChoiceSet1 is not");
            return new HashSet<int> { ChoiceSetId };
        }

        // Same as parent1 - might as well reuse for memory/cpu efficiency
        var rootAncestorIds = ParentChoiceSet1.RootAncestorIds;

        // Union in parent2's if they exist
        if (ParentChoiceSet2 != null)
        {
            rootAncestorIds = rootAncestorIds.Union(ParentChoiceSet2.RootAncestorIds).ToHashSet();
        }

        return rootAncestorIds;
    }
}

public class ChoiceSetNameIdComparer : IEqualityComparer<ChoiceSet>
{
    public static IEqualityComparer<ChoiceSet> Instance { get; } = new ChoiceSetNameIdComparer();

    public bool Equals(ChoiceSet cs1, ChoiceSet cs2)
    {
        if (ReferenceEquals(cs1, cs2)) return true;
        if (cs1 == null ^ cs2 == null) return false;
        if (cs1.SurveyChoiceIdsHash != cs2.SurveyChoiceIdsHash) return false;

        return cs1.Choices.OrderBy(c => c.SurveyChoiceId).SequenceEqual(cs2.Choices.OrderBy(c => c.SurveyChoiceId), ChoiceEqualityComparer.Instance);
    }

    public int GetHashCode(ChoiceSet obj) => obj.SurveyChoiceIdsHash;
}

public class ChoiceEqualityComparer : IEqualityComparer<Choice>
{
    public static IEqualityComparer<Choice> Instance { get; } = new ChoiceEqualityComparer();

    public bool Equals(Choice x, Choice y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null ^ y == null) return false;
        return x.SurveyChoiceId == y.SurveyChoiceId && StringComparer.OrdinalIgnoreCase.Equals(x.GetDisplayName(), y.GetDisplayName());
    }

    public int GetHashCode(Choice obj) => (obj.SurveyChoiceId, obj.GetDisplayName()).GetHashCode();
}

public class ChoiceSetAncestryComparer : IEqualityComparer<ChoiceSet>
{
    public static IEqualityComparer<ChoiceSet> Instance { get; } = new ChoiceSetAncestryComparer();

    public bool Equals(ChoiceSet cs1, ChoiceSet cs2)
    {
        if (ReferenceEquals(cs1, cs2)) return true;
        if (cs1 == null ^ cs2 == null) return false;

        return cs1.RootAncestorIds.SetEquals(cs2.RootAncestorIds);
    }

    public int GetHashCode(ChoiceSet obj) => obj.RootAncestorIdsHash;
}

public enum ChoiceSetType
{
    AnswerChoiceSet,
    PageChoiceSet,
    SectionChoiceSet,
    QuestionChoiceSet
}