using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    public class ConfigureMetricController : ApiController
    {
        private IConfigureMetricService _metricService;
        private readonly IBrandVueDataLoader _dataLoader;

        public ConfigureMetricController(IConfigureMetricService metricService, IBrandVueDataLoader dataLoader)
        {
            _metricService = metricService;
            _dataLoader = dataLoader;
        }

        [HttpPut]
        [Route("configureMetric/updateMetricDisabled")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void UpdateMetricDisabled(string metricName, bool disableMeasure)
        {
            _metricService.UpdateMetricDisabled(metricName, disableMeasure);
        }
        [HttpPut]
        [Route("configureMetric/updateMetricFilterDisabled")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void UpdateMetricFilterDisabled(string metricName, bool disableFilterForMeasure)
        {
            _metricService.UpdateMetricFilterDisabled(metricName, disableFilterForMeasure);
        }

        [HttpPut]
        [Route("configureMetric/updateEligibleForCrosstabOrAllVue")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void UpdateEligibleForCrosstabOrAllVue(string metricName, bool updatedValue)
        {
            _metricService.UpdateEligibleForCrosstabOrAllVue(metricName, updatedValue);
        }

        [HttpPut]
        [Route("configureMetric/updateMetricDefaultSplitBy")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void UpdateMetricDefaultSplitBy(string metricName, string entityTypeName)
        {
            _metricService.UpdateMetricDefaultSplitBy(metricName, entityTypeName);
        }

        [HttpPut]
        [InvalidateBrowserCache]
        [Route("configureMetric/UpdateMetricModalData")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void UpdateMetricModalData([FromBody] MetricModalDataModel model)
        {
            _metricService.UpdateMetricModalData(model);
        }

        [HttpPut]
        [InvalidateBrowserCache]
        [Route("configureMetric/convertCalculationType")]
        [SubsetAuthorisation(nameof(subsetId))]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void ConvertCalculationType(string metricName, CalculationType convertToCalculationType, string subsetId)
        {
            _metricService.ConvertCalculationType(metricName, convertToCalculationType, subsetId);
        }
        
        [HttpPut]
        [InvalidateBrowserCache]
        [Route("configureMetric/updateBaseVariable")]
        [Authorize(Policy = FeatureRolePolicy.VariablesEdit)]
        public void UpdateBaseVariable(string metricName, int? baseVariableId)
        {
            _metricService.UpdateBaseVariable(metricName, baseVariableId);
        }
    }
}
