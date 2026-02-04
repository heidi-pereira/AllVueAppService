using System.Data;
using System.IO;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Averages
{
    public class AverageDescriptorMapFileLoader
        : ReasonablyResilientBaseLoader<AverageDescriptor, string>
    {
        private readonly IProductContext _productContext;
        private readonly ISubsetRepository _subsetRepository;

        private readonly AverageDescriptorRepository _averageDescriptorRepository;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public AverageDescriptorMapFileLoader(IProductContext productContext,
            ISubsetRepository subsetRepository,
            AverageDescriptorRepository baseRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            ILogger<AverageDescriptorMapFileLoader> logger)
            : base(baseRepository, typeof(AverageDescriptorMapFileLoader), logger)
        {
            _productContext = productContext;
            _subsetRepository = subsetRepository;
            _averageDescriptorRepository = baseRepository;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        public void Load(string fullyQualifiedPathToCsvDataFile, AllVueConfiguration allVueConfiguration)
        {
            try
            {
                if (allVueConfiguration.AllowLoadFromMapFile && File.Exists(fullyQualifiedPathToCsvDataFile)) base.Load(fullyQualifiedPathToCsvDataFile);

                if (_averageDescriptorRepository.Count == 0)
                {
                    DefaultAverageRepositoryData.CopyFallbackAveragesToRealRepo(_averageDescriptorRepository, _productContext);
                }
            }
            catch (DataException de)
            {
                _logger.LogError(de,
                    "Using default average configuration (14 day, monthly, and quarterly) due to previous DataException encountered reading file {Path}.",
                    fullyQualifiedPathToCsvDataFile);

                DefaultAverageRepositoryData.CopyFallbackAveragesToRealRepo(_averageDescriptorRepository, _productContext);
            }

            _averageDescriptorRepository.InitialiseOrderedList();
        }

        public void TemporaryLoadOnlyFromDataFile(string fullyQualifiedPathToCsvDataFile)
        {
            try
            {
                bool fileExists = File.Exists(fullyQualifiedPathToCsvDataFile);
                if (fileExists) base.Load(fullyQualifiedPathToCsvDataFile);
            }
            catch (DataException) { }

            _averageDescriptorRepository.InitialiseOrderedList();
        }

        protected override string IdentityPropertyName => AverageDescriptorFields.Id;

        protected override string GetIdentity(
            string[] currentRecord,
            int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex];
        }

        protected override bool ProcessLoadedRecordFor(
            AverageDescriptor targetThing,
            string[] currentRecord,
            string[] headers)
        {
            targetThing.DisplayName = FieldExtractor.ExtractString(
                AverageDescriptorFields.DisplayName, headers, currentRecord);

            _commonMetadataFieldApplicator.ApplyAvailability(
                targetThing,
                _subsetRepository,
                headers,
                currentRecord);

            try
            {
                targetThing.Order = FieldExtractor.ExtractInteger(
                    AverageDescriptorFields.Order, headers, currentRecord, int.MaxValue);
            }
            catch (FormatException fe)
            {
                throw new DataException(
                    $@"Invalid value supplied for required field Order in average configuration {
                            JsonConvert.SerializeObject(currentRecord)
                        } with headers {
                            JsonConvert.SerializeObject(headers)
                        }. Average will not be included in dashboard. {fe.Message}",
                    fe);
            }

            try
            {
                targetThing.IsDefault = FieldExtractor.ExtractBoolean(AverageDescriptorFields.IsDefault, headers, currentRecord);
            }
            catch
            {
                targetThing.IsDefault = false;  //Optional field
            }

            try
            {
                targetThing.AllowPartial = FieldExtractor.ExtractBoolean(AverageDescriptorFields.AllowPartial, headers, currentRecord);
            }
            catch
            {
                targetThing.AllowPartial = false;  //Optional field
            }

            targetThing.Group = FieldExtractor.ExtractStringArray(
                AverageDescriptorFields.Group, headers, currentRecord, true);

            targetThing.TotalisationPeriodUnit = FieldExtractor.ExtractEnum<TotalisationPeriodUnit>(
                AverageDescriptorFields.TotalisationPeriodUnit, headers, currentRecord);

            targetThing.NumberOfPeriodsInAverage = FieldExtractor.ExtractInteger(
                AverageDescriptorFields.NumberOfPeriodsInAverage, headers, currentRecord, -1);

            if (targetThing.NumberOfPeriodsInAverage < 1)
            {
                throw new DataException(
                    $@"Error in average definition. Cannot have less that 1 period in average: {
                            targetThing.NumberOfPeriodsInAverage
                        } is not an acceptable value in average definition {
                            JsonConvert.SerializeObject(currentRecord)
                        } with headers {
                            JsonConvert.SerializeObject(headers)
                        }. Have you forgotten to supply a value for {
                            nameof(targetThing.NumberOfPeriodsInAverage)
                        }?");
            }

            targetThing.WeightingMethod = FieldExtractor.ExtractEnum<WeightingMethod>(
                AverageDescriptorFields.WeightingMethod, headers, currentRecord);

            targetThing.WeightAcross = FieldExtractor.ExtractEnum<WeightAcross>(
                AverageDescriptorFields.WeightAcross, headers, currentRecord, WeightAcross.AllPeriods, true);


            targetThing.AverageStrategy = targetThing.WeightingMethod == WeightingMethod.None
                ? AverageStrategy.OverAllPeriods
                : FieldExtractor.ExtractEnum<AverageStrategy>(
                    AverageDescriptorFields.AverageStrategy,
                    headers,
                    currentRecord);

            targetThing.MakeUpTo = FieldExtractor.ExtractEnum<MakeUpTo>(
                AverageDescriptorFields.MakeUpTo, headers, currentRecord);

            targetThing.IncludeResponseIds = FieldExtractor.ExtractBoolean(
                AverageDescriptorFields.IncludeResponseIds, headers, currentRecord);

            return true;
        }
    }
}
