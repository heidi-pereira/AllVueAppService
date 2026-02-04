using System.Collections.Generic;
using System.Linq;

namespace MIG.SurveyPlatform.MapGeneration.Model
{
    /// <summary>
    /// These match up to two of the tabs in the map file. The information is derived from the mqml.
    /// </summary>
    internal class FieldCollections
    {
        public IReadOnlyCollection<IFieldDefinition> ProfileFields { get; }
        public IReadOnlyCollection<FieldDefinition> BrandFields { get; }

        public FieldCollections(IEnumerable<IFieldDefinition> profileFields, IEnumerable<FieldDefinition> brandFields)
        {
            ProfileFields = profileFields.ToList();
            BrandFields = brandFields.ToList();
        }
    }
}