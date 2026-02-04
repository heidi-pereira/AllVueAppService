using System;
using BrandVue.SourceData;
using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Vue.Common.Auth;

namespace Test.BrandVue.SourceData
{
    public class BaseLoaderTests
    {
        [Test]
        public void ConfigurationErrorsThrownDoNotEscapeLoader()
        {
            var substituteLogger = Substitute.For<ILogger<BaseLoaderConfigurationErrorSimulator>>();
            Assert.That(
                () =>
                {
                    new BaseLoaderConfigurationErrorSimulator(
                (Measure targetThing, string[] currentRecord, string[] headers) =>
                    throw new Exception("Test exception representing configuration errors."), substituteLogger).ProcessOneRecord();
                }
                , Throws.Nothing);
        }

        public class BaseLoaderConfigurationErrorSimulator : BaseLoader<Measure, string>
        {
            private Func<Measure, string[], string[], bool> ProcessLoadedRecordAction;
            public BaseLoaderConfigurationErrorSimulator(Func<Measure, string[], string[], bool> processLoadedRecordAction, ILogger logger) : base(new MetricRepository(Substitute.For<IUserDataPermissionsOrchestrator>()), typeof(MapFileMetricLoader), logger)
            {
                ProcessLoadedRecordAction = processLoadedRecordAction;
            }

            public void ProcessOneRecord()
            {
                CreateAndStoreObjectForCsvDataRow("", 0, new[] { "" }, new[] { "" }, 0);
            }

            protected override string IdentityPropertyName { get; }
            protected override string GetIdentity(string[] currentRecord, int identityFieldIndex)
            {
                return "Identity";
            }

            protected override bool ProcessLoadedRecordFor(Measure targetThing, string[] currentRecord, string[] headers)
            {
                return ProcessLoadedRecordAction(targetThing, currentRecord, headers);
            }
        }
    }
}