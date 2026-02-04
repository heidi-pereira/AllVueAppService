using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import 
{
    public class EntityInstanceInformationLoader : ReasonablyResilientBaseIdentifiableLoader<EntityInstance>
    {
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;
        private readonly ISubsetRepository _subsetRepository;

        public EntityInstanceInformationLoader(MapFileEntityInstanceRepository mapFileEntityInstanceRepository,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator, ISubsetRepository subsetRepository,
            ILogger<EntityInstanceInformationLoader> logger) : base(mapFileEntityInstanceRepository, typeof(EntityInstanceInformationLoader), logger)
        {
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
            _subsetRepository = subsetRepository;
        }

        protected override bool ProcessLoadedRecordFor(EntityInstance instance, string[] currentRecord, string[] headers)
        {
            int fieldCount = currentRecord.Length;
            for (int i = 0; i < fieldCount; i++)
            {
                string headerName = headers[i];

                switch (headerName)
                {
                    case EntityInstanceFields.Name:
                        instance.Identifier = currentRecord[i];
                        instance.Name = currentRecord[i];
                        break;
                    case EntityInstanceFields.Subset:
                        var subsets = FieldExtractor.ExtractStringArray(EntityInstanceFields.Subset, headers, currentRecord);
                        if (subsets != null)
                        {
                            instance.Subsets = subsets.Select(s => _subsetRepository.Get(s)).ToArray();
                        }
                        break;
                    case CommonMetadataFields.StartDate:
                        {
                            var dateTimeAsString = currentRecord[i];
                            if (!string.IsNullOrEmpty(dateTimeAsString))
                            {
                                try
                                {
                                    var entityInstanceStartDate = DateTimeOffsetExtensions.ParseDate(dateTimeAsString, "dd/MM/yyyy");
                                    // Terrible hack which relies on the subsets being loaded before the startDates,
                                    // but that's how we have them in the map file atm

                                    foreach (var subset in instance.Subsets)
                                    {
                                        instance.StartDateBySubset[subset.Id] = entityInstanceStartDate;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //  Problems with start dates aren't a dashboard
                                    //  breaking error, but errors in specification should be
                                    //  logged.
                                    _logger.LogError(ex,
                                        "Invalid start date specification for entity instance '{EntityInstanceName}' with ID {EntityInstanceId}: {CurrentRecord}",
                                        instance.Name, instance.Id, dateTimeAsString);
                                }
                            }
                        }
                        break;
                    default:
                        if (!string.IsNullOrWhiteSpace(headerName))
                        {
                            instance.Fields.Add(headerName, currentRecord[i]);
                        }
                        break;
                }
            }

            return true;
        }
    }
}