using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CustomerPortal.Shared.Models;
using Egnyte.Api.Common;
using Egnyte.Api.Files;

namespace CustomerPortal.Shared.Egnyte
{
    public class DocumentService
    {
        private readonly IEgnyteService _egnyteService;
        private readonly IEgnyteFolderResolver _egnyteFolderResolver;
        private readonly IDocumentUrlProvider _documentUrlProvider;

        public DocumentService(IEgnyteService egnyteService, IEgnyteFolderResolver egnyteFolderResolver, IDocumentUrlProvider urlProvider)
        {
            _egnyteService = egnyteService;
            _egnyteFolderResolver = egnyteFolderResolver;
            _documentUrlProvider = urlProvider;
        }

        public async Task<IEnumerable<SurveyDocument>> SurveyDocuments(
            int surveyId, 
            string clientName,
            SurveyDocumentsRequestContext surveyDocumentsRequestContext)
        {
            await GetClientFolderLocation(surveyId);
            var generateUrlFunc = _documentUrlProvider.DocumentUrlProvider(surveyId);
            var includeClientDocuments = string.IsNullOrEmpty(surveyDocumentsRequestContext.FolderPath);
            var folderPath = await _egnyteFolderResolver.GetSurveyFolderPath(surveyId);
            if (!includeClientDocuments)
            {
                folderPath = Path.Combine(folderPath, surveyDocumentsRequestContext.FolderPath);
            }
            var savantaFiles = await ListDocuments(folderPath);
            if (!includeClientDocuments)
            {
                return GetSurveyDocuments(savantaFiles, clientName,DocumentOwnedBy.Savanta, surveyDocumentsRequestContext, generateUrlFunc);
            }
            var clientFolderPath = await _egnyteFolderResolver.GetSurveyClientFolderPath(surveyId);
            var clientFiles = await ListDocuments(clientFolderPath);

            return GetSurveyDocuments(savantaFiles, clientName, DocumentOwnedBy.Savanta, surveyDocumentsRequestContext, generateUrlFunc)
                .Concat(GetSurveyDocuments(clientFiles, clientName, DocumentOwnedBy.Client, surveyDocumentsRequestContext, generateUrlFunc));
        }

        public async Task<string> PathToEgnite(int surveyId, string dataDownloadDomain)
        {
            var folderPath = await _egnyteFolderResolver.GetSurveyFolderPath(surveyId);
            var linkUrl = $"https://{_egnyteService.EgnyteDomain}.egnyte.com/app/index.do#storage/files/1{folderPath}";
            return linkUrl;
        }

        private async Task<FileOrFolderMetadata> ListDocuments(string folderPath)
        {
            var documentFolderPath = await GetDocumentFolderPath(folderPath);

            if (documentFolderPath == null)
            {
                return null;
            }

            FileOrFolderMetadata files;
            try
            {
                files = await _egnyteService.ExecuteEgnyteCall(client => client.Files.ListFileOrFolder(documentFolderPath));
            }
            catch (EgnyteApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                files = null;
            }
            return files;
        }


        private static IEnumerable<SurveyDocument> GetSurveyDocuments(
            FileOrFolderMetadata metadata,
            string clientName,
            DocumentOwnedBy documentOwnedBy,
            SurveyDocumentsRequestContext context, DocumentUrlProviderSignature generateUrlFunc)
        {
            if (metadata == null)
            {
                return Enumerable.Empty<SurveyDocument>();
            }

            return metadata
                .AsFolder
                .Folders
                .Where(folder=> folder.Name.ToLowerInvariant() != "client").Select(folder => new SurveyDocument
                {
                    Id = folder.FolderId,
                    Name = folder.Name,
                    LastModified = folder.LastModified,
                    Size = 0,
                    IsClientDocument = documentOwnedBy== DocumentOwnedBy.Client,
                    ClientName = clientName,
                    DownloadUrl = null,
                    IsFolder = true,
                })
                .OrderBy(d => d.Name).Concat(


                metadata
                .AsFolder
                .Files
                .Select(file => new SurveyDocument
                {
                    Id = file.EntryId,
                    Name = file.Name,
                    LastModified = file.LastModified,
                    Size = file.Size,
                    IsClientDocument = documentOwnedBy == DocumentOwnedBy.Client,
                    ClientName = clientName,
                    DownloadUrl = generateUrlFunc(file.Name, documentOwnedBy, context),
                    IsFolder = false,
                })
                .OrderBy(d => d.Name));
        }

        private async Task<string> GetDocumentFolderPath(string surveyFolderPath)
        {
            var surveyFolders = await _egnyteService.GetEgnyteFolder(surveyFolderPath);

            return surveyFolders?.Path;
        }

        public async Task<Uri> GetFolderUrl(int surveyId)
        {
            var surveyFolderPath = await _egnyteFolderResolver.GetSurveyFolderPath(surveyId);

            return await GetFolderUrl(surveyFolderPath);
        }

        public async Task<string> GetClientFolderLocation(int surveyId)
        {
            var surveyClientFolderPath = await _egnyteFolderResolver.GetSurveyClientFolderPath(surveyId);

            return await GetEgynteFolderPath(surveyClientFolderPath);
        }

        private async Task<Uri> GetFolderUrl(string surveyFolderPath)
        {
            var egynteFolderPath = await GetEgynteFolderPath(surveyFolderPath);

            var linkUrl = $"https://{_egnyteService.EgnyteDomain}.egnyte.com/app/index.do#storage/files/1{egynteFolderPath}";

            return new Uri(linkUrl);
        }

        private async Task<string> GetEgynteFolderPath(string surveyFolderPath)
        {
            var folderPath = await GetDocumentFolderPath(surveyFolderPath);

            if (folderPath == null)
            {
                folderPath = surveyFolderPath;

                var createdResponse = await _egnyteService.ExecuteEgnyteCall(client => client.Files.CreateFolder(folderPath));

                if (string.IsNullOrEmpty(createdResponse.FolderId))
                {
                    throw new Exception($"Could not create folder {folderPath}");
                }
            }

            return folderPath;
        }

        public async Task<Stream> DocumentDownload(Guid surveyFileDownloadGuid, string fileName, string path, bool isSecureDownload)
        {
            var surveyFolderPath = await _egnyteFolderResolver.GetSurveyFolderPath(surveyFileDownloadGuid, isSecureDownload);
            return await DownloadDocument(fileName, surveyFolderPath + path);
        }

        public async Task<Stream> ClientDocumentDownload(Guid surveyFileDownloadGuid, string fileName, bool isSecureDownload)
        {
            var surveyFolderPath = await _egnyteFolderResolver.GetSurveyClientFolderPath(surveyFileDownloadGuid, isSecureDownload);
            return await DownloadDocument(fileName, surveyFolderPath);
        }

        private async Task<Stream> DownloadDocument(string fileName, string surveyFolderPath)
        {
            var pathToDocument = await GetDocumentFolderPath(surveyFolderPath) + "/" + fileName;

            return (await _egnyteService.ExecuteEgnyteCall(async client =>
            {
                    FileOrFolderMetadata details ;
                    try
                    {
                        details = await client.Files.ListFileOrFolder(pathToDocument);
                    }
                    catch (EgnyteApiException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new DocumentNotFound($"Document {pathToDocument} not found", e);
                        }
                        throw;
                    }
                    if (details.IsFolder)
                    {
                        throw new DocumentNotFound($"Document {pathToDocument} was a folder");
                    }
                    if (details.AsFile == null || details.AsFile.NumberOfVersions == 0)
                    {
                        throw new DocumentNotFound($"Document {pathToDocument} has no versions");
                    }
                    await Task.Delay(1000);
                    return await client.Files.DownloadFileAsStream(pathToDocument);

            })).Data;
        }

        public async Task<UploadedFileMetadata> ClientDocumentUpload(int surveyId, string fileName, int fileSize, Stream fileStream)
        {
            var companyClientFolderLocation = GetClientFolderLocation(surveyId).Result;
            var pathToDocument = companyClientFolderLocation + "/" + fileName;

            const int oneHundredMb = 100 * 1024 * 1024;
            const int oneHundredAndTenMb = 110 * 1024 * 1024;
            var buffer = new byte[oneHundredMb];
            if (fileSize < oneHundredMb)
            {
                buffer = new byte[fileSize];
                await fileStream.ReadAsync(buffer, 0, fileSize);

                return await _egnyteService.ExecuteEgnyteCall(client =>
                    client.Files.CreateOrUpdateFile(pathToDocument, new MemoryStream(buffer)));
            }

            var bytesRead = oneHundredMb;
            await fileStream.ReadAsync(buffer, 0, oneHundredMb);

            var response = await _egnyteService.ExecuteEgnyteCall(client =>
                client.Files.ChunkedUploadFirstChunk(pathToDocument, new MemoryStream(buffer)));
            var chunkNumber = 1;

            // Minimum chunk size is 10MB, so check we have at least 110MB remaining
            while (fileSize - bytesRead > oneHundredAndTenMb)
            {
                chunkNumber++;
                bytesRead += oneHundredMb;
                buffer = new byte[oneHundredMb];
                await fileStream.ReadAsync(buffer, 0, oneHundredMb);

                await _egnyteService.ExecuteEgnyteCall(client =>
                    client.Files.ChunkedUploadNextChunk(pathToDocument, chunkNumber, response.UploadId,
                        new MemoryStream(buffer)));
            }

            chunkNumber++;
            var remainingBytes = fileSize - bytesRead;
            buffer = new byte[remainingBytes];
            await fileStream.ReadAsync(buffer, 0, remainingBytes);

            return await _egnyteService.ExecuteEgnyteCall(client =>
                client.Files.ChunkedUploadLastChunk(pathToDocument, chunkNumber, response.UploadId,
                    new MemoryStream(buffer)));
        }

        public async Task<bool> ClientDocumentDelete(int surveyId, string fileName)
        {
            var companyClientFolderLocation = GetClientFolderLocation(surveyId).Result;
            var pathToDocument = companyClientFolderLocation + "/" + fileName;

            return await _egnyteService.ExecuteEgnyteCall(client => client.Files.DeleteFileOrFolder(pathToDocument));
        }
    }
}
