using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.PublicApi.Controllers;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Measures;
using Microsoft.AspNetCore.Mvc;
using AverageDescriptor = BrandVue.PublicApi.Models.AverageDescriptor;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    internal class TestDataProvider
    {
        //
        // Nb. the AllVue surveys are just the allVue versions of the brandvue survey! this is because this data lasts
        //a long time in the VueExport.
        //
        public static ProductToTest[] Products{ get; } = [
            new AllVueProductToTest("11816"),
            new AllVueProductToTest("11821"),
            new AllVueProductToTest("samsung-eu-campaigns"),
            new BrandVueProductToTest("barometer"),
            new BrandVueProductToTest("eatingout"),
            new BrandVueProductToTest("brandvue"),
            new BrandVueProductToTest("drinks"),
            new BrandVueProductToTest("finance"),
            new BrandVueProductToTest("retail"),
            new BrandVueProductToTest("wealth"),
            new BrandVueProductToTest("charities"),
            ];

        public static IEnumerable<MetricAndClass> MetricsForProducts(int maxMetricsToTest, int maxClassInstancesToTest = 1) =>
            Products.SelectMany(p =>
            {
                var localBrandVueServer = LocalBrandVueServer.For(p);
                return MetricsForProduct(localBrandVueServer, maxMetricsToTest, maxClassInstancesToTest);
            });

        public static IEnumerable<MetricAndClass> MetricsForProduct(LocalBrandVueServer server, int maxMetricsToTest = int.MaxValue, int maxClassInstancesToTest = 1)
        {
            var scope = server.LifetimeScope;
            var subsetController = scope.Resolve<SurveysetsApiController>();
            var surveySets = JsonResultToType<IEnumerable<SurveysetDescriptor>>(subsetController.GetSurveysets());
            var metricController = (MetricsApiController)scope.Resolve(typeof(MetricsApiController));

            var metrics = surveySets.SelectMany(s => JsonResultToType<IEnumerable<MetricDescriptor>>(metricController.GetMetrics(s)).Select(m => (surveySet: s, metric: m)));

            var selectedMetrics = metrics.OrderBy(m => m.metric.Name)
                .Where(metric => metric.metric.Measure.CalculationType != CalculationType.Text &&
                                !metric.metric.Measure.Disabled &&
                                metric.metric.Measure.EligibleForCrosstabOrAllVue)
                .Take(maxMetricsToTest);

            var classesRepository = (IClassDescriptorRepository)scope.Resolve(typeof(IClassDescriptorRepository));

            var classesController = scope.Resolve<ClassesApiController>();
            var averageDescriptorRepository = scope.Resolve<IAverageDescriptorRepository>();
            if (server.ProductToTest.IsAllVue)
            {
                if (averageDescriptorRepository.All(x => x.DisplayName.ToLower() != "monthlyweighted"))
                {
                    SimulateUnweightedMonthlyAverage(averageDescriptorRepository);
                }
            }
            var average = averageDescriptorRepository.Get("monthly", "test");
            if (!server.ProductToTest.IsAllVue)
            {
                if (average.WeightingMethod != WeightingMethod.QuotaCell)
                {
                    throw new Exception("Bad weighting");
                }
            }

            foreach (var metric in selectedMetrics)
            {
                var classInstances =
                    classesRepository.ValidClassDescriptors().Where(d => metric.metric.QuestionClasses.Contains(d.Name))
                        .ToDictionary(d => d.Name, d => GetSomeClassInstances(classesController, metric.surveySet, d, maxClassInstancesToTest));
                yield return new MetricAndClass(server.ProductToTest, metric.surveySet, metric.metric, new AverageDescriptor(average),
                    classInstances.OrderBy(c => c.Key).ToDictionary());
            }
        }

        public static IEnumerable<Class> ClassesForProducts()
        {
            return Products.SelectMany(p =>
            {
                var localBrandVueServer = LocalBrandVueServer.For(p);
                var subsetController = localBrandVueServer.LifetimeScope.Resolve<SurveysetsApiController>();
                var surveySets = JsonResultToType<IEnumerable<SurveysetDescriptor>>(subsetController.GetSurveysets());
                var surveySet = surveySets.SelectMany(
                    ss => ClassesForProduct(localBrandVueServer, ss)
                ); // limit to one surveyset per product for testing
                return surveySet;
            });
        }

        public static IEnumerable<Class> ClassesForProduct(LocalBrandVueServer server, SurveysetDescriptor surveyset) =>
            server.LifetimeScope.Resolve<IClassDescriptorRepository>().ValidClassDescriptors().Select(cd =>
            {
                return new Class(server.ProductToTest, surveyset, cd);
            });

        private static void SimulateUnweightedMonthlyAverage(IAverageDescriptorRepository averageDescriptorRepository)
        {
            DefaultAverageRepositoryData.AddDefaultAverages(averageDescriptorRepository as AverageDescriptorRepository, false, ["Monthly"], false);
            var average1 = averageDescriptorRepository.Get("monthly", "test");
            average1.WeightingMethod = WeightingMethod.None;
        }

        public static IReadOnlyCollection<ClassInstanceDescriptor> GetSomeClassInstances(ClassesApiController classesController, SurveysetDescriptor surveySet, ClassDescriptor d, int maxClassInstancesToTest)
        {
            var classInstances = JsonResultToType<IOrderedEnumerable<ClassInstanceDescriptor>>(classesController.GetClassInstances(surveySet, d));
            return classInstances.OrderBy(c => c.ClassInstanceId).Take(maxClassInstancesToTest)
                .ToArray();
        }

        private static T JsonResultToType<T>(IActionResult actionResult) where T : class
        {
            var jsonResult = actionResult as JsonResult;
            var apiResult = jsonResult?.Value as ApiResponse<T>;
            return apiResult?.Value;
        }

    }
}
