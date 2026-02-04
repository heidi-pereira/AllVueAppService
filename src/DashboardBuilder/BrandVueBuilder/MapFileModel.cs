using System;
using System.Collections.Generic;
using System.Linq;
using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace BrandVueBuilder
{
    internal class MapFileModel : IMapFileModel
    {
        private const string EntityIdentifier = "Entity";
        private const string LookupIdentifier = "Lookup";
        private const StringComparison StringComparison = System.StringComparison.InvariantCultureIgnoreCase;
        
        private readonly Workbook _mapFile;
        private IReadOnlyCollection<Entity> _entities;

        private IReadOnlyCollection<TextLookup> _lookups;

        public IReadOnlyCollection<Entity> Entities => _entities ??= GetEntities();

        public IReadOnlyCollection<Fields> FieldsForSubset(string subsetId)
        {
            return GetFields(subsetId);
        }
        public IReadOnlyCollection<TextLookup> Lookups => _lookups ??= GetLookups();

        public MapFileModel(Workbook mapFile)
        {
            _mapFile = mapFile;
        }

        private IReadOnlyCollection<Entity> GetEntities()
        {
            var entityTypes = GetSheetNames(EntityIdentifier);
            return entityTypes.Select(e => ConstructEntity(_mapFile, e)).ToArray();

            Entity ConstructEntity(Workbook workbook, string entityType)
            {
                var instances = new TypedWorksheet<EntityInstance>(workbook, true, entityType + EntityIdentifier).Rows;
                return new Entity(entityType, instances);
            }
        }

        private IReadOnlyCollection<Fields> GetFields(string subsetId)
        {
            return new TypedWorksheet<Fields>(_mapFile, true).Rows.Where(x=>x.IsIncludedForSubset(subsetId)).ToList();
        }

        private IReadOnlyCollection<TextLookup> GetLookups()
        {
            var lookupNames = GetSheetNames(LookupIdentifier);
            return lookupNames.Select(ConstructLookup).ToArray();

            TextLookup ConstructLookup(string lookupName)
            {
                var lookupData = new TypedWorksheet<TextLookupData>(_mapFile, true, lookupName + LookupIdentifier).Rows;
                return new TextLookup(lookupName, lookupData);
            }
        }

        private IEnumerable<string> GetSheetNames(string sheetSuffix)
        {
            return _mapFile.Worksheets
                .Where(w => w.Name.EndsWith(sheetSuffix, StringComparison))
                .Select(w => w.Name.Substring(0, w.Name.IndexOf(sheetSuffix, StringComparison)));
        }
    }
}