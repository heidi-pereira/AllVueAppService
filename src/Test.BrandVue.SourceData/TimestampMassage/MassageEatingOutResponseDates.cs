using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BrandVue.SourceData;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData.TimestampMassage
{
    [TestFixture, Explicit("Should only be run if we actually have something to relocate")]
    public class MassageEatingOutResponseDates
    {
        [Test]
        public void MoveUKResponsesForErrantMonths()
        {
            var settings = TestLoaderSettings.EatingOut;
            var loader = TestDataLoader.Create(settings);
            loader.LoadBrandVueMetadataAndData();

            var respondents = loader.RespondentRepositorySource.GetForSubset(loader.SubsetRepository.Get("UK"));

            RemapMayResponses(respondents);
            RemapJuneResponses(respondents);
            RemapJulyResponses(respondents);
            RemapSeptemberResponses(respondents);
            RemapOctoberResponses(respondents);
        }

        private static void RemapMayResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/05/01"),
                DateTimeOffsetExtensions.ParseDate("2017/06/09"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/05/01"),
                DateTimeOffsetExtensions.ParseDate("2017/05/31"));

            RemapResponses(respondents, "May", sourceRange, targetRange);
        }

        private static void RemapJuneResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/06/10"),
                DateTimeOffsetExtensions.ParseDate("2017/07/07"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/06/01"),
                DateTimeOffsetExtensions.ParseDate("2017/06/30"));

            RemapResponses(respondents, "June", sourceRange, targetRange);
        }

        private static void RemapJulyResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/07/08"),
                DateTimeOffsetExtensions.ParseDate("2017/07/31"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/07/01"),
                DateTimeOffsetExtensions.ParseDate("2017/07/31"));

            RemapResponses(respondents, "July", sourceRange, targetRange);
        }

        private static void RemapSeptemberResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/09/01"),
                DateTimeOffsetExtensions.ParseDate("2017/10/02"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/09/01"),
                DateTimeOffsetExtensions.ParseDate("2017/09/30"));

            RemapResponses(respondents, "September", sourceRange, targetRange);
        }

        private static void RemapOctoberResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/10/03"),
                DateTimeOffsetExtensions.ParseDate("2017/10/31"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2017/10/01"),
                DateTimeOffsetExtensions.ParseDate("2017/10/31"));

            RemapResponses(respondents, "October", sourceRange, targetRange);
        }

        private static void RemapResponses(
            IRespondentRepository respondents,
            string monthName,
            DateRange sourceRange,
            DateRange targetRange)
        {
            var remapper = new RespondentDateRemapper(respondents);

            var remappings = remapper.RemapRespondentDates(sourceRange, targetRange);

            Console.WriteLine($"Remappings for {monthName}:");
            Console.WriteLine(remappings);
        }
    }
}
