using System.Collections.Generic;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    internal class MapData
    {
        public FieldCollections FieldCollections { get; set; }
        public string BrandAskedQuestion { get; set; }
        public List<string> BrandAskedChoices { get; set; }
        public IEnumerable<Subset> Subsets { get; set; }
    }
}