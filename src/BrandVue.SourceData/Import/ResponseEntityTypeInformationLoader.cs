using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import 
{
    public class ResponseEntityTypeInformationLoader : ReasonablyResilientBaseLoader<EntityType, string>
    {
        public ResponseEntityTypeInformationLoader(EntityTypeRepository baseRepository, ILogger<ResponseEntityTypeInformationLoader> logger) : base(baseRepository, typeof(EntityTypeRepository), logger) { }
        
        protected override string IdentityPropertyName => "Type";
        private const string DisplayNameSingularHeader = "DisplayNameSingular";
        private const string DisplayNamePluralHeader = "DisplayNamePlural";
        private const string SurveyChoiceSetNames = "SurveyChoiceSetNames";
        
        protected override string GetIdentity(string[] currentRecord, int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex].ToLower();
        }

        protected override bool ProcessLoadedRecordFor(EntityType targetThing, string[] currentRecord, string[] headers)
        {
            targetThing.DisplayNameSingular = FieldExtractor.ExtractString(DisplayNameSingularHeader, headers, currentRecord);
            targetThing.DisplayNamePlural = FieldExtractor.ExtractString(DisplayNamePluralHeader, headers, currentRecord);
            targetThing.CreatedFrom = EntityTypeCreatedFrom.QuestionField;
            var csNames = FieldExtractor.ExtractStringArray(SurveyChoiceSetNames, headers, currentRecord, true)?.ToHashSet();
            if (csNames != null) targetThing.SurveyChoiceSetNames = csNames;

            return true;
        }
    }
}