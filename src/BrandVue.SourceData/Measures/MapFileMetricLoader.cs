using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Measures
{
    public class MapFileMetricLoader : ReasonablyResilientBaseLoader<Measure, string>
    {
        private readonly IMetricFactory _metricFactory;
        private readonly IProductContext _productContext;
        private readonly bool _marketAverageEnabled;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public MapFileMetricLoader(
            MetricRepository baseRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            ILogger<MapFileMetricLoader> logger,
            IMetricFactory metricFactory,
            IProductContext productContext) : base(baseRepository, typeof(MapFileMetricLoader), logger)
        {
            // Can remove this once it's not used and market average has been used in the Q1 or Q2 2019 reports
            _marketAverageEnabled = false;
            _metricFactory = metricFactory;
            _productContext = productContext;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        }

        protected override string IdentityPropertyName => MetricFields.Name;

        protected override string GetIdentity(
            string[] currentRecord,
            int identityFieldIndex)
        {
            return currentRecord[identityFieldIndex];
        }

        protected override bool ProcessLoadedRecordFor(
            Measure measure,
            string[] currentRecord,
            string[] headers)
        {
            //  Using the field names isn't the most efficient way
            //  of doing this but it's simple and fairly readable.
            //  More importantly it's not performance critical
            //  because the number of measures is small (on the
            //  order of dozens or hundreds) compared with the
            //  number of survey responses.

            var metricConfiguration = new MetricConfiguration
            {
                Id = 0, // Id of metrics coming from map files is 0
                ProductShortCode = _productContext.ShortCode,
                Name = FieldExtractor.ExtractString(MetricFields.Name, headers, currentRecord, true),
                FieldExpression = FieldExtractor.ExtractString(MetricFields.FieldExpression, headers, currentRecord, true),
                Field = FieldExtractor.ExtractString(MetricFields.Field, headers, currentRecord, true),
                Field2 = FieldExtractor.ExtractString(MetricFields.Field2, headers, currentRecord, true),
                FieldOp = FieldExtractor.ExtractString(MetricFields.FieldOperation, headers, currentRecord, true),
                CalcType = FieldExtractor.ExtractString(MetricFields.CalculationType, headers, currentRecord, true),
                TrueVals = FieldExtractor.ExtractString(MetricFields.TrueValues, headers, currentRecord, true),
                BaseExpression = FieldExtractor.ExtractString(MetricFields.BaseExpression, headers, currentRecord, true),
                BaseField = FieldExtractor.ExtractString(MetricFields.BaseField, headers, currentRecord, true),
                BaseVals = FieldExtractor.ExtractString(MetricFields.BaseValues, headers, currentRecord, true),
                MarketAverageBaseMeasure = _marketAverageEnabled ? FieldExtractor.ExtractString(MetricFields.MarketAverageBaseMeasure, headers, currentRecord, true) : null,
                KeyImage = FieldExtractor.ExtractString(MetricFields.KeyImage, headers, currentRecord, true),
                Measure = FieldExtractor.ExtractString(MetricFields.Description, headers, currentRecord, true),
                HelpText = FieldExtractor.ExtractString(MetricFields.HelpText, headers, currentRecord, true),
                VarCode = FieldExtractor.ExtractString(MetricFields.Name, headers, currentRecord, true),
                NumFormat = FieldExtractor.ExtractString(MetricFields.NumberFormatDescriptor, headers, currentRecord, true),
                Min = MetricValueParser.ParseNullableInteger(FieldExtractor.ExtractString(MetricFields.Minimum, headers, currentRecord, true)),
                Max = MetricValueParser.ParseNullableInteger(FieldExtractor.ExtractString(MetricFields.Maximum, headers, currentRecord, true)),
                ExcludeWaves = MetricValueParser.ParseNullableInteger(FieldExtractor.ExtractString(MetricFields.ExcludeWaves, headers, currentRecord, true)),
                StartDate = FieldExtractor.ParseStartDate(MetricFields.StartDate, headers, currentRecord, true)?.DateTime,
                FilterValueMapping = FieldExtractor.ExtractString(MetricFields.FilterValueMapping, headers, currentRecord, true),
                FilterGroup = FieldExtractor.ExtractString(MetricFields.FilterGroup, headers, currentRecord, true),
                FilterMulti = FieldExtractor.ExtractBoolean(MetricFields.FilterMulti, headers, currentRecord),
                PreNormalisationMinimum = MetricValueParser.ParseNullableInteger(FieldExtractor.ExtractString(MetricFields.PreNormalisationMinimum, headers, currentRecord, true)),
                PreNormalisationMaximum = MetricValueParser.ParseNullableInteger(FieldExtractor.ExtractString(MetricFields.PreNormalisationMaximum, headers, currentRecord, true)),
                Subset = FieldExtractor.ExtractString(CommonMetadataFields.Subset, headers, currentRecord, true),
                DisableMeasure = FieldExtractor.ExtractBoolean(MetricFields.DisableMeasure, headers, currentRecord),
                DisableFilter = FieldExtractor.ExtractBoolean(MetricFields.DisableFilter, headers, currentRecord),
                ExcludeList = FieldExtractor.ExtractString(CommonMetadataFields.ExcludeList, headers, currentRecord, true),
                EligibleForMetricComparison = FieldExtractor.ExtractBoolean(MetricFields.EligibleForMetricComparison, headers, currentRecord),
                EligibleForCrosstabOrAllVue = FieldExtractor.ExtractBoolean(MetricFields.EligibleForCrosstabOrAllVue, headers, currentRecord, (FieldExtractor.ExtractBoolean(MetricFields.EligibleForMetricComparison, headers, currentRecord))),
                DownIsGood = FieldExtractor.ExtractBoolean(MetricFields.DownIsGood, headers, currentRecord),
                ScaleFactor = MetricValueParser.ParseNullableDouble(FieldExtractor.ExtractString(MetricFields.ScaleFactor, headers, currentRecord, true)),
            };

            try
            {
                _metricFactory.LoadMetric(metricConfiguration, measure);

                // Environment and Disabled fields are only supported in the map files
                CommonMetadataFieldApplicator.ApplyDisabled(measure, headers, currentRecord);
                _commonMetadataFieldApplicator.ApplyEnvironment(measure, headers, currentRecord);
                if (measure.Disabled)
                {
                    measure.Disabled =
                    measure.DisableMeasure =
                    measure.DisableFilter = true;
                }
            }
            catch (Exception ex)
            {
                LogErrorWithMeasure(measure, ex.Message);
                return false;
            }

            return true;
        }

        private void LogErrorWithMeasure(Measure measure, string errorText)
        {
            _logger.LogError("Measure unavailable '{MeasureName}'. {ErrorMessage} ImportFile : {Path} {@Measure}",
                measure.Name, errorText, _fullyQualifiedPathToCsvDataFile, measure);
        }
    }
}
