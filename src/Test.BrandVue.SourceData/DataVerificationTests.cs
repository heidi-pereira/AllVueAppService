using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Import;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    public class DataVerificationTests
    {
        private const string ProductName = "Test.barometer";

        private static readonly LowRentCsvLoader LowRentCsvLoader = new LowRentCsvLoader(Substitute.For<ILogger<LowRentCsvLoader>>());
        private static readonly AppSettings AppSettings = TestLoaderSettings.WithProduct(ProductName).AppSettings;
        private static readonly MetadataPaths MetadataPaths = new MetadataPaths(AppSettings);
        private static readonly IBrandVueDataLoaderSettings ConfigurationSourcedLoaderSettings = new ConfigurationSourcedLoaderSettings(AppSettings);

        [Test]
        public void ReferencesFromDashPartsToMeasuresAreCorrect()
        {
            var validIds = GetDistinctValuesForColumns(ConfigurationSourcedLoaderSettings.MeasureMetadataFilepath, "name");
            var attemptedReferences = GetDistinctValuesForColumns(MetadataPaths.DashParts, row => row["partType"] != "Text", "spec1");
            var invalidMeasures = GetInvalidReferences(attemptedReferences, validIds);
            Assert.That(invalidMeasures, Is.Empty);
        }

        [Test]
        public void ReferencesFromDashPartsToDashPanesAreCorrect()
        {
            var validIds = GetDistinctValuesForColumns(MetadataPaths.DashPanes, "id");
            var attemptedReferences = GetDistinctValuesForColumns(MetadataPaths.DashParts, "paneId");
            var invalidMeasures = GetInvalidReferences(attemptedReferences, validIds);
            Assert.That(invalidMeasures, Is.Empty);
        }

        [Test]
        public void ReferencesFromDashPanesToDashPagesAreCorrect()
        {
            var validIds = GetDistinctValuesForColumns(MetadataPaths.DashPages, "name");
            var attemptedReferences = GetDistinctValuesForColumns(MetadataPaths.DashPanes, "pageName");
            var invalidMeasures = GetInvalidReferences(attemptedReferences, validIds);
            Assert.That(invalidMeasures, Is.Empty);
        }

        private static HashSet<string> GetValidMeasureNames()
        {
            var subsets = new FallbackSubsetRepository();
            var validMeasureNames = new[]
            {
                ConfigurationSourcedLoaderSettings.BrandResponseDataFilepath(subsets.Get("UK")),
                ConfigurationSourcedLoaderSettings.RespondentProfileDataFilepath(subsets.Get("US"))
            }.SelectMany(LowRentCsvLoader.GetColumnNames);
            return new HashSet<string>(validMeasureNames);
        }

        private static HashSet<string> GetDistinctValuesForColumns(string fullyQualifiedPathNameToCsv, params string[] colNames)
        {
            return GetDistinctValuesForColumns(fullyQualifiedPathNameToCsv, _ => true, colNames);
        }

        private static HashSet<string> GetDistinctValuesForColumns(string fullyQualifiedPathNameToCsv, Func<IDictionary<string, string>, bool> rowsWhere, params string[] colNames)
        {
            Assert.That(LowRentCsvLoader.GetColumnNames(fullyQualifiedPathNameToCsv), Is.SupersetOf(colNames), $"Column names should be present in file {fullyQualifiedPathNameToCsv}");
            var measureMetadata = LowRentCsvLoader.Load(fullyQualifiedPathNameToCsv, false).Where(rowsWhere).ToList();
            var allMeasureForeignKeys = colNames.SelectMany(key => GetValuesForColumn(measureMetadata, key));
            return new HashSet<string>(allMeasureForeignKeys);
        }

        private static IEnumerable<string> GetValuesForColumn(List<IDictionary<string, string>> measureMetadata, string key)
        {   //Note the Split('|') which is valid for lots of columns we use. If an id included a pipe character this would pretty much go to hell.
            return measureMetadata.SelectMany(row => row.TryGetValue(key, out var value) ? value.Split('|') : new string[0]).Where(s => !string.IsNullOrWhiteSpace(s));
        }

        private static IEnumerable<string> GetInvalidReferences(IEnumerable<string> allMeasureForeignKeys, HashSet<string> validMeasureNames)
        {
            return allMeasureForeignKeys.Where(measureName => !validMeasureNames.Contains(measureName))
                .Select(measure => $"Reference to '{measure}' is invalid");
        }
    }
}
