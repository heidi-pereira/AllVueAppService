using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Egnyte.Api.Common;
using Microsoft.Extensions.Logging;

namespace VueReporting.EgnyteHelper
{
    /*
     * Cut down version of https://github.com/MIG-Global/Dashboard-Builder/blob/master/EgnyteClient/FileClient.cs adapted to work with stream
    */
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
                    _currentClientIndex = (ushort)((_currentClientIndex + 1) % _egnyteClients.Count);
                    return currentClient;
                }
            }
        }

        public async Task UploadStreamToFile(string egnyteFilePath, byte[] data)
        {
            _logger.LogInformation($"Uploading to {egnyteFilePath}");
            if (IsValidClient)
            {
                await RetryRequest(async () =>
                {
                    using var memoryStream = new MemoryStream(data);
                    return await CurrentClient.Files.CreateOrUpdateFile(egnyteFilePath, memoryStream);
                },
                uploadMetadata => uploadMetadata.Checksum.Equals(_sha512Hash.FromBytes(data),
                    StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                throw new InvalidOperationException("Egnyte client is invalid");
            }
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

        internal class Sha512Hash
        {
            public string FromBytes(byte[] bytes)
            {
                return Hash(sha => sha.ComputeHash(bytes));
            }

            public string FromFilePath(string destinationFilePath)
            {
                using (var fileStream = new FileStream(destinationFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return Hash(sha => sha.ComputeHash(fileStream));
                }
            }

            private string Hash(Func<SHA512, byte[]> computeHash)
            {
                using (var sha = SHA512.Create())
                {
                    var hash = computeHash(sha);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
        }
    }
}