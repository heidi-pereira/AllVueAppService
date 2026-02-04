namespace BrandVue.SourceData.QuotaCells
{
    /// <summary>
    /// AKA Weighting cell
    /// Every respondent with the same quota cell, will be weighted identically if they are within the same time period for the average being used.
    ///
    /// The "quota" part of the name:
    /// When gathering respondents, we tend to set *quotas* for various combinations of gender, age, region, etc.
    /// Each combination of values forms a "cell". For example we may aim to get 20 men aged 16-24 from London.
    /// Since it's very hard to perfectly meet the desired sample (usually a nationally representative sample), we weight responses along those same lines so that the results more accurately represent the quotas intended for the population.
    /// </summary>
    public class QuotaCell : IEquatable<QuotaCell>
    {
        internal const char PartSeparator = ':';
        public int Id { get; }
        private const int NotYetIndexed = -1;
        private const int UnweightedCellId = -1;

        private string _stringRepresentation;
        public IReadOnlyDictionary<string, string> FieldGroupToKeyPart { get; }

        /// <summary>
        /// Prefer this constructor
        /// </summary>
        public QuotaCell(int id, Subset subset, IReadOnlyDictionary<string, int> fieldGroupToKeyPart,
            int? weightingGroupId = null)
            : this(id, subset, fieldGroupToKeyPart?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()), weightingGroupId)
        {
        }

        /// <summary>
        /// Prefer other constructor. Only legacy brandvues should have non-integer values in the dictionary
        /// </summary>
        public QuotaCell(int id, Subset subset, IReadOnlyDictionary<string, string> fieldGroupToKeyPart, int? weightingGroupId = null, bool isResponseLevelWeighting = false)
        {
            Id = id;
            Subset = subset;
            WeightingGroupId = weightingGroupId;
            FieldGroupToKeyPart = fieldGroupToKeyPart;
            IsResponseLevelWeighting = isResponseLevelWeighting;
        }

        public static Dictionary<string, string> DefaultCellDefinition(string region, string gender, string ageGroup, string socioEconomicGroup)
        {
            return new Dictionary<string, string> {{DefaultQuotaFieldGroups.Region, region}, {DefaultQuotaFieldGroups.Gender, gender}, {DefaultQuotaFieldGroups.Age, ageGroup}, {DefaultQuotaFieldGroups.Seg, socioEconomicGroup}};
        }

        public Subset Subset { get; }
        public int? WeightingGroupId { get; }

        public bool IsResponseLevelWeighting { get ; }

        public int Index { get; internal set; } = NotYetIndexed;

        public string GetKeyPartForFieldGroup(string fieldGroupName) => FieldGroupToKeyPart[fieldGroupName];

        public bool Equals(QuotaCell other) => other != null && Id == other.Id;

        public override bool Equals(object obj) => Equals(obj as QuotaCell);

        public override string ToString() => _stringRepresentation ??= GenerateKey(FieldGroupToKeyPart.Values);

        public static string GenerateKey(IEnumerable<string> values) => string.Join(PartSeparator, values);

        public override int GetHashCode() => Id;
        public const string Unweighted = nameof(Unweighted);

        public bool IsUnweightedCell => Id == UnweightedCellId;

        internal static QuotaCell UnweightedQuotaCell(Subset subset) =>
            new(UnweightedCellId, subset, DefaultCellDefinition(Unweighted, Unweighted, Unweighted, Unweighted), null) {Index = 0};
    }
}
