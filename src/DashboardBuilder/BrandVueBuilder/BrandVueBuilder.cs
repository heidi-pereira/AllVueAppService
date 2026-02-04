using Microsoft.Extensions.Logging;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace BrandVueBuilder
{
    public class BrandVueBuilder
    {
        private readonly BrandVueBuilderAppSettings _appSettings;
        private readonly IBrandVueProductSettings _brandVueProductSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly TableDefinitionFactory _tableDefinitionFactory;

        public BrandVueBuilder(BrandVueBuilderAppSettings appSettings, IBrandVueProductSettings brandVueProductSettings, ILoggerFactory loggerFactory)
        {
            _appSettings = appSettings;
            _brandVueProductSettings = brandVueProductSettings;
            _loggerFactory = loggerFactory;
            _tableDefinitionFactory = new TableDefinitionFactory(_loggerFactory, new MapFileModel(brandVueProductSettings.Map), appSettings.MetaAppSettings.RequiresV2CompatibleFieldModel);
        }

        public void Build()
        {
            var metaDataBuilder = new MetaDataBuilder(_appSettings.MetaAppSettings, _brandVueProductSettings, _loggerFactory, _tableDefinitionFactory, new TypedWorksheet<FieldCategories>(_brandVueProductSettings.Map).Rows.ToArray());
            metaDataBuilder.Build();
        }
    }
}