using System.Data;
using System.IO;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;


namespace BrandVue.SourceData.Filters
{
    public class FilterDescriptorLoader
        : ReasonablyResilientBaseLoader<FilterDescriptor, string>
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly FilterRepository _filterRepository;
        protected override string IdentityPropertyName => FilterFields.Name;
        protected string IdentityOptionalPropertyName => CommonMetadataFields.Subset;
        private int IdentityOptionalPropertyIndex;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public FilterDescriptorLoader(ISubsetRepository subsetRepository,
            FilterRepository baseRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            ILogger<FilterDescriptorLoader> logger)
            : base(baseRepository, typeof(FilterDescriptorLoader), logger)
        {
            _subsetRepository = subsetRepository;
            _filterRepository = baseRepository;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        public override void Load(string fullyQualifiedPathToCsvDataFile)
        {
            try
            {
                bool fileExists = File.Exists(fullyQualifiedPathToCsvDataFile);
                if (fileExists) base.Load(fullyQualifiedPathToCsvDataFile);
            }
            catch (DataException de)
            {
                _logger.LogError(de, "Error loading filters from {Path}", fullyQualifiedPathToCsvDataFile);
            }
        }

        protected override int GetIdentityFieldIndex(string[] fieldHeaders)
        {
            IdentityOptionalPropertyIndex = Array.FindIndex(
                fieldHeaders,
                header => string.Equals(
                    header,
                    IdentityOptionalPropertyName,
                    StringComparison.OrdinalIgnoreCase));


            return base.GetIdentityFieldIndex(fieldHeaders);
        }

        protected override string GetIdentity(
            string[] currentRecord,
            int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex] + (IdentityOptionalPropertyIndex >=0 ? currentRecord[IdentityOptionalPropertyIndex] : "");
        }

        protected override bool ProcessLoadedRecordFor(
            FilterDescriptor targetThing,
            string[] currentRecord,
            string[] headers)
        {
            
            targetThing.Name = FieldExtractor.ExtractString(FilterFields.Name, headers, currentRecord);
            targetThing.DisplayName = FieldExtractor.ExtractString(FilterFields.DisplayName, headers, currentRecord, true);
            _commonMetadataFieldApplicator.ApplyAvailability(
                targetThing,
                _subsetRepository,
                headers,
                currentRecord);

            targetThing.Categories = FieldExtractor.ExtractString(FilterFields.Categories, headers, currentRecord);
            targetThing.Field = FieldExtractor.ExtractString(FilterFields.Field, headers, currentRecord);
            targetThing.FilterValueType = FieldExtractor.ExtractEnum<FilterValueTypes>(FilterFields.ValueType, headers, currentRecord);
            return FieldExtractor.ExtractBoolean(FilterFields.Active, headers, currentRecord);
        }
    }
}