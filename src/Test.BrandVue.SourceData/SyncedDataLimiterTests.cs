using System;
using System.Data;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.LazyLoading;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class SyncedDataLimiterTests
    {
        private ProductContext _allVue = new("survey", 1.ToString(), true, null);

        [Test]
        public void AllVue_WhenThingsStayTheSame_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentResponse, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, ()=> 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
        }

        [Test]
        public void AllVue_WhenResynching_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentResponse, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 1);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
        }

        [Test]
        public void AllVue_AfterResynching_DoesReload()
        {
            var responses = 27;
            var isReSynching = 1;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentResponse, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => isReSynching);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            isReSynching = 0;
            Assert.That(limiter.RequiresReload, Is.True);
        }

        [Test]
        public void AllVue_WhenOnlyLatestUsableDateChanges_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync.AddHours(-1);
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            mostRecentSync = mostRecentResponse.AddDays(1);
            Assert.That(limiter.RequiresReload, Is.False);
        }

        [Test]
        public void AllVue_WhenOnlyMostRecentResponseChanges_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01 12:00");
            var mostRecentResponse = mostRecentSync.AddHours(-1);
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            mostRecentResponse = mostRecentResponse.AddDays(1);
            Assert.That(limiter.RequiresReload, Is.False);
        }

        [Test]
        public void AllVue_WhenLatestUsableDateChanges_AndLatestResponseExists_Reloads()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01 12:00");
            var mostRecentResponse = mostRecentSync.AddHours(1);
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            mostRecentSync = mostRecentResponse.AddDays(1);
            Assert.That(limiter.RequiresReload, Is.True);
        }

        [Test]
        public void AllVue_WhenResponseCountChanges_Reloads()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            responses++;
            Assert.That(limiter.RequiresReload, Is.True);
        }


        [TestCase(1)]
        [TestCase(-1)]
        public void AllVue_WhenResponseArchivedCountChanged_Reloads(int deltaOfNumberOfArchived)
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            archivedResponses+=deltaOfNumberOfArchived;
            Assert.That(limiter.RequiresReload, Is.True);
        }

        [Test]
        public void BrandVue_WhenResponseCountChanges_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, new ProductContext("somebrandvue"));
            Assert.That(limiter.RequiresReload, Is.False);
            responses++;
            Assert.That(limiter.RequiresReload, Is.False);
        }

        [Test]
        public void AllVue_WithinTheRecheckInterval_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue, 60);
            Assert.That(limiter.RequiresReload, Is.False);
            responses++;
            mostRecentResponse = mostRecentResponse.AddMinutes(1);
            Assert.That(limiter.RequiresReload, Is.False);
        }

        [Test]
        public void AllVue_WhenMostRecentResponseGoesBackwards_Reloads()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentResponse, () => null, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            mostRecentResponse = mostRecentResponse.AddMinutes(-1);
            Assert.That(limiter.RequiresReload, Is.True);
        }

        [Test]
        public void AllVue_WhenDataAppended_Reloads()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mostRecentDataAppend = DateTime.Parse("2021-03-25");
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentSync, () => mostRecentDataAppend, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, _allVue);
            Assert.That(limiter.RequiresReload, Is.False);
            mostRecentDataAppend = DateTime.Parse("2021-04-01");
            Assert.That(limiter.RequiresReload, Is.True);
        }

        [Test]
        public void BrandVue_WhenDataAppended_DoesNotReload()
        {
            var responses = 27;
            var archivedResponses = 15;
            var mostRecentSync = DateTime.Parse("2021-04-01");
            var mostRecentResponse = mostRecentSync;
            var mostRecentDataAppend = DateTime.Parse("2021-03-25");
            var mockSqlProvider = MockSqlProvider(() => mostRecentSync, () => mostRecentSync, () => mostRecentDataAppend, () => responses, () => archivedResponses, () => 0);
            var limiter = RealSyncedDataLimiter(mockSqlProvider, new ProductContext("somebrandvue"));
            Assert.That(limiter.RequiresReload, Is.False);
            mostRecentDataAppend = DateTime.Parse("2021-04-01");
            Assert.That(limiter.RequiresReload, Is.False);
        }

        private SyncedDataLimiter RealSyncedDataLimiter(ISqlProvider mockSqlProvider, IProductContext productContext, int recheckIntervalSeconds = -1) =>
            new(mockSqlProvider, productContext, new[] {1}, null, Substitute.For<ILogger>()) {RecheckIntervalSeconds = recheckIntervalSeconds};

        private static ISqlProvider MockSqlProvider(Func<DateTime> latestUsableDate, Func<DateTime> lastResponse, Func<DateTime?> lastDataAppendTime, Func<int> responseCount, Func<int> archivedCount, Func<int> isResynching)
        {
            var sqlProvider = Substitute.For<ISqlProvider>();

            sqlProvider.WhenForAnyArgs(s => s.ExecuteReader(null, null, null)).Do(c =>
            {
                var sql = c.Arg<string>();
                var handleRow = c.Arg<Action<IDataRecord>>();

                var response = new (string SqlContains, IDataRecord Record)[]
                {
                    ("vue.syncstates", DataRecord(
                        (SqlDbType.DateTime2, latestUsableDate(), "Last Data Sync Time"),
                        (SqlDbType.DateTime2, lastResponse(), "Most Recent Response"),
                        (SqlDbType.DateTime2, lastDataAppendTime(), "Most Recent Data Append"),
                        (SqlDbType.Int, isResynching(), "Resynching")
                    )),
                    ("surveyResponse", DataRecord((SqlDbType.Int, responseCount(), "Response Count"))),
                    ("panelRespondents", DataRecord((SqlDbType.Int, archivedCount(), "Count"))),
                    ("dbo.SurveyGroups", DataRecord((SqlDbType.Int, 1, "SurveyId")))
                }.Single(r => sql.Contains(r.SqlContains, StringComparison.OrdinalIgnoreCase));

                handleRow(response.Record);
            });
            return sqlProvider;
        }

        private static SqlDataRecord DataRecord(params (SqlDbType sqlDbType, object value, string name)[] cols)
        {
            var mostRecentRecord = new SqlDataRecord(cols.Select(c => new SqlMetaData(c.name, c.sqlDbType)).ToArray());
            mostRecentRecord.SetValues(cols.Select(c => c.value).ToArray());
            return mostRecentRecord;
        }
    }
}
