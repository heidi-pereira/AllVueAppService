using System;

namespace CustomerPortal.Shared.Egnyte
{
    public class SurveyDocumentsRequestContext
    {
        public string FolderPath { get; }
        public string InsecureDownloadDomain { get; }
        public string PathBase { get; }
        public Guid SurveyDownloadGuid { get; }
        public Uri SurveyDownloadUri { get; }
        public SurveyDocumentsRequestContext(string folderPath, string insecureDownloadDomain, Uri surveyDownloadUri, string pathBase, Guid surveyDownloadGuid)
        {
            FolderPath = folderPath;
            InsecureDownloadDomain = insecureDownloadDomain;
            PathBase = pathBase;
            SurveyDownloadGuid = surveyDownloadGuid;
            SurveyDownloadUri = surveyDownloadUri;
        }
    }

    public enum DocumentOwnedBy
    {
        Client,
        Savanta
    }

    public delegate string DocumentUrlProviderSignature(string fileName, DocumentOwnedBy ownedBy, SurveyDocumentsRequestContext requestContext);

    public interface IDocumentUrlProvider
    {
        DocumentUrlProviderSignature DocumentUrlProvider(int surveyId);
    }
}