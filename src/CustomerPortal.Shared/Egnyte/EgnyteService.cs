using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Egnyte.Api;
using Egnyte.Api.Common;
using Egnyte.Api.Files;
using Newtonsoft.Json.Linq;

namespace CustomerPortal.Shared.Egnyte
{
    public class EgnyteService : IEgnyteService
    {
        private readonly string _egnyteDomain;
        private readonly string _egnyteClientId;
        private readonly string _egnyteUsername;
        private readonly string _egnytePassword;
        private readonly string _egnyteAccessToken;
        private EgnyteClient _egnyteClient;
        private const string Access_Token = "access_token";
        private const string EgnyteApi = ".egnyte.com/puboauth/token";
        private static readonly Random _random = new Random();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1,1);
        public EgnyteService(string egnyteDomain, string egnyteClientId, string egnyteUsername, string egnytePassword, string egnyteAccessToken)
        {
            _egnyteDomain = egnyteDomain;
            _egnyteClientId = egnyteClientId;
            _egnyteUsername = egnyteUsername;
            _egnytePassword = egnytePassword;
            _egnyteAccessToken = egnyteAccessToken;
        }

        public async Task<EgnyteClient> GetClient()
        {
            if (_egnyteClient != null)
            {
                return _egnyteClient;
            }

            await _semaphoreSlim.WaitAsync();
            try
            {
                if (_egnyteClient != null)
                {
                    return _egnyteClient;
                }

                var accessToken = await ExecuteWithRetry(GetAccessToken);

                _egnyteClient = new EgnyteClient(accessToken, _egnyteDomain);

                return _egnyteClient;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task<string> GetAccessToken()
        {
            if (!string.IsNullOrEmpty(_egnyteAccessToken))
            {
                return _egnyteAccessToken;
            }

            HttpResponseMessage response;

            using (var client = new HttpClient())
            {
                response = await client.PostAsync($"https://{_egnyteDomain}{EgnyteApi}",
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", _egnyteClientId),
                        new KeyValuePair<string, string>("username", _egnyteUsername),
                        new KeyValuePair<string, string>("password", _egnytePassword),
                        new KeyValuePair<string, string>("grant_type", "password"),
                    }));
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new EgnyteApiException($"Cannot get token to access {_egnyteDomain}", response, new Exception(responseJson));
            }

            var result = JObject.Parse(responseJson);

            var accessToken = result[Access_Token];

            if (accessToken == null || string.IsNullOrEmpty(accessToken.Value<string>()))
            {
                throw new Exception($"No access token returned from {_egnyteDomain}");
            }

            return accessToken.Value<string>();
        }

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
        {
            //https://egnyte.github.io/integrations-cookbook/throttling.html
            var retry = 0;

            async Task<bool> DoRetry(int minimumSecondsDelay)
            {
                retry++;
                if (retry > 5 || minimumSecondsDelay > 30)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(minimumSecondsDelay * 1000 + _random.Next(retry * 300)));
                return true;
            }

            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (ex is QPSLimitExceededException or RateLimitExceededException)
                {
                    string retryAfter = ex switch
                    {
                        QPSLimitExceededException qps => qps.RetryAfter,
                        RateLimitExceededException rate => rate.RetryAfter,
                        _ => throw new InvalidOperationException($"Unexpected exception type: {ex.GetType().FullName} encountered in retry logic.")
                    };
                    int minimumSeconds = int.TryParse(retryAfter, out var value) ? value : retry + 1;

                    if (!await DoRetry(minimumSeconds))
                    {
                        throw;
                    }
                }
                catch (EgnyteApiException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden)
                {
                    if (!await DoRetry(retry + 1))
                    {
                        throw;
                    }
                }
            }
        }

        public async Task<T> ExecuteEgnyteCall<T>(Func<EgnyteClient, Task<T>> action)
        {
            var client = await GetClient();
            return await ExecuteWithRetry(()=> action(client));
        }


        public async Task<FolderExtendedMetadata> GetEgnyteFolder(string folderPath)
        {
            FileOrFolderMetadata folderOrFile;

            try
            {
                folderOrFile = await ExecuteEgnyteCall(client => client.Files.ListFileOrFolder(folderPath));
            }
            catch (EgnyteApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }


            if (!folderOrFile.IsFolder)
            {
                throw new Exception($"{folderPath} is not a folder");
            }

            return folderOrFile.AsFolder;
        }

        public string EgnyteDomain => _egnyteDomain;
    }
}
