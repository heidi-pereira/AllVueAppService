using BrandVue.SourceData;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData.TimestampMassage
{
    [TestFixture, Explicit("Should only be run if we actually have something to relocate")]
    public class MassageUkMenswearResponseDates
    {
        [Test]
        public void MoveUKResponsesForErrantMonths()
        {
            var settings = TestLoaderSettings.Default;
            var loader = TestDataLoader.Create(settings);
            loader.LoadBrandVueMetadataAndData();

            var respondents = loader.RespondentRepositorySource.GetForSubset(loader.SubsetRepository.Get("UKMen"));

            RemapFebruaryResponses(respondents);
            RemapMarchResponses(respondents);
            RemapAprilResponses(respondents);
        }

        [Test]
        public void MoveUKResponsesForAprilPartDeux()
        {
            var settings = TestLoaderSettings.Default;
            var loader = TestDataLoader.Create(settings);
            loader.LoadBrandVueMetadataAndData();

            var respondents = loader.RespondentRepositorySource.GetForSubset(loader.SubsetRepository.Get("UKMen"));

            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/04/02"),
                DateTimeOffsetExtensions.ParseDate("2018/04/30"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/04/01"),
                DateTimeOffsetExtensions.ParseDate("2018/04/30"));

            RemapResponses(respondents, "April", sourceRange, targetRange);
        }

        private static void RemapFebruaryResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/02/01"),
                DateTimeOffsetExtensions.ParseDate("2018/03/01"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/02/01"),
                DateTimeOffsetExtensions.ParseDate("2018/02/28"));

            RemapResponses(respondents, "February", sourceRange, targetRange);
        }

        private static void RemapMarchResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/03/02"),
                DateTimeOffsetExtensions.ParseDate("2018/04/01"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/03/01"),
                DateTimeOffsetExtensions.ParseDate("2018/03/31"));

            RemapResponses(respondents, "March", sourceRange, targetRange);
        }

        private static void RemapAprilResponses(IRespondentRepository respondents)
        {
            var sourceRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/04/02"),
                DateTimeOffsetExtensions.ParseDate("2018/04/10"));

            var targetRange = new DateRange(
                DateTimeOffsetExtensions.ParseDate("2018/04/01"),
                DateTimeOffsetExtensions.ParseDate("2018/04/10"));

            RemapResponses(respondents, "April", sourceRange, targetRange);
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
