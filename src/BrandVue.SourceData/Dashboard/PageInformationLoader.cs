using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Dashboard
{
    public class PageInformationLoader : ReasonablyResilientBaseLoader<PageDescriptor, string>
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public PageInformationLoader(
            ISubsetRepository subsetRepository,
            PagesRepositoryMapFile pageRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            ILogger<PageInformationLoader> logger)
            : base(pageRepository, typeof(PageInformationLoader), logger)
        {
            _subsetRepository = subsetRepository;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        protected override string IdentityPropertyName => PageFields.Name;

        protected override string GetIdentity(
            string[] currentRecord, int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex];
        }

        protected override bool ProcessLoadedRecordFor(
            PageDescriptor targetThing,
            string[] currentRecord,
            string[] headers)
        {
            targetThing.DisplayName = FieldExtractor.ExtractString(
                PageFields.DisplayName,
                headers,
                currentRecord,
                true) ?? targetThing.Name;

            targetThing.MenuIcon = FieldExtractor.ExtractString(
                PageFields.MenuIcon,
                headers,
                currentRecord,
                true);

            targetThing.PageType = FieldExtractor.ExtractString(
                PageFields.PageType,
                headers,
                currentRecord,
                true);

            targetThing.HelpText = FieldExtractor.ExtractString(
                PageFields.HelpText,
                headers,
                currentRecord,
                true);
            
            targetThing.MinUserLevel = FieldExtractor.ExtractInteger(
                PageFields.MinUserLevel,
                headers,
                currentRecord,
                100);

            string yoCheckMahBooty = FieldExtractor.ExtractString(
                PageFields.StartPage,
                headers,
                currentRecord,
                true);
            targetThing.StartPage =
                !string.IsNullOrWhiteSpace(yoCheckMahBooty);

            targetThing.Layout = FieldExtractor.ExtractString(
                PageFields.Layout, headers, currentRecord, true);

            targetThing.PageTitle = FieldExtractor.ExtractString(
                PageFields.PageTitle, headers, currentRecord, true);

            targetThing.AverageGroup = FieldExtractor.ExtractStringArray(
                PageFields.AverageGroup,
                headers,
                currentRecord,
                true);

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
