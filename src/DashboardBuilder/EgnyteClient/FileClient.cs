using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Egnyte.Api.Common;
using Egnyte.Api.Files;
using Microsoft.Extensions.Logging;

namespace EgnyteClient
{
    public class FileClient
    {
        private readonly List<Egnyte.Api.EgnyteClient> _egnyteClients;
        private ushort _currentClientIndex;
        private readonly ILogger _logger;
        private readonly Sha512Hash _sha512Hash;
        private DateTime _nextRequestCanStartAfter = DateTime.Now;
        private readonly int _msToWaitBetweenRequests;

        public FileClient(IEnumerable<string> egnyteBearerTokens, string egnyteSubdomain, ILoggerFactory loggerFactory)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _logger = loggerFactory.CreateLogger<FileClient>();
            _egnyteClients = egnyteBearerTokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct()
                .Select(token => new Egnyte.Api.EgnyteClient(token.Trim(), egnyteSubdomain)).ToList();
            _msToWaitBetweenRequests = 0;
            if (IsValidClient)
                _msToWaitBetweenRequests = 505 / _egnyteClients.Count; // Aim for just under quota of 2 per second per distinct bearer token
            _sha512Hash = new Sha512Hash();
        }

        public bool IsValidClient => _egnyteClients.Count != 0;

        private Egnyte.Api.EgnyteClient CurrentClient
        {
            get
            {
                lock (_egnyteClients)
                {
                    var currentClient = _egnyteClients.ElementAt(_currentClientIndex);
                    _currentClientIndex = (ushort) ((_currentClientIndex + 1) % _egnyteClients.Count);
                    return currentClient;
                }
            }
        }

        public async Task UploadFile(string egnyteFilePath, FileInfo fileToUpload)
        {
            _logger.LogInformation($"Uploading {fileToUpload.FullName} to {egnyteFilePath}");
            if (IsValidClient)
            {
                await RetryRequest(async () =>
                    {
                        using (var ms = new MemoryStream())
                        using (var fileStream =
                            new FileStream(fileToUpload.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            await fileStream
                                .CopyToAsync(
                                    ms); //It's a bit inefficient to redo this, but the memory stream gets closed otherwise by the client code
                            ms.Position = 0;
                            return await CurrentClient.Files.CreateOrUpdateFile(egnyteFilePath, ms);
                        }
                    },
                    uploadMetadata => uploadMetadata.Checksum.Equals(_sha512Hash.FromFilePath(fileToUpload.FullName),
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<(List<FolderMetadata> Folders, List<FileBasicMetadata> Files)> GetEgnyteFilesAndFolders(string egnyteFolderPath, Func<string, bool> skipEgnytePath)
        {
            var egnyteFilesAndFolders = await RetryRequest(() => CurrentClient.Files.ListFileOrFolder(egnyteFolderPath));
            if (!egnyteFilesAndFolders.IsFolder)
                throw new ArgumentException($"{nameof(egnyteFolderPath)} should be a folder, but {egnyteFolderPath} is a file",
                    nameof(egnyteFolderPath));
            return (egnyteFilesAndFolders.AsFolder.Folders.Where(f => !skipEgnytePath(f.Path)).ToList(), egnyteFilesAndFolders.AsFolder.Files.Where(f => !skipEgnytePath(f.Path)).ToList());
        }

        public async Task<bool> DownloadFile(FileBasicMetadata fileMetadata, string destinationFilePath, Func<string, string, DateTime, Task<bool>> shouldKeepNewFile)
        {
            if (File.Exists(destinationFilePath) && fileMetadata.Checksum.Equals(_sha512Hash.FromFilePath(destinationFilePath), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Skipping {destinationFilePath} because it's already up to date");
                return false;
            }

            _logger.LogInformation($"Downloading {fileMetadata.Path} to {Path.GetDirectoryName(destinationFilePath)}");
            var downloadedFile = await RetryRequest(() => CurrentClient.Files.DownloadFile(fileMetadata.Path),
                f => IsValidResponse(fileMetadata, f));

            await WriteFile(fileMetadata, destinationFilePath, shouldKeepNewFile, downloadedFile);
            return true;
        }

        private async Task WriteFile(FileBasicMetadata fileMetadata, string destinationFilePath,
            Func<string, string, DateTime, Task<bool>> shouldKeepNewFile, DownloadedFile downloadedFile)
        {
            var previousFileBytes = File.Exists(destinationFilePath) ? await File.ReadAllBytesAsync(destinationFilePath) : null;
            await File.WriteAllBytesAsync(destinationFilePath, downloadedFile.Data);

            try
            {
                if (await shouldKeepNewFile(destinationFilePath, fileMetadata.UploadedBy, fileMetadata.LastModified)) return;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            if (previousFileBytes != null)
            {
                await File.WriteAllBytesAsync(destinationFilePath, previousFileBytes);
                _logger.LogInformation($"Restored previous version to {destinationFilePath}");
            }
            else
            {
                File.Delete(destinationFilePath);
                _logger.LogWarning($"Removed {destinationFilePath}");
            }
        }

        public async Task<DownloadedFile> DownloadFile(string path)
        {
            _logger.LogInformation($"Downloading {path}");
            var downloadedFile = await RetryRequest(() => CurrentClient.Files.DownloadFile(path), file => true);
            return downloadedFile;
        }

        private bool IsValidResponse(FileBasicMetadata fileMetadata, DownloadedFile downloaded)
        {
            var areEqual = fileMetadata.Checksum.Equals(_sha512Hash.FromBytes(downloaded.Data), StringComparison.OrdinalIgnoreCase);
            if (!areEqual)
            {
                var responseString = Encoding.UTF8.GetString(downloaded.Data, 0, Math.Min(200, downloaded.Data.Length));
                if (responseString.Trim().Equals("<h1>Developer Over Qps</h1>")) //Hack around issue in Egnyte API which incorrectly returns success sometimes
                {
                    _logger.LogInformation($"Request for {fileMetadata.Name} forbidden due to exceeding quota");
                    _nextRequestCanStartAfter = _nextRequestCanStartAfter.AddSeconds(1);
                }
                else
                {
                    _logger.LogWarning(
                        $"{downloaded.Data.Length} bytes of downloaded data last modified {downloaded.LastModified} had a different hash to the metadata listing filesize as {fileMetadata.Size}, last modified {fileMetadata.LastModified}");
                    _logger.LogWarning(
                        "The first 200 bytes of the downloaded file interpreted as UTF were: {0}",
                        responseString);
                }
            }
            return areEqual;
        }


        private async Task<T> RetryRequest<T>(Func<Task<T>> clientAction, Func<T, bool> isValidResponse = null)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await AvoidHammeringApiAsync();
                    var result = await clientAction();
                    if (isValidResponse == null || isValidResponse(result))
                    {
                        return result;
                    }

                    _logger.LogInformation($"Retrying {clientAction.Method.Name} because the API call was not successful");
                }
                catch (EgnyteApiException e) when (e.StatusCode == HttpStatusCode.Forbidden &&
                                                   e.Message.ToUpperInvariant().Contains("OVER"))
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, i));
                    _nextRequestCanStartAfter = DateTime.Now.Add(delay);
                    _logger.LogInformation($"Request {clientAction.Method.Name} forbidden due to exceeding quota");
                }
                catch (EgnyteApiException e) when (e.Message.ToUpperInvariant().Contains("GATEWAY TIMEOUT"))
                {
                    var delay = TimeSpan.FromMinutes(Math.Pow(4, i));
                    _nextRequestCanStartAfter = DateTime.Now.Add(delay);
                    _logger.LogInformation(
                        $"Request {clientAction.Method.Name} timed out - egnyte or our internet connection is probably down");
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    var delay = TimeSpan.FromMinutes(Math.Pow(4, i));
                    _nextRequestCanStartAfter = DateTime.Now.Add(delay);
                    _logger.LogInformation($"Request {clientAction.Method.Name} timed out - egnyte or our internet connection is probably down", ex);
                }
            }
            throw new InvalidDataException($"Request {clientAction.Method.Name} failed after several retries");
        }

        private async Task AvoidHammeringApiAsync()
        {
            var now = DateTime.Now;
            if (_nextRequestCanStartAfter > now)
            {
                await Task.Delay(_nextRequestCanStartAfter.Subtract(now));
            }
            _nextRequestCanStartAfter = DateTime.Now.AddMilliseconds(_msToWaitBetweenRequests);
        }
    }
}