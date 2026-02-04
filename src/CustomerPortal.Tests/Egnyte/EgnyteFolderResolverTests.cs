using CustomerPortal.Models;
using CustomerPortal.Services;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using AuthServer.GeneratedAuthApi;

namespace CustomerPortal.Tests.Egnyte
{
    [TestFixture]
    public class EgnyteFolderResolverTests
    {
        [Test]
        public async Task Test_survey_folder_path_uses_internal_survey_name()
        {
            var egnyteFolderResolver = GetEgnyteFolderResolver();

            Assert.That(await egnyteFolderResolver.GetSurveyFolderPath(1),
                Is.EqualTo("/Shared/Savanta/Service Assets/Customer Portal/PortalGroup/InternalName (1)"));
        }

        [Test]
        public async Task Test_survey_client_folder_path_uses_internal_survey_name()
        {
            var egnyteFolderResolver = GetEgnyteFolderResolver();

            Assert.That(await egnyteFolderResolver.GetSurveyClientFolderPath(1),
                Is.EqualTo("/Shared/Savanta/Service Assets/Customer Portal/PortalGroup/InternalName (1)/Client"));
        }

        private static EgnyteFolderResolver GetEgnyteFolderResolver()
        {
            var survey = new Survey
            {
                Id = 1,
                Name = "DisplayName",
                InternalName = "InternalName",
                AuthCompanyId = "AuthCompanyId"
            };
            var requestContext = Substitute.For<IRequestContext>();
            string portalgroup = "PortalGroup";
            requestContext.PortalGroup.Returns(portalgroup);

            var surveyService = Substitute.For<ISurveyService>();
            surveyService.SurveyForEgnytePathUnrestricted(0).ReturnsForAnyArgs(survey);
            surveyService.GetCompanyForSurvey(null).ReturnsForAnyArgs(new CompanyModel() { ShortCode = portalgroup });

            var appSettings = new AppSettings
            {
                EgnyteRootFolder = "/Shared/Savanta/Service Assets/Customer Portal/"
            };
            return new EgnyteFolderResolver(surveyService, appSettings);
        }
    }
}
