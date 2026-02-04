using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Dashboard
{
    public class PartInformationLoader : ReasonablyResilientBaseLoader<PartDescriptor, string>
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public PartInformationLoader(ISubsetRepository subsetRepository,
            PartsRepositoryMapFile partRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            ILogger<PartInformationLoader> logger)
            : base(partRepository, typeof(PartInformationLoader), logger)
        {
            _subsetRepository = subsetRepository;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        protected override string IdentityPropertyName => PartFields.PaneId;

        protected override string GetIdentity(string[] currentRecord, int identityFieldIndex)
        {
            //  This is a hack. We never use or need a unique identifier for panes,
            //  but the loader code assumes that each row in the CSV has a unique
            //  identifier of some sort. In our case, we'll just return some unique
            //  value and it really doesn't matter what.
            return Guid.NewGuid().ToString("D");
        }

        protected override bool ProcessLoadedRecordFor(
            PartDescriptor targetThing,
            string[] currentRecord,
            string[] headers)
        {
            targetThing.PaneId = FieldExtractor.ExtractString(
                PartFields.PaneId,
                headers,
                currentRecord);

            targetThing.PartType = FieldExtractor.ExtractString(
                PartFields.PartType,
                headers,
                currentRecord);

            targetThing.Spec1 = FieldExtractor.ExtractString(
                PartFields.Spec1,
                headers,
                currentRecord);

            targetThing.Spec2 = FieldExtractor.ExtractString(
                PartFields.Spec2,
                headers,
                currentRecord,
                true);

            targetThing.Spec3 = FieldExtractor.ExtractString(
                PartFields.Spec3,
                headers,
                currentRecord,
                true);

            targetThing.DefaultSplitBy = FieldExtractor.ExtractString(
                PartFields.DefaultSplitBy,
                headers,
                currentRecord,
                true);

            targetThing.HelpText = FieldExtractor.ExtractString(
                PartFields.HelpText,
                headers,
                currentRecord,
                true);

            targetThing.DefaultAverageId = FieldExtractor.ExtractString(
                PartFields.DefaultAverageId,
                headers,
                currentRecord,
                true);

            targetThing.AutoMetrics = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.AutoMetrics,
                headers,
                currentRecord,
                true);

            targetThing.AutoPanes = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.AutoPanes,
                headers,
                currentRecord,
                true);

            targetThing.Roles = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Roles,
                headers,
                currentRecord,
                true);

            targetThing.Ordering = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Ordering,
                headers,
                currentRecord,
                true);

            targetThing.OrderingDirection = FieldExtractor.ExtractEnum(
                CommonMetadataFields.OrderingDirection,
                headers,
                currentRecord,
                DataSortOrder.Ascending,
                true);

            targetThing.Colours = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Colours,
                headers,
                currentRecord,
                true);

            targetThing.Filters = FieldExtractor.ExtractStringArray(
                CommonMetadataFields.Filters,
                headers,
                currentRecord,
                true);

            targetThing.XAxisRange = new AxisRange(FieldExtractor.ExtractDoubleArray(
                CommonMetadataFields.XRange,
                headers,
                currentRecord,
                true));

            targetThing.YAxisRange = new AxisRange(FieldExtractor.ExtractDoubleArray(
                CommonMetadataFields.YRange,
                headers,
                currentRecord,
                true));

            targetThing.Sections = FieldExtractor.ExtractArrayOfStringArray(
                CommonMetadataFields.Sections,
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
