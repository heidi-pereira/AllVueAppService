using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrandVue.PublicApi.Models;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    public class MetricAndClass
    {
        public MetricAndClass(ProductToTest productToTest, SurveysetDescriptor surveySet, MetricDescriptor metric,
            AverageDescriptor average,
            Dictionary<string, IReadOnlyCollection<ClassInstanceDescriptor>> classes)
        {
            ProductToTest = productToTest;
            SurveySet = surveySet;
            Metric = metric;
            Classes = classes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(i => i.ClassInstanceId).ToArray());
            Average = average;
        }


        public ProductToTest ProductToTest { get; set; }
        public SurveysetDescriptor SurveySet { get; set; }
        public MetricDescriptor Metric { get; set; }
        public Dictionary<string, int[]> Classes { get; set; }

        public AverageDescriptor Average { get; set; }

        public string RelativeUri => $"surveysets/{SurveySet.SurveysetId}/metrics/{Metric.MetricId}/{Average.AverageId}";

        public override string ToString() =>
            $"{ProductToTest}/{RelativeUri}/{string.Join(",", Classes.Select(c => c.Key + "(" + string.Join(",", c.Value) + ")"))}";
        public string AverageIdSlug()
        {
            return Average.AverageId.Substring(0, 2).ToUpperInvariant();
        }
        public string ToPath()
        {
            var metricName = Metric.Name.Replace(">", ".gt.").Replace("<", ".lt.").Replace(" ", "");
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string validMetricName = string.Join("_", metricName.Split(invalidChars.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

            var location = $"{ProductToTest}\\{SurveySet.SurveysetId}\\{AverageIdSlug()}\\{validMetricName}.{string.Join(",", Classes.Select(c => c.Key + "(" + string.Join(",", c.Value) + ")"))}";
            
            return location;
        }
    }
}