using System;
using System.Collections.Generic;
using System.Linq;
using Aspose.Cells;
using BrandVueBuilder;
using DashboardBuilder.Core;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardBuilder
{
    internal class BrandVueProductSettings : IBrandVueProductSettings
    {
        private readonly IMapSettings _mapSettings;

        public Workbook Map => _mapSettings.Map;
        public string ShortCode => _mapSettings.ShortCode;
        public IEnumerable<string> SubsetIds { get; }

        public static BrandVueProductSettings FromMapSettings(IMapSettings mapSettings)
        {
            var subset = new TypedWorksheet<SubsetsIdOnly>(mapSettings.Map, false);
            var subsetIds = subset?.Rows?.Select(s => s.Id)?.ToArray();
            return new BrandVueProductSettings(mapSettings, subsetIds??new string[] {"All" });
        }
        private BrandVueProductSettings(IMapSettings mapSettings, IEnumerable<string> subsetIds)
        {
            _mapSettings = mapSettings;
            SubsetIds = subsetIds;
        }
    }
}