using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using BrandVue.EntityFramework;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using Newtonsoft.Json;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.DataWarehouseTests;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.DatabaseCalculationTests
{
    [Explicit]
    [TestFixture]
    public class DatabaseCalculationTests
    {
        [Test]
        public async Task CalculationEngineVsDatabaseCalculationAsync()
        {
            TestContext.AddFormatter<CategoryResult>(c => JsonConvert.SerializeObject(c, Formatting.Indented));
            var localBrandVueServer = LocalBrandVueServer.For(new BrandVueProductToTest("retail"));
            var lifetimeScope = localBrandVueServer.LifetimeScope;
            var subset = lifetimeScope.Resolve<ISubsetRepository>().First();
            var dataLoader = lifetimeScope.Resolve<IBrandVueDataLoader>();
            dataLoader.LoadBrandVueMetadataAndData();
            var measureRepository = lifetimeScope.Resolve<IMeasureRepository>();
            var suitableMeasures = measureRepository
                .GetAllMeasuresWithDisabledPropertyFalseForSubset(subset)
                .Where(m => PipelineResultsProvider.SuitableForDatabaseAssistance(m, subset.Id))
                .ToArray();

            if (suitableMeasures.Any())
            {
                var measure = suitableMeasures.First();
                var requestDate = new DateTimeOffset(new DateTime(2021, 04, 30)).ToDateInstance();
                var calculationPeriodSpan = new CalculationPeriodSpan { StartDate = requestDate, EndDate = requestDate }.Yield().ToArray();
                var calculationPeriod = new Period { Average = "MonthlyOver3Months", ComparisonDates = calculationPeriodSpan};
                var defaultEntitySet = lifetimeScope.Resolve<IEntitySetRepository>()
                    .GetDefaultSetForOrganisation(EntityType.Brand, subset, "savanta");
                var multipleInstanceRequest = new EntityInstanceRequest(EntityType.Brand, defaultEntitySet.Instances.Select(i => i.Id).ToArray());
                string[] measureNames = measure.Name.Yield().ToArray();
                var databaseRequestModel = new MultiEntityProfileModel(subset.Id, calculationPeriod,
                    multipleInstanceRequest, defaultEntitySet.MainInstance.Id, measureNames, Array.Empty<int>(), true);
                var databaseResults = lifetimeScope.Resolve<IResultsProvider>()
                    .GetProfileResultsForMultipleEntities(databaseRequestModel, CancellationToken.None);

                //Switch off database calculations
                lifetimeScope.Resolve<AppSettings>().UseDatabaseAssistedCalculationsForAudiences = false;

                //This is not strictly necessary to re-resolve this as AppSettings is single instance, however I think the intention is clearer here.
                var engineResults = lifetimeScope.Resolve<IResultsProvider>()
                    .GetProfileResultsForMultipleEntities(databaseRequestModel, CancellationToken.None);

                Assert.That(databaseResults, Is.EquivalentTo(await engineResults).Using(new CategoryResultComparer()));
            }
            else
            {
                Assert.Fail("No suitable measures found to compare database vs engine calculation. If running locally you maybe missing metric configurations for this product in your local database");
            }
        }

        private class CategoryResultComparer : IEqualityComparer<CategoryResult>
        {
            private const double AcceptableError = 0.0000001;

            public bool Equals(CategoryResult x, CategoryResult y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.MeasureName == y.MeasureName && x.EntityInstanceName == y.EntityInstanceName &&
                       Math.Abs(x.Result - y.Result) < AcceptableError &&
                       Math.Abs(x.AverageValue ?? 0 - y.AverageValue ?? 0) < AcceptableError;
            }

            public int GetHashCode(CategoryResult obj)
            {
                return HashCode.Combine(obj.MeasureName, obj.EntityInstanceName, obj.Result, obj.AverageValue);
            }
        }
    }


}
