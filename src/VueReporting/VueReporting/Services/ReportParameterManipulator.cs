using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using VueReporting.Models;
using static System.String;

namespace VueReporting.Services
{
    public class ReportParameterManipulator : IReportParameterManipulator
    {
        private readonly EntitySet _brandSet;
        private readonly IBrandVueService _brandVueService;
        private readonly IAppSettings _appSettings;
        private long? _mainBrandId;

        public ReportParameterManipulator(EntitySet brandSet, IBrandVueService brandVueService, IAppSettings appSettings)
        {
            _brandSet = brandSet;
            _brandVueService = brandVueService;
            _appSettings = appSettings;
            _mainBrandId = _brandSet.MainInstanceId;
        }


        private long? GetMainBrand(Uri url, string productFilter, string root)
        {
            long? mainBrandId;

            switch (_brandSet.Name)
            {
                case ReportConstants.OriginalBrands:
                    mainBrandId = GetBrandFromQueryString(url.Query, root);
                    break;
                case ReportConstants.CurrentBrands:
                    mainBrandId = GetBrandFromQueryString(productFilter, root);
                    break;
                default:
                    mainBrandId = _brandSet.MainInstanceId;
                    break;
            }

            return mainBrandId;

            long? GetBrandFromQueryString(string appSettingsProductFilter, string root)
            {
                var productFilters = HttpUtility.ParseQueryString(appSettingsProductFilter);
                long.TryParse(productFilters[QueryStringParamNames.Active], out long activeBrandId);
                return activeBrandId;
            }
        }

        public Uri UpdateUrl(Uri url, DateTimeOffset reportDate, string productFilter, string root)
        {
            if (_mainBrandId == null)
            {
                _mainBrandId = GetMainBrand(url, productFilter, root);
            }
            var queryParameters = HttpUtility.ParseQueryString(url.Query);

            ApplyProductFilters(queryParameters, productFilter);

            RemoveFilteredParameters(queryParameters);

            UpdateDateForReport(queryParameters, reportDate);

            switch (_brandSet.Name)
            {
                case ReportConstants.OriginalBrands:
                    break;
                case ReportConstants.CurrentBrands:
                    UpdateQueryWithCurrentBrands(queryParameters);
                    break;
                default:
                    UpdateQueryWithBrandset(queryParameters);
                    break;
            }

            var updatedUri = ConstructUpdatedUrl(url, queryParameters);

            return updatedUri;
        }

        private void UpdateQueryWithCurrentBrands(NameValueCollection queryParameters)
        {
            if (_appSettings.ProductFilter != null)
            {
                var productFilters = HttpUtility.ParseQueryString(_appSettings.ProductFilter);
                queryParameters[QueryStringParamNames.Active] = productFilters[QueryStringParamNames.Active];

                if (!ActiveEntityTypeIsBrand(queryParameters))
                {
                    queryParameters[QueryStringParamNames.FilterBy] = productFilters[QueryStringParamNames.Active];
                    return;
                }
            }
        }
        
        private void UpdateQueryWithBrandset(NameValueCollection queryParameters)
        {
            if (_mainBrandId != null)
            {
                queryParameters[QueryStringParamNames.Active] = _mainBrandId.ToString();
            }
            
            if (!ActiveEntityTypeIsBrand(queryParameters))
            {
                queryParameters[QueryStringParamNames.FilterBy] = _mainBrandId?.ToString();
                return;
            }

            if (_brandSet.InstanceIds.Any())
            {
                queryParameters[QueryStringParamNames.Highlighted] = Join(".", _brandSet.InstanceIds);
            }
        }

        private static bool ActiveEntityTypeIsBrand(NameValueCollection queryParameters)
        {
            var splitByValueFromUrl = queryParameters.Get("SplitBy");
            // We default to brand because in old report templates SplitBy parameter is not in the URL
            if (splitByValueFromUrl is null)
            {
                return true;
            }

            return splitByValueFromUrl.Equals("brand", StringComparison.OrdinalIgnoreCase);
        }

        public void ApplyProductFilters(NameValueCollection queryParameters, string productFilter)
        {
            if (productFilter == null) return;
            
            var productFilters = HttpUtility.ParseQueryString(productFilter);

            foreach (var item in productFilters.AllKeys)
            {
                if (item.Equals(QueryStringParamNames.Subset))
                {
                    if (!_brandSet.Name.Equals(ReportConstants.OriginalBrands))
                    {
                        queryParameters[item] = productFilters[item];
                    }
                }
                else if (queryParameters[item] == null && !_appSettings.ExcludedFilters.Contains(item, StringComparer.InvariantCultureIgnoreCase))
                {
                    queryParameters[item] = productFilters[item];
                }
            }
        }

        private Uri ConstructUpdatedUrl(Uri url, NameValueCollection queryParameters)
        {
            var urlAuthority = url.Authority;

            if (!IsNullOrEmpty(_brandSet?.Organisation))
            {
                var authorityParts = url.Authority.Split('.');
                // If more than one part to the host (i.e. not localhost), assume that url in format
                // organisation.{environment}-vue-te.ch
                // and therefore replace the organisation with the organisation of the main brand for the brandset.
                if (authorityParts.Length > 1)
                {
                    urlAuthority = Join('.', new[] { _brandSet.Organisation}.Union(authorityParts.Skip(1)));
                }
            }

            string uriString = null;
            try
            {
                var queryString = queryParameters.Count == 0 ? Empty : "?" + Join("&", queryParameters);
                uriString = $"{url.Scheme}{Uri.SchemeDelimiter}{urlAuthority}{url.AbsolutePath}{queryString}";
                return new Uri(uriString);
            }
            catch (UriFormatException uriFormatException)
            {
                throw new Exception($"Error creating uri from '{uriString}'", uriFormatException);
            }
        }

        public void RemoveFilteredParameters(NameValueCollection queryParameters)
        {
            foreach (var key in _appSettings.RemoveFilters)
            {
                queryParameters.Remove(key);
            }
        }

        private static void UpdateDateForReport(NameValueCollection queryParameters, DateTimeOffset reportDate)
        {
            // Set now - this is the date from which the report will run
            queryParameters["Now"] = reportDate.ToString("yyyy-MM-dd");

        }

        public string Name => _brandSet.Name;

        public bool ReplaceTokens(string text, DateTimeOffset reportDate, out string textWithReplacedTokens)
        {
            int Quarter(DateTimeOffset date) => (int)(Math.Ceiling(date.Month / 3.0));
            var tokenReplacers = new Dictionary<string, Func<string>>
            {
                { "#BrandName#", () => _brandSet.MainInstanceName ?? "Unknown Brand" },
                { "#Date#", () => reportDate.ToString("MMMM yyyy") },
                { "#Quarter#", () => $"Q{Quarter(reportDate)} {reportDate.Year}" }
            };

            textWithReplacedTokens = text;
            
            if (!tokenReplacers.Any(tr => text.Contains(tr.Key)))
            {
                return false;
            }

            foreach (var tokenReplacer in tokenReplacers)
            {
                if (textWithReplacedTokens.Contains(tokenReplacer.Key))
                {
                    textWithReplacedTokens = textWithReplacedTokens.Replace(tokenReplacer.Key, tokenReplacer.Value());
                }
            }

            return true;
        }


        public static string Replace(string s, int index, int length, string replacement)
        {
            var builder = new StringBuilder();
            builder.Append(s.Substring(0, index));
            builder.Append(replacement);
            builder.Append(s.Substring(index + length));
            return builder.ToString();
        }

    }

    public static class ReportConstants
    {
        public const string OriginalBrands = "OriginalBrands";
        public const string CurrentBrands = "CurrentBrands";
    }

    public static class QueryStringParamNames
    {
        public const string FilterBy = "FilterBy";
        public const string Active = "Active";
        public const string Subset = "Subset";
        public const string Highlighted = "Highlighted";
    }
}