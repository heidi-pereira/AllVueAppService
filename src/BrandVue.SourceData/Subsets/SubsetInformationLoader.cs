using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Subsets
{
    public class SubsetInformationLoader : ReasonablyResilientBaseLoader<Subset, string>
    {
        private IReadOnlyDictionary<string, IReadOnlyDictionary<int, IReadOnlyCollection<string>>> _surveyIdsBySubset;
        private static char[] _delimiters = new[] { '|' };
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public SubsetInformationLoader(SubsetRepository baseRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator, ILogger<SubsetInformationLoader> logger) : base(
                baseRepository, typeof(SubsetInformationLoader), logger)
        {
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        public void Load(string subsetsCsvFullPath, string surveysCsvFullPath)
        {
            _surveyIdsBySubset = ReadPerSubsetSurveyIdAndSegments(surveysCsvFullPath, _logger);
            Load(subsetsCsvFullPath);
        }

        public override void Load(string fullyQualifiedPathToCsvDataFile)
        {
            try
            {
                base.Load(fullyQualifiedPathToCsvDataFile);
            }
            catch (Exception de)
            {
                _logger.LogError(de, "Failed to load subsets from {Path}.", fullyQualifiedPathToCsvDataFile);
                throw;
            }
        }

        protected override string IdentityPropertyName => SubsetFields.Id;

        protected override string GetIdentity(
            string[] currentRecord, int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex];
        }

        protected override bool ProcessLoadedRecordFor(
            Subset targetThing, string[] currentRecord, string[] headers)
        {
            targetThing.DisplayName = FieldExtractor.ExtractString(
                SubsetFields.DisplayName, headers, currentRecord);
            targetThing.DisplayNameShort = FieldExtractor.ExtractString(
                SubsetFields.DisplayNameShort, headers, currentRecord, true);
            if (string.IsNullOrWhiteSpace(targetThing.DisplayNameShort))
            {
                targetThing.DisplayNameShort = targetThing.DisplayName;
            }

            targetThing.Iso2LetterCountryCode = FieldExtractor.ExtractString(
                SubsetFields.Iso2LetterCountryCode, headers, currentRecord).ToLower();
            targetThing.Description = FieldExtractor.ExtractString(
                SubsetFields.Description, headers, currentRecord, true);
            targetThing.Order = FieldExtractor.ExtractInteger(
                SubsetFields.Order, headers, currentRecord, Subset.OrderNotSpecified);

            targetThing.ExternalUrl = FieldExtractor.ExtractString(
                SubsetFields.ExternalUrl, headers, currentRecord, true);

            targetThing.ProductId =
                FieldExtractor.ExtractInteger(SubsetFields.NumericSuffix, headers, currentRecord, int.MinValue);

            CommonMetadataFieldApplicator.ApplyDisabled(targetThing, headers, currentRecord);
            _commonMetadataFieldApplicator.ApplyEnvironment(targetThing, headers, currentRecord);

            targetThing.EnableRawDataApiAccess =
                string.Equals(FieldExtractor.ExtractString(SubsetFields.EnableRawDataApiAccess, headers, currentRecord, true)
                    , "YES", StringComparison.OrdinalIgnoreCase);

            targetThing.Alias =
                FieldExtractor.ExtractString(SubsetFields.Alias, headers, currentRecord, true) ?? targetThing.Id;


            var allowedSegmentNamesOrNull = FieldExtractor.ExtractStringArray(SubsetFields.SegmentNames, headers, currentRecord, true);
            if (allowedSegmentNamesOrNull is not null)
            {
                targetThing.AllowedSegmentNames = allowedSegmentNamesOrNull;
            }

            if (_surveyIdsBySubset.TryGetValue(targetThing.Id, out var surveyIds) && surveyIds.Any())
            {
                targetThing.SurveyIdToSegmentNames = surveyIds;
            }
            else if (!targetThing.Disabled)
            {
                _logger.LogWarning($"Disabling subset `{targetThing.Id}` since it has no associated survey ids");
                targetThing.Disabled = true;
            }

            return true;
        }

        public static IReadOnlyDictionary<string, IReadOnlyDictionary<int, IReadOnlyCollection<string>>> ReadPerSubsetSurveyIdAndSegments(string filenameForSurveysCsv, ILogger logger)
        {
            var surveyRecords = new SimpleCsvReader(logger).ReadCsv<SurveyRecord>(filenameForSurveysCsv);
            return surveyRecords.SelectMany(s => ExtractStringArray(s.SubsetId).Select(subsetId =>
                    (SubsetId: subsetId, s.SurveyId, Segments: ExtractStringArray(s.IncludedSegments))))
                .GroupBy(s => s.SubsetId, s => (s.SurveyId, s.Segments))
                .ToDictionary(g => g.Key,
                    g => (IReadOnlyDictionary<int, IReadOnlyCollection<string>>)g.ToDictionary(s => s.SurveyId, s => s.Segments));
        }

        private static IReadOnlyCollection<string> ExtractStringArray(string pipeSeparated) => pipeSeparated.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries);
    }
}
