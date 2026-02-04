using BrandVue.SourceData.CommonMetadata;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Subsets
{
    public class Subset : IDisableable, IEnvironmentConfigurable, IEquatable<Subset>
    {
        public const int IndexHasNotBeenSet = int.MinValue;
        public const int OrderNotSpecified = int.MaxValue;

        private int _index = IndexHasNotBeenSet;

        public Subset()
        {
        }

        public string Id { get; set; }

        public int Index
        {
            get { return _index; }
            set
            {
                if (_index != IndexHasNotBeenSet)
                {
                    throw new InvalidOperationException(
                        $@"Index on subset {
                            this
                        } has already been set to {
                            _index
                        } and cannot be set again.");
                }

                _index = value;
            }
        }

        public string DisplayName { get; set; }
        public string DisplayNameShort { get; set; }
        public string Iso2LetterCountryCode { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool Disabled { get; set; }
        public string ExternalUrl { get; set; }
        public string [] Environment { get; set; }
        public bool EnableRawDataApiAccess { get; set; }
        public DateTimeOffset? OverriddenStartDate { get; set; }
        public bool AlwaysShowDataUpToCurrentDate { get; set; }
        public string? ParentGroupName { get; set; }

        /// <summary>
        /// Force Vue to consider a timespan of this length "complete" so it can average over that period
        /// </summary>
        public TimeSpan MinimumDataSpan { get; set; } = TimeSpan.Zero;
        public string Alias { get; set; }
        /// <summary>
        /// This won't be required once BrandVue supports products directly rather than through subsets.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// If there are no specified segment names, use all (except test, which isn't in the data warehouse)
        /// </summary>
        [JsonIgnore] //The UI doesn't need to know about this
        public IReadOnlyDictionary<int, IReadOnlyCollection<string>> SurveyIdToSegmentNames { get; set; }
        /// <summary>
        /// This gets initialized in AdjustForAnswersTable, no need to set it elsewhere
        /// </summary>
        [JsonIgnore] //The UI doesn't need to know about this
        public IReadOnlyCollection<int> SegmentIds { get; set; }

        /// <summary>
        /// If there are no specified segment names, use all, this was a map file mechanism
        /// </summary>
        [JsonIgnore] //The UI doesn't need to know about this
        public IReadOnlyCollection<string> AllowedSegmentNames { get; set; } = Array.Empty<string>();

        public bool Equals(Subset other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            var subset = obj as Subset;
            return Equals(subset);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Subset first, Subset second)
        {
            if (first is null)
            {
                return second is null;
            }

            return first.Equals(second);
        }

        public static bool operator !=(Subset first, Subset second)
        {
            return !(first == second);
        }

        public override string ToString()
        {
            return $"{Id} ({DisplayName})";
        }
    }
}
