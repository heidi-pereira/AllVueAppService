using System.IO;
using System.Reflection;
using System.Threading;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Png.Chunks;
using SixLabors.ImageSharp.Processing;
using Size = System.Drawing.Size;

namespace BrandVue.Services
{
    public class SeleniumService : ISeleniumService
    {
        private readonly IBookmarkRepository _bookmarkRepository;
        private readonly ILogger<SeleniumService> _logger;
        private readonly string _chromeLocation;
        private readonly string _reportingApiAccessToken;
        private readonly SemaphoreSlim _throttle;

        private const int ViewPortHeight = 5000;    //Height bigger than anything that can be generated...
        private const int ViewPortWidth = 5000;    //Width bigger than anything that can be generated...
        private const int TimeoutForPageLoad = 300;
        private const string XPathForLoadIndicator = "//span[@id=\'loaded\']";
        private const int SeleniumPool = 5;
        private const int MaxRetriesForSeleniumBecauseItSucksDonkeyBalls = 3;

        public SeleniumService(IBookmarkRepository bookmarkRepository, ILogger<SeleniumService> logger,
            string chromeLocation, string reportingApiAccessToken)
        {
            _bookmarkRepository = bookmarkRepository;
            _logger = logger;
            _chromeLocation = chromeLocation;
            _reportingApiAccessToken = reportingApiAccessToken;
            _throttle = new SemaphoreSlim(SeleniumPool);
        }

        public async Task<byte[]> ExportChart(string appBase, string appUrl, string name, string viewType, int width,
            int height, string[] metrics, string userName, string remoteIpAddress, string optionalOrganization)
        {
            // Right now we throw the width and height provided by the browser on the floor and rely on CSS providing the correct width and height to export.
            // In the future we could decide to use the width and height from the browser to influence the exported image dimensions.
            await _throttle.WaitAsync();
            return await Task.Run(()=>
            {
                try
                {
                    return ExportChartInternal(appBase, appUrl, name, viewType, metrics, userName, remoteIpAddress, optionalOrganization);
                }
                catch (Exception x)
                {
                    _logger.LogError(x, $"Error occurred while trying to export chart image with SeleniumService for url {appBase}{appUrl}");
                    throw;
                }
                finally
                {
                    _throttle.Release();
                }
            });
        }

        private byte[] ExportChartInternal(string appBase, string appUrl, string name, string viewType,
            string[] metrics, string userName, string remoteIpAddress, string optionalOrganization)
        {
            var chromeOptions = SetupChromeOptions(ViewPortWidth, ViewPortHeight);
            chromeOptions.SetLoggingPreference(LogType.Browser, OpenQA.Selenium.LogLevel.All);
            string chromeDriverDirectory = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty).AbsolutePath;
            using var cachedChromeDriver = new CachedChromeDriver(chromeDriverDirectory, chromeOptions);
            int retry = 0;

            while (true)
            {
                var uriBuilder = new UriBuilder(appBase + appUrl);
                // https://msdn.microsoft.com/en-us/library/system.uribuilder.query(v=vs.110).aspx
                var queryToAppend = $"appMode=export-image&BVReporting={_reportingApiAccessToken}";
                if (!string.IsNullOrEmpty(optionalOrganization))
                {
                    queryToAppend += $"&BVOrg={Uri.EscapeDataString(optionalOrganization)}";
                }
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                    ? queryToAppend
                    : uriBuilder.Query.Substring(1) + "&" + queryToAppend;
                
                _logger.LogInformation($"Making request with url '{uriBuilder.Uri.AbsoluteUri}'");

                cachedChromeDriver.Driver.Navigate().GoToUrl(uriBuilder.Uri);

                // Wait for the page to load
                var wait = new WebDriverWait(cachedChromeDriver.Driver, TimeSpan.FromSeconds(TimeoutForPageLoad));

                // Reply on BrandVue adding an element to the DOM to let us know
                IWebElement loadIndicatorElement = null;
                try
                {
                    loadIndicatorElement = wait.Until(d =>
                    {
                        try
                        {
                            return d.FindElement(By.XPath(XPathForLoadIndicator));
                        }
                        catch (NoSuchElementException)
                        {
                            return null;
                        }
                    });
                }
                catch (WebDriverTimeoutException ex)
                {
                    _logger.LogError(ex, "");
                }

                // Capture any client-side errors in logs
                TestForConsoleErrors(cachedChromeDriver.Driver);

                if (loadIndicatorElement == null)
                {
                    throw new Exception("Cannot find load indicator for page: " + cachedChromeDriver.Driver.Url);
                }

                // Check if the loaded indicator has an error attribute
                var isErrorPage = loadIndicatorElement.GetAttribute("data-error");
                if (!string.IsNullOrEmpty(isErrorPage) && isErrorPage.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException("Cannot export chart: An error occurred while loading the page. Please check that the chart loads correctly in your browser before attempting to export.");
                }

                // Find the body element
                var body = ((ISearchContext) cachedChromeDriver.Driver).FindElement(By.TagName("BODY"));

                //Obtain new heights from body
                int newHeight = body.Size.Height;
                int newWidth = body.Size.Width;

                if (newHeight > 0)
                {
                    // Resize the window to the dimensions the <body> element believes itself to be
                    cachedChromeDriver.Driver.Manage().Window.Size = new Size(newWidth, newHeight);

                    // Hide scrollbars (Chrome)
                    string script = "let style = document.createElement('style');";
                    script += "style.innerHTML = 'body::-webkit-scrollbar { display: none }';";
                    script += "document.head.appendChild(style);";
                    cachedChromeDriver.Driver.ExecuteScript(script);

                    // Get an image of the current rendered browser
                    var screenShot = cachedChromeDriver.Driver.TakeScreenshot().AsByteArray;

                    var screenShotWithProperties = AddPropertiesToPng(screenShot, new Dictionary<string, string>
                    {
                        {"AppBase", appBase},
                        {"AppUrl", appUrl},
                        {"Name", name},
                        {"ViewType", viewType},
                        {"Metrics", string.Join(",",metrics)},
                        {"Bookmark", _bookmarkRepository.GenerateRedirectFromUrl(appBase, appUrl, userName, remoteIpAddress) }
                    }, newWidth, newHeight);

                    return screenShotWithProperties;
                }

                if (retry == MaxRetriesForSeleniumBecauseItSucksDonkeyBalls)
                {
                    throw new Exception($"Exceeded {retry} attempts at getting {appBase}{appUrl}");
                }

                retry++;
                _logger.LogError("Retrying getting {AppBase}{AppUrl}, retry count: {RetryCount}", appBase, appUrl, retry);
            }
        }

        private ChromeOptions SetupChromeOptions(int width, int height)
        {
            var chromeOptions = new ChromeOptions();

            // Comment out to see the browser in development
            chromeOptions.AddArguments("headless");

            // Need to set this large enough so we don't get scrollbars and so that the exported image looks good in PowerPoint
            chromeOptions.AddArgument($"--window-size={width},{height}");
            chromeOptions.SetLoggingPreference(LogType.Browser, OpenQA.Selenium.LogLevel.Severe);

            //https://github.com/SeleniumHQ/selenium/issues/5457
            //For an unknown reason we started getting a time out error when exporting image
            //26 March 2019
            //This code should be removed, as MarkR. & DavidC did not understand what this did but it made it stop timing out
            //No Sandbox, means that it runs in a higher permissions level
            chromeOptions.AddArgument("-no-sandbox");

            if (!string.IsNullOrEmpty(_chromeLocation))
            {
                chromeOptions.BinaryLocation = _chromeLocation;
            }

            return chromeOptions;
        }

        private void TestForConsoleErrors(IWebDriver driver)
        {
            try
            {
                TestForConsoleErrorsUnhandled(driver);
            }
            catch (Exception ex)
            {
                // Added due to https://github.com/SeleniumHQ/selenium/issues/7342
                _logger.LogError(ex, "Failed to retrieve console log");
            }
        }

        private static string[] blacklistMessages = {
            "strict MIME checking is enabled",
            "404 (Not Found)"
        };

        private void TestForConsoleErrorsUnhandled(IWebDriver driver)
        {
            var logs = driver.Manage().Logs.GetLog(LogType.Browser);

            foreach (var logEntry in logs)
            {
                var isInBlacklist = blacklistMessages.Any(x => logEntry.Message.Contains(x));
                if (logEntry.Level == OpenQA.Selenium.LogLevel.Severe && !isInBlacklist)
                {
                    _logger.LogError("browser log at {TimeStamp}: {URL}: {Message}",
                        logEntry.Timestamp.ToLocalTime(), driver.Url, logEntry.Message);

                } else if (logEntry.Level == OpenQA.Selenium.LogLevel.Warning || (logEntry.Level == OpenQA.Selenium.LogLevel.Severe && isInBlacklist)) {
                    _logger.LogWarning("browser log at {TimeStamp}: {URL}: {Message}",
                        logEntry.Timestamp.ToLocalTime(), driver.Url, logEntry.Message);
                }
                else
                {
                    _logger.LogInformation("browser log at {TimeStamp}: {URL}: {Message}",
                        logEntry.Timestamp.ToLocalTime(),
                        driver.Url,
                        logEntry.Message);
                }
            }
        }

        private static byte[] AddPropertiesToPng(byte[] screenShot, IDictionary<string, string> properties, int width, int height)
        {
            using var image = Image.Load(screenShot);
            var newWidth = Math.Min(image.Width, width);
            var newHeight = Math.Min(image.Height, height);
            image.Mutate(i => i.Crop(new Rectangle(0, 0, newWidth, newHeight)));

            var pngTextData = properties.Select(k => new PngTextData($"{k.Key}", k.Value, "", ""));
            image.Metadata.GetFormatMetadata(PngFormat.Instance).TextData.AddRange(pngTextData);

            using var newPng = new MemoryStream();
            image.Save(newPng, PngFormat.Instance);
            return newPng.ToArray();
        }
    }
}