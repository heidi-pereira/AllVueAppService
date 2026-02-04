using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.GeneratedAuthApi;
using CustomerPortal.Models;
using CustomerPortal.Services;
using CustomerPortal.Shared.Egnyte;
using NSubstitute;
using NUnit.Framework;

namespace CustomerPortal.Tests.Egnyte
{
    [TestFixture, Ignore("Very flaky, especially with rate limiting")]
    public class DocumentServiceTests: IDocumentUrlProvider
    {
        private DocumentService _documentService;
        private int _SurveyId = 1;
        private Guid _SurveyDownloadGuid = Guid.NewGuid();

        string GenerateUrl(string fileName, DocumentOwnedBy ownedBy, SurveyDocumentsRequestContext details)
        {
            return fileName;
        }
        public DocumentUrlProviderSignature DocumentUrlProvider(int surveyId)
        {
            return GenerateUrl;
        }
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var appSettings = new AppSettings
            {
                EgnyteDomain = "savantatest",
                EgnyteClientId = "rtuyjyt7queuc3ybefsgawd9",
                EgnyteUsername = "integration.tech.team",
                EgnytePassword = "b$fDErH75MR8[\"V/(@/s(-",
                EgnyteRootFolder = "/Shared/Savanta/Service Assets/Customer Portal/SavantaUnitTesting/"
            };

            var survey = new Survey
            {
                Id = _SurveyId,
                Name = "SurveyName",
                InternalName = nameof(DocumentServiceTests),
                AuthCompanyId = "AuthCompanyId",
                FileDownloadGuid = _SurveyDownloadGuid,
            };

            var requestContext = Substitute.For<IRequestContext>();
            string portalgroup = "PortalGroup";
            requestContext.PortalGroup.Returns(portalgroup);

            var surveyService = Substitute.For<ISurveyService>();
            surveyService.SurveyForEgnytePathUnrestricted(0).ReturnsForAnyArgs(survey);
            surveyService.SurveyForEgnytePathUnrestricted(_SurveyDownloadGuid, true).ReturnsForAnyArgs(survey);
            surveyService.GetCompanyForSurvey(null).ReturnsForAnyArgs(new CompanyModel() {ShortCode = portalgroup });


            var egnyteService = new EgnyteService(appSettings.EgnyteDomain, appSettings.EgnyteClientId, appSettings.EgnyteUsername, appSettings.EgnytePassword, appSettings.EgnyteAccessToken);
            var egnyteFolderResolver = new EgnyteFolderResolver(surveyService, appSettings);

            _documentService = new DocumentService(egnyteService, egnyteFolderResolver, this);
        }

        [Test]
        public async Task EndToEndClientDocumentTest()
        {
            var surveyId = _SurveyId;
            // Only want to test uploading/deleting documents if we know the urls are as expected
            // so make this an end-to-end test with multiple assertions throughout

            var folderUrl = await _documentService.GetFolderUrl(surveyId);
            var clientFolderLocation = await _documentService.GetClientFolderLocation(surveyId);

            Assert.Multiple(() =>
            {
                Assert.That(folderUrl.AbsoluteUri,
                    Is.EqualTo("https://savantatest.egnyte.com/app/index.do#storage/files/1/Shared/Savanta/Service%20Assets/Customer%20Portal/SavantaUnitTesting/PortalGroup/DocumentServiceTests%20(1)"));
                Assert.That(clientFolderLocation,
                    Is.EqualTo("/Shared/Savanta/Service Assets/Customer Portal/SavantaUnitTesting/PortalGroup/DocumentServiceTests (1)/Client"));
            });

            var documentPath = string.Empty;
            try
            {
                var documentName = Path.GetRandomFileName();
                const string documentContents = "I am the contents of the file";

                documentPath = Path.Combine(Path.GetTempPath(), documentName);
                File.WriteAllText(documentPath, documentContents);

                await using var documentStream = File.OpenRead(documentPath);
                await _documentService.ClientDocumentUpload(surveyId, documentName, documentContents.Length, documentStream);
                try
                {

                    var downloadGuid = _SurveyDownloadGuid;
                    var request = new SurveyDocumentsRequestContext(string.Empty, "localhost", new Uri("http://localhost"), string.Empty, downloadGuid);

                    var surveyDocuments = (await _documentService.SurveyDocuments(surveyId, "myCompany", request)).ToList();
                    var surveyDocument = surveyDocuments.FirstOrDefault(d => d.Name == documentName);

                    Assert.Multiple(() =>
                    {
                        Assert.That(surveyDocument, Is.Not.Null);
                        Assert.That(surveyDocument.Size, Is.EqualTo(documentContents.Length));
                        Assert.That(surveyDocument.IsClientDocument, Is.True);
                    });

                    // Prevent the API call being blocked or we'll receive an error message in the document download stream
                    Thread.Sleep(1000);

                    var buffer = new byte[documentContents.Length];
                    var downloadedDocumentByGuid = await _documentService.ClientDocumentDownload(downloadGuid, documentName, false);

                    await downloadedDocumentByGuid.ReadAsync(buffer);
                    var bufferStringByGuid = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    Assert.That(bufferStringByGuid, Is.EqualTo(documentContents));

                }
                finally
                {
                    var successfulDelete = await _documentService.ClientDocumentDelete(_SurveyId, documentName);
                    Assert.That(successfulDelete, Is.True);
                }
                var newRequest = new SurveyDocumentsRequestContext(string.Empty, "localhost", new Uri("http://localhost"), string.Empty, Guid.NewGuid());
                var finalSurveyDocuments = (await _documentService.SurveyDocuments(_SurveyId, "myCompany", newRequest)).ToList();
                var finalSurveyDocument = finalSurveyDocuments.FirstOrDefault(d => d.Name == documentName);
                Assert.That(finalSurveyDocument, Is.Null);
            }
            finally
            {
                if (File.Exists(documentPath))
                {
                    File.Delete(documentPath);
                }
            }
        }
    }
}
