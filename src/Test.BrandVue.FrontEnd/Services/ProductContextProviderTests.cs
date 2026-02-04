using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using VerifyNUnit;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class ProductContextProviderTests
    {
        private TestChoiceSetReaderFactory _answerDbContextFactory;

        [OneTimeSetUp]
        public void Setup()
        {
            _answerDbContextFactory = new TestChoiceSetReaderFactory();
            using var dbContext = _answerDbContextFactory.CreateDbContext();
            
            var survey1 = CreateMockSurvey(11111, "MyVisibleSurvey");
            dbContext.Surveys.Add(survey1);
            
            var survey2 = CreateMockSurvey(22222, "MyHiddenSurvey", portalVisible: false, status: 2);
            dbContext.Surveys.Add(survey2);
            
            var samsungSurvey = CreateMockSurvey(13290, "Samsung EU Campaigns", authCompanyId: "Samsung");
            dbContext.Surveys.Add(samsungSurvey);
                        
            var surveyGroup = new SurveyGroup { Name = "My Group", SurveyGroupId = 1, Type = SurveyGroupType.AllVue, UrlSafeName = "my-group"};
            var surveyGroupSurveys = new List<SurveyGroupSurveys>
            {
                new() { SurveyGroup = surveyGroup, Survey = survey1, SurveyGroupId = surveyGroup.SurveyGroupId, SurveyId = survey1.SurveyId },
                new() { SurveyGroup = surveyGroup, Survey = survey2, SurveyGroupId = surveyGroup.SurveyGroupId, SurveyId = survey2.SurveyId },
            };
            surveyGroup.Surveys = surveyGroupSurveys;

            var samsungGroup = new SurveyGroup { Name = "Samsung EU Campaigns", SurveyGroupId = 2, Type = SurveyGroupType.AllVue, UrlSafeName = "samsung-eu-campaigns" };
            var samsungGroupSurveys = new List<SurveyGroupSurveys>
            {
                new() { SurveyGroup = samsungGroup, Survey = samsungSurvey, SurveyGroupId = samsungGroup.SurveyGroupId, SurveyId = samsungSurvey.SurveyId },
            };
            samsungGroup.Surveys = samsungGroupSurveys;

            var brandVueGroup = CreateProductSurveyGroup(3, "brandvue",
                [12682, 12805, 12976, 12686, 12808, 12993, 12685, 12807, 12981,
                        12691, 12812, 13003, 12687, 12809, 12994, 12689, 12810, 12998,
                        12684, 12806, 12979, 12681, 12803, 12973, 12690, 12811, 12999 ],
                dbContext, SurveyGroupType.BrandVue
            );

            var drinksGroup = CreateProductSurveyGroup(4, "drinks",
                [7396,7543,7687,7896,8115,8325,8466,8703,8934,9133,9389,9666,9914,
                10133,10510,10765,11146,11373,11598,11815,25513],
                dbContext, SurveyGroupType.BrandVue
            );

            dbContext.SurveyGroups.Add(surveyGroup);
            dbContext.SurveyGroups.Add(samsungGroup);
            dbContext.SurveyGroups.Add(brandVueGroup);
            dbContext.SurveyGroups.Add(drinksGroup);
            dbContext.SaveChanges();
        }

        [TestCase("survey", "11111", TestName = "Test_VisibleSurveyNumericSubProductId_ShouldReturnExpectedSettings")]
        [TestCase("survey", "non-existent-group", TestName = "Test_NonExistentSurveyGroupSubProductId_ShouldReturnExpectedSettings")]
        [TestCase("survey", "my-group", TestName = "Test_SurveyGroupSubProductId_ShouldReturnExpectedSettings")]
        [TestCase("survey", "samsung-eu-campaigns", TestName = "Test_SamsungSubProductId_ShouldReturnExpectedSettings")]
        [TestCase("brandvue", null, TestName = "Test_BrandVue360Product_ShouldReturnExpectedSettings")]
        [TestCase("drinks", null, TestName = "Test_DrinksProduct_ShouldReturnExpectedSettings")]
        public async Task VerifyProductContext(string productName, string subProductId)
        {
            var appSettings = new AppSettings();
            var productContextProvider = new ProductContextProvider(appSettings, _answerDbContextFactory, Substitute.For<ILogger<ProductContextProvider>>());
            var productContext = productContextProvider.ProvideProductContext(productName, subProductId);
            await Verifier.Verify(productContext);
        }

        private SurveyGroup CreateProductSurveyGroup(int groupId, string urlSafeName, int[] surveyIds, AnswersDbContext dbContext, SurveyGroupType groupType)
        {
            var group = new SurveyGroup { Name = urlSafeName, SurveyGroupId = groupId, Type = groupType, UrlSafeName = urlSafeName };
            var groupSurveys = new List<SurveyGroupSurveys>();

            foreach (var surveyId in surveyIds)
            {
                var survey = CreateMockSurvey(surveyId, urlSafeName, portalVisible: true, authCompanyId: "Savanta");
                groupSurveys.Add(new SurveyGroupSurveys
                {
                    SurveyGroup = group,
                    Survey = survey,
                    SurveyGroupId = group.SurveyGroupId,
                    SurveyId = surveyId,
                });
            }

            group.Surveys = groupSurveys;
            return group;
        }

        private Surveys CreateMockSurvey(int surveyId, string name, bool portalVisible = true, string authCompanyId = "Savanta", int status = 1)
        {
            return new Surveys
            {
                SurveyId = surveyId,
                Name = name,
                PortalName = name,
                PortalVisible = portalVisible,
                AuthCompanyId = authCompanyId,
                UniqueSurveyId = $"#{surveyId}",
                Status = status,
            };
        }
    }
}
