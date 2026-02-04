using NSubstitute;
using Vue.Common.BrandVueApi;
using Vue.Common.BrandVueApi.Models;
namespace Test.Vue.Common.BrandVueApi;

    public class BrandVueApiClientTests
    {
        [Test]
        public async Task GetProjectQuestionsAvailableAsync_ReturnsCorrectUnionAndIntersection()
        {
            // Arrange
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var client = new TestableBrandVueApiClient("apiKey", "https://api/", httpClientFactory);

            // Setup test subsets
            client.SubsetsToReturn =
            [
                new SurveySet { Name = "Subset1", SurveySetId = "1" },
                new SurveySet { Name = "Subset2", SurveySetId = "2" }
            ];

            // Setup test questions
            var q1 = new QuestionWithSurveySets { QuestionId = "1", QuestionText = "Q1" };
            var q2 = new QuestionWithSurveySets { QuestionId = "2", QuestionText = "Q2" };
            var q3 = new QuestionWithSurveySets { QuestionId = "3", QuestionText = "Q3" };

            client.QuestionsBySubset = new Dictionary<string, List<QuestionWithSurveySets>>
            {
                { "1", [q1, q2] },
                { "2", [q2, q3] }
            };

            // Act
            var result = await client.GetProjectQuestionsAvailableAsync("co", "prod", "sub", CancellationToken.None);

            // Assert
            // Union: q1, q2, q3
            Assert.That(result.UnionOfQuestions.Select(q => q.QuestionId), Is.EquivalentTo(new[] { "1", "2", "3" }));
            Assert.That(result.UnionOfQuestions.Single(q => q.QuestionId == "1").SurveySets.Select(s=>s.SurveySetId), Is.EquivalentTo(new[] { "1" }));
            Assert.That(result.UnionOfQuestions.Single(q => q.QuestionId == "2").SurveySets.Select(s => s.SurveySetId), Is.EquivalentTo(new[] { "1","2" }));
            Assert.That(result.UnionOfQuestions.Single(q => q.QuestionId == "3").SurveySets.Select(s => s.SurveySetId), Is.EquivalentTo(new[] { "2" }));
        }

        // Testable subclass to override data fetching
        private class TestableBrandVueApiClient : BrandVueApiClient
        {
            public List<SurveySet> SubsetsToReturn { get; set; }
            public Dictionary<string, List<QuestionWithSurveySets>> QuestionsBySubset { get; set; }

            public TestableBrandVueApiClient(string apiKey, string apiBaseUrl, IHttpClientFactory httpClientFactory)
                : base(apiKey, apiBaseUrl, httpClientFactory) { }

            internal override Task<List<SurveySet>> GetSubsets(string companyShortCode, string productShortCode, string subProductId, CancellationToken cancellationToken)
                => Task.FromResult(SubsetsToReturn);

            protected override Task<T> MakeRequestAsync<T>(string apiUrl, CancellationToken cancellationToken)
            {
                var requestPath = apiUrl.Split('/').Last().ToLower();
                var indexOfQuestionMark = requestPath.IndexOf('?');
                var requestType = (indexOfQuestionMark >= 0) ? requestPath.Substring(0, indexOfQuestionMark) : requestPath;
                switch (requestType)
                {
                    case "questions":
                        var subsetId = apiUrl.Split('/').AsEnumerable().Reverse().Skip(1).First();
                        var questions = QuestionsBySubset[subsetId];
                        if (questions is T result)
                        {
                            return Task.FromResult(result);
                        }
                        throw new InvalidOperationException("Mismatch between endpoint and generic type");
            }
            throw new Exception($"Not supported request {apiUrl}");

            }
        }
    }
