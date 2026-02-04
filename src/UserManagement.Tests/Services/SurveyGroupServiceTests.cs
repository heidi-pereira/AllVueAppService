using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using UserManagement.BackEnd.Services;

namespace UserManagement.Tests.Services;

[TestFixture]
public class SurveyGroupServiceTests
{
    [Test]
    public void GetLookupOfSurveyGroupIdToSafeUrl_IsThreadSafe()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnswersDbContext>()
            .UseInMemoryDatabase(databaseName: "SurveyGroupTestDb")
            .Options;

        // Seed the in-memory database
        using (var context = new AnswersDbContext(options))
        {
            context.SurveyGroups.AddRange(
                new SurveyGroup { SurveyGroupId = 1, UrlSafeName = "group1", Type = SurveyGroupType.AllVue },
                new SurveyGroup { SurveyGroupId = 2, UrlSafeName = "group2", Type = SurveyGroupType.BrandVue }
            );
            context.SaveChanges();
        }

        IDictionary<int, string>[] results;
        // Act
        using (var context = new AnswersDbContext(options))
        {
            var service = new SurveyGroupService(context);

            const int threadCount = 100;
            results = new IDictionary<int, string>[threadCount];
            Parallel.For(0, threadCount, i =>
            {
                results[i] = service.GetLookupOfSurveyGroupIdToSafeUrl();
            });
        }

        // Assert
        var first = results[0];
        foreach (var r in results)
        {
            Assert.That(first, Is.EquivalentTo(r));
        }
        Assert.That(2, Is.EqualTo(first.Count));
        Assert.That("group1", Is.EqualTo(first[1]));
        Assert.That("group2", Is.EqualTo(first[2]));
    }
}