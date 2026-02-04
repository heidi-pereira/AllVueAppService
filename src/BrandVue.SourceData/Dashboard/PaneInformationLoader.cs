using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Dashboard
{
    public class PaneInformationLoader : ReasonablyResilientBaseLoader<PaneDescriptor, string>
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public PaneInformationLoader(ISubsetRepository subsetRepository,
            PanesRepositoryMapFile paneRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            ILogger<PaneInformationLoader> logger)
            : base(paneRepository, typeof(PaneInformationLoader), logger)
        {
            _subsetRepository = subsetRepository;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        protected override string IdentityPropertyName => PaneFields.Id;

        protected override string GetIdentity(
            string[] currentRecord, int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex];
        }

        protected override bool ProcessLoadedRecordFor(
            PaneDescriptor targetThing,
            string[] currentRecord,
            string[] headers)
        {
            targetThing.PageName = FieldExtractor.ExtractString(
                PaneFields.PageName,
                headers,
                currentRecord);

            targetThing.Height = FieldExtractor.ExtractInteger(
                PaneFields.Height,
                headers,
                currentRecord,
                500);

            targetThing.PaneType = FieldExtractor.ExtractString(
                PaneFields.PaneType,
                headers,
                currentRecord,
                true) ?? "Standard";

            targetThing.Spec = FieldExtractor.ExtractString(
                PaneFields.Spec,
                headers,
                currentRecord,
                true);

            targetThing.Spec2 = FieldExtractor.ExtractString(
                PaneFields.Spec2,
                headers,
                currentRecord,
                true);

            targetThing.View = FieldExtractor.ExtractInteger(
                PaneFields.View,
                headers,
                currentRecord,
                int.MinValue);

            targetThing.Roles = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Roles,
                headers,
                currentRecord,
                true);

            _commonMetadataFieldApplicator.ApplyAvailability(
                targetThing,
                _subsetRepository,
                headers,
                currentRecord);

            return true;
        }
    }
}
