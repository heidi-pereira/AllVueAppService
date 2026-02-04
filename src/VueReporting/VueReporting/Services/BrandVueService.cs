using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VueReporting.Models;

namespace VueReporting.Services
{
    public class BrandVueService : IBrandVueService
    {
        private readonly ILogger<IBrandVueService> _logger;
        private readonly IAppSettings _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public BrandVueService(ILogger<IBrandVueService> logger, IAppSettings appSettings, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Stream> ExportChart(Uri url, string viewType, string name, int width, int height, string[] metrics, string root)
        {
            try
            {
                var json = new JsonContent(new
                {
                    Url = url,
                    Name = name,
                    ViewType = viewType,
                    Width = width,
                    Height = height,
                    Metrics = metrics
                });

                var exportApiUrl = new Uri(root + "/api/data/chartexport");
                var httpClient = _httpClientFactory.CreateClient(Constants.DefaultReportingClient);
                httpClient.DefaultRequestHeaders.Add("X-BVReporting", _appSettings.ReportingApiAccessToken);
                _logger.LogInformation($"Posting url '{exportApiUrl}'");
                var response = await httpClient.PostAsync(exportApiUrl, json);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new InvalidOperationException(response.Content.ReadAsStringAsync().Result);
                }
                else if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Image export via {exportApiUrl} with payload {json.ReadAsStringAsync().Result} returned status code {response.StatusCode}", new Exception(response.Content.ReadAsStringAsync().Result));
                }

                if (response.Content.Headers.ContentType.MediaType != "image/png")
                {
                    throw new Exception($"Image export via {exportApiUrl} with payload {json.ReadAsStringAsync().Result} returned media type of {response.Content.Headers.ContentType.MediaType} which was not expected.");
                }

                return await response.Content.ReadAsStreamAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }

        public Uri GetUrlFromBookmark(string appBase, string bookmark, string root)
        {
            // All a bit nasty as we have to support potential for bookmark from either of possible sites!

            var bookmarkUris = new[]
            {
                AllVueUrlForBookmark(appBase, bookmark, root),
                UrlForBookmark(appBase, bookmark, root),
                UrlForBookmarkNotAdjustedForEnvironment(appBase, bookmark),
            };
            try
            {
                foreach (var bookmarkUri in bookmarkUris)
                {
                    var httpClient = _httpClientFactory.CreateClient(Constants.BookmarkUrlClient);
                    httpClient.DefaultRequestHeaders.Add("X-BVReporting", _appSettings.ReportingApiAccessToken);
                    HttpResponseMessage response;
                    try
                    {
                        response = httpClient.GetAsync(bookmarkUri).Result;
                    }
                    catch (Exception)
                    {
                        //invalid host or other error, try next one
                        continue;
                    }

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        continue;
                    }

                    if (response.StatusCode != HttpStatusCode.Redirect)
                    {
                        _logger.LogError(
                            $"Unexpected response from redirect. Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                    }

                    var redirect = response.Headers.Location;
                    return redirect;
                }

                // Each of them failed!
                throw new Exception("Cannot find bookmark");
            }
            catch (Exception ex)
            {
                var message = $"Error getting bookmark for {string.Join(", ", bookmarkUris.Select(x => x.ToString()))}";
                _logger.LogError(ex, message);
                throw new Exception(message, ex);
            }
        }

        public Uri UrlForBookmark(string appBase, string bookmark, string root)
        {
            var uriBuilder = new UriBuilder(UrlForBookmarkNotAdjustedForEnvironment(appBase, bookmark));
            uriBuilder.Host = AdjustHostForCurrentEnvironment(uriBuilder.Host, root);

            return uriBuilder.Uri;
        }

        public Uri AllVueUrlForBookmark(string appBase, string bookmark, string root)
        {
            var uriBuilder = new UriBuilder(UrlForBookmarkNotAdjustedForEnvironment(appBase, bookmark));
            uriBuilder.Host = AdjustHostForAllVueCurrentEnvironment(uriBuilder.Host, root);

            return uriBuilder.Uri;
        }

        public Uri UrlForBookmarkNotAdjustedForEnvironment(string appBase, string bookmark)
        {
            return new Uri(appBase + "bookmark/" + bookmark);
        }

        public static string AdjustHostForCurrentEnvironment(string host, string currentRoot)
        {
            // This method is only required if we want to continue to support embedding images from different environments into reports.
            // i.e. the bookmark URL should be on the same environment as the current one for reporting, even if the redirect it to
            // a different environment!
            var authoritySplit = new Uri(currentRoot).Authority.Split('.');
            if (authoritySplit.Length < 2)
            {
                return host;
            }
            var hostParts = host.Split('.');
            authoritySplit[0] = hostParts[0];
            var adjustedHost = string.Join('.', authoritySplit);

            return adjustedHost;
        }

        public static string AdjustHostForAllVueCurrentEnvironment(string host, string currentRoot)
        {
            // This method is only required if we want to continue to support embedding images from different environments into reports.
            // i.e. the bookmark URL should be on the same environment as the current one for reporting, even if the redirect it to
            // a different environment!
            var authoritySplit = new Uri(currentRoot).Authority.Split('.');
            var authorityLength = authoritySplit.Length;
            if (authorityLength < 2)
            {
                return host;
            }

            var hostSubDomain = host.Split('.')[0];
            var adjustedHost = $"{hostSubDomain}.{string.Join('.', authoritySplit.Skip(1))}";

            var hostBase = host.Split('/');
            if (hostBase.Length > 1)
            {
                adjustedHost = $"{adjustedHost}/{string.Join('/', hostBase.Skip(1))}";
            }

            return adjustedHost;
        }

        private string GetIdForDefaultSubset(string root)
        {
            return CallApi<List<Subset>>(new Uri(root + "/api/meta/subsets")).First().Id;
        }

        public IReadOnlyCollection<EntitySet> GetBrandSets(string root, string subsetId)
        {
            var subsetEntityConfigurationModel = CallApi<SubsetEntityConfigurationModel>(new Uri(root + "/api/meta/entitytypeconfigurationmodelsall?selectedSubsetId=" + Uri.EscapeDataString(subsetId ?? GetIdForDefaultSubset(root))));
            var entityTypeConfigurationModel = subsetEntityConfigurationModel.EntityTypeConfigurationModels.First(x => x.EntityType == EntityType.Brand);
            foreach (var entitySet in entityTypeConfigurationModel.EntitySets)
            {
                entitySet.MainInstanceName = entityTypeConfigurationModel.AllInstances.SingleOrDefault(e => e.Id == entitySet.MainInstanceId)?.Name;
            }
            return entityTypeConfigurationModel.EntitySets;
        }

        public EntityInstance[] GetWholeMarket(string root, string subsetId)
        {
            var subsetEntityConfigurationModel = CallApi<SubsetEntityConfigurationModel>(new Uri(root + "/api/meta/entitytypeconfigurationmodels?selectedSubsetId=" + Uri.EscapeDataString(subsetId ?? GetIdForDefaultSubset(root))));
            return subsetEntityConfigurationModel.EntityTypeConfigurationModels.First(x => x.EntityType == EntityType.Brand).AllInstances.ToArray();
        }

        private T CallApi<T>(Uri uri)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient(Constants.DefaultReportingClient);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                httpRequestMessage.Headers.Add("X-BVReporting", _appSettings.ReportingApiAccessToken);

                var response = httpClient.SendAsync(httpRequestMessage).Result;
                response.EnsureSuccessStatusCode();
                var json = response.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (WebException webException)
            {
                string errorResponseBody;
                try
                {
                    var streamReader = new StreamReader(webException.Response.GetResponseStream());
                    errorResponseBody = "Response body: " + streamReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    errorResponseBody = "Unable to read response body: " + ex.Message;
                }

                throw new Exception($"Error calling '{uri}' - {errorResponseBody}", webException);
            }
            catch (HttpRequestException httpRequestException)
            {
                throw new Exception($"Error calling '{uri}'", httpRequestException);
            }
        }
    }

    internal class JsonContent : StringContent
    {
        public JsonContent(object obj) :
            base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        {
        }
    }
}
