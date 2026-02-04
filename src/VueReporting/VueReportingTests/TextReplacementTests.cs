using System;
using NUnit.Framework;
using VueReporting.Models;
using VueReporting.Services;

namespace VueReportingTests
{
    internal static class EntitySetExtensions
    {
        internal static EntitySet WithMainInstance(this EntitySet entitySet, int mainInstanceId, string mainInstanceName)
        {
            entitySet.MainInstanceId = mainInstanceId;
            entitySet.MainInstanceName = mainInstanceName;
            return entitySet;
        }
    }

    [TestFixture]
    internal class TextReplacementTests
    {
        private readonly EntitySet _entitySet = new()
        {
            InstanceIds = new long[] {0, 1, 2, 3},
        };

        [TestCase("The #BrandName# is this #Date#", 2018, 5, 1, "The Blah is this May 2018")]
        [TestCase("The #BrandName# is this #Quarter#", 2018, 12, 31, "The Blah is this Q4 2018")]
        [TestCase("The #BrandName# is this #Quarter#", 2018, 1, 1, "The Blah is this Q1 2018")]
        [TestCase("#Quarter#", 2020, 10, 1, "Q4 2020")]
        [TestCase("#BrandName##Quarter##Date#", 2018, 5, 1, "BlahQ2 2018May 2018")]
        [TestCase("#BrandName###Quarter##Date#", 2018, 5, 1, "Blah#Q2 2018May 2018")]
        [TestCase("#BrandName###Quarter##Date1#", 2018, 5, 1, "Blah#Q2 2018#Date1#")]
        [TestCase("#wrong un#BrandName#", 2018, 5, 1, "#wrong unBlah")]
        [TestCase("#quarter#", 2020, 10, 1, "#quarter#")]
        [TestCase("#BrandName##wrong un", 2018, 5, 1, "Blah#wrong un")]
        public void Test_token_replacement(string source, int year, int month, int day, string expected)
        {
            var rpm = new ReportParameterManipulator(_entitySet.WithMainInstance(0, "Blah"), null, new TestAppSettings());

            rpm.ReplaceTokens(source, new DateTimeOffset(year, month, day, 1, 1, 1, TimeSpan.Zero), out var replaced);

            Assert.That(replaced, Is.EqualTo(expected));
        }

        [TestCase("The #BrandName# is this #Date#", 2018, 5, 1, "The Unknown Brand is this May 2018")]
        public void Test_token_replacement_for_non_existent_brand(string source, int year, int month, int day, string expected)
        {
            var rpm = new ReportParameterManipulator(_entitySet.WithMainInstance(99, null), null, new TestAppSettings());

            rpm.ReplaceTokens(source, new DateTimeOffset(year, month, day, 1, 1, 1, TimeSpan.Zero), out var replaced);

            Assert.That(replaced, Is.EqualTo(expected));
        }

        [Test]
        public void Test_token_replacement_for_text_with_newline_in_it()
        {
            var rpm = new ReportParameterManipulator(_entitySet.WithMainInstance(0, "BrandX"), null, new TestAppSettings());

            string source = $"String with #BrandName# on one line {Environment.NewLine} and some more text on another line";
            string expected = $"String with BrandX on one line {Environment.NewLine} and some more text on another line";

            rpm.ReplaceTokens(source, new DateTimeOffset(2020, 1, 1, 1, 1, 1, TimeSpan.Zero), out var replaced);

            Assert.That(replaced, Is.EqualTo(expected));
        }

        [TestCase("A title with no tokens")]
        [TestCase("A title with a mangled token : #BrandNme#")]
        [TestCase("#Some text with hashes either end#")]
        [TestCase("####Some text with BrandName# <p> odd xml </> tokens")]
        public void Test_token_replacement_where_no_matching_tokens_found(string sourceAndExpected)
        {
            var rpm = new ReportParameterManipulator(_entitySet.WithMainInstance(0, "Blah"), null, new TestAppSettings());

            rpm.UpdateUrl(new Uri("https://blah.vue-te.ch"), default, "", "");

            rpm.ReplaceTokens(sourceAndExpected, new DateTimeOffset(2020, 1, 1, 1, 1, 1, TimeSpan.Zero), out var replaced);

            Assert.That(replaced, Is.EqualTo(sourceAndExpected));
        }

        [TestCase("azzuri.beta-vue-te.ch", "https://demo.vue-te.ch/", "azzuri.vue-te.ch")]
        [TestCase("azzuri.beta-vue-te.ch", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.vue-te.ch", "https://demo.vue-te.ch", "azzuri.vue-te.ch")]
        [TestCase("azzuri.vue-te.ch", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.vue-te.ch", "https://demo.test.all-vue.com", "azzuri.test.all-vue.com")]
        [TestCase("azzuri.beta.all-vue.com", "https://demo.vue-te.ch", "azzuri.vue-te.ch")]
        public void Test_that_host_adjusted_to_correct_environment(string host, string root, string expected)
        {
            Assert.That(BrandVueService.AdjustHostForCurrentEnvironment(host, root), Is.EqualTo(expected));
        }

        [TestCase("azzuri.beta-vue-te.ch/test", "https://demo.all-vue.com/different", "azzuri.all-vue.com/test")]
        [TestCase("azzuri.beta-vue-te.ch", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.beta.vue-te.ch/test", "https://demo.all-vue.com/different", "azzuri.all-vue.com/test")]
        [TestCase("azzuri.beta.vue-te.ch", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.vue-te.ch", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.vue-te.ch/test", "https://demo.all-vue.com", "azzuri.all-vue.com/test")]
        [TestCase("azzuri.beta.all-vue.com/test", "https://demo.all-vue.com/different", "azzuri.all-vue.com/test")]
        [TestCase("azzuri.beta.all-vue.com", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.beta.all-vue.com/test", "https://demo.all-vue.com/different", "azzuri.all-vue.com/test")]
        [TestCase("azzuri.beta.all-vue.com", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.all-vue.com", "https://demo.all-vue.com", "azzuri.all-vue.com")]
        [TestCase("azzuri.all-vue.com/test", "https://demo.all-vue.com", "azzuri.all-vue.com/test")]
        public void Test_that_host_adjusted_to_allvue_correct_environment(string host, string root, string expected)
        {
            Assert.That(BrandVueService.AdjustHostForAllVueCurrentEnvironment(host, root), Is.EqualTo(expected));
        }
    }
}
