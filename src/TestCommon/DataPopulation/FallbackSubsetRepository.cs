using System;
using System.Collections;
using System.Collections.Generic;
using BrandVue.SourceData.Subsets;

namespace TestCommon.DataPopulation
{
    public class FallbackSubsetRepository : ISubsetRepository
    {
        private IDictionary<string, Subset> _subsets =
            new Dictionary<string, Subset>(StringComparer.InvariantCultureIgnoreCase);

        public static Subset UkSubset { get; }= new()
        {
            Id = "UK",
            Index = 0,
            Iso2LetterCountryCode = Iso2LetterCountryCodesLowercase.GB,
            DisplayName = "UK",
            DisplayNameShort = "UK",
            Description = "UK surveyset",
            Alias = "UK"
        };

        public FallbackSubsetRepository()
        {
            _subsets.Add("UK", UkSubset);

            _subsets.Add("US", new Subset()
            {
                Id = "US",
                Index = 1,
                Iso2LetterCountryCode = Iso2LetterCountryCodesLowercase.US,
                DisplayName = "US",
                DisplayNameShort = "US",
                Description = "US surveyset",
                Alias = "US"
            });
        }

        public int Count => _subsets.Count;

        public Subset Get(string identity)
        {
            return _subsets[identity];
        }

        public IEnumerator<Subset> GetEnumerator()
        {
            return _subsets.Values.GetEnumerator();
        }

        public bool HasSubset(string subsetId)
        {
            return _subsets.ContainsKey(subsetId);
        }

        public bool TryGet(string identity, out Subset stored)
        {
            return _subsets.TryGetValue(identity, out stored);
        }



        IEnumerator IEnumerable.GetEnumerator()
        {
            return _subsets.Values.GetEnumerator();
        }
    }
}
