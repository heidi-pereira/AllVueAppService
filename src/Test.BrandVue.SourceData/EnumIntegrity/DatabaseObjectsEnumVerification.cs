using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.MetaData;
using NUnit.Framework;
using JetBrains.Annotations;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using TestCommon;
using VerifyNUnit;
using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace Test.BrandVue.SourceData.EnumIntegrity
{
    /// <summary>
    /// This test verifies all enums used by any entity in <see cref="MetaDataContext"/> and <see cref="AnswersDbContext"/>.
    /// It is required because in a lot of our models we are mixing database and application code in a way that makes enumerations similar to reference tables.
    /// This is incredibly fragile and could cause some horrible bugs especially when an enumerations values are implicit.
    /// Reorders or insertions of implicit enumerations would cause breakages and pass through the build/test process unnoticed
    /// A better long term solution is to create the required reference tables with foreign keys and seed them in a migration. Data layer could then translate back to enumerations if required.
    /// </summary>
    [TestFixture]
    internal class DatabaseObjectsEnumVerification
    {
        [UsedImplicitly]
        private record EnumValue(object Value, string ValueString);
        [UsedImplicitly]
        private record EnumItem(string EnumName, IEnumerable<EnumValue> Values);

        private readonly List<Type> ExcludedEnums = new()
        {
            typeof(FeatureCode)
        };

        [Test]
        public async Task VerifyAllEnumerationsUsedInDatabaseModels()
        {
            var metadataContextEntityTypes = ITestMetadataContextFactory.Create(StorageType.InMemory).CreateDbContext().Model.GetEntityTypes();
            var answersContextEntityTypes = new TestChoiceSetReaderFactory().CreateDbContext().Model.GetEntityTypes();
            var allTypes = metadataContextEntityTypes.Concat(answersContextEntityTypes);
            var distinctEnumItems = allTypes
                .Select(t => t.ClrType)
                .SelectMany(m => m.GetProperties().Where(p => p.PropertyType.IsEnum && !ExcludedEnums.Contains(p.PropertyType)))
                .ToLookup(p => p.PropertyType.FullName, p => Enum.GetValues(p.PropertyType).Select(i => new EnumValue(i, i.ToString())))
                .Select(p => new EnumItem(p.Key, p.First()));

            await Verifier.VerifyJson(JsonConvert.SerializeObject(distinctEnumItems));
        }
    }
}
