using System.Collections.Specialized;
using BrandVue.EntityFramework;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd
{
    public class RequestScopeRetrieverTests
    {
        private const string Barometer = "barometer";
        private const string EatingOut = "eatingout";
        private const string HyphenatedName = "hyphenated-name";
        private const string Default = "common";
        private const string FirstLoaded = Barometer;
        private readonly NameValueCollection DefaultAppSettings = new NameValueCollection
        {
            { "localOrganisationOverride", "demo"},
            {"AppDeploymentEnvironment", "live"},
            {"AnswersConnectionString", @"Server=.\sql2022;Database=VueExport;TrustServerCertificate=True;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;Encrypt=True;"},
            {"MetaConnectionString", @"Server=.\sql2022;Database=BrandVueMeta;TrustServerCertificate=True;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;Encrypt=True;"},
            {"ReportingApiAccessToken", "<empty>"}
        };

        [TestCase("barOmeter.wgsn.com", "", Barometer, "WGSN")]
        [TestCase("carluccios.anydomain.com", "/eaTingout", EatingOut, "carluccios")]
        [TestCase("carluccios.anydomain.com", "/eAtingout/something", EatingOut, "carluccios")]
        [TestCase("carluccios.anydomain.com", "/eAtingout/something/somemore/stuff.html", EatingOut, "carluccios")]
        [TestCase("carluccios.anydomain.com", "/eatingout/", EatingOut, "carluccios")]
        [TestCase("savanta.anydomain.com", "/barometer/", Barometer, "savanta")]
        public void NonLocalMatchForPath(string host, string path, string expectedProductname, string expectedOrgShortCode)
        {
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), PathString.Empty, new PathString(path), false, DefaultAppSettings).ProductName, Is.EqualTo(expectedProductname));
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), PathString.Empty, new PathString(path), false, DefaultAppSettings).Organization, Is.EqualTo(expectedOrgShortCode));
        }

        [TestCase("barOmeter.wgsn.com", "", Barometer, "WGSN")]
        [TestCase("carluccios.anydomain.com", "/eaTingout", EatingOut, "carluccios")]
        [TestCase("carluccios.anydomain.com", "/eatingout/", EatingOut, "carluccios")]
        [TestCase("savanta.anydomain.com", "/barometer/", Barometer, "savanta")]
        public void NonLocalMatchForPathBase(string host, string pathBase, string expectedProductname, string expectedOrgShortCode)
        {
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), new PathString(pathBase), PathString.Empty, false, DefaultAppSettings).ProductName, Is.EqualTo(expectedProductname));
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), new PathString(pathBase), PathString.Empty, false, DefaultAppSettings).Organization, Is.EqualTo(expectedOrgShortCode));
        }

        [TestCase("test-barometer.morar.co", "", Barometer, "WGSN")]
        [TestCase("beta-barometer.morar.co", "", Barometer, "WGSN")]
        public void NonLocalMatchMorarDomainForLowerEnvironmentBarometer(string host, string pathBase, string expectedProductname, string expectedOrgShortCode)
        {
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), new PathString(pathBase), PathString.Empty, false, DefaultAppSettings).ProductName, Is.EqualTo(expectedProductname));
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), new PathString(pathBase), PathString.Empty, false, DefaultAppSettings).Organization, Is.EqualTo(expectedOrgShortCode));
        }

        [TestCase("localhost", Barometer, "demo")]
        [TestCase("anything", Barometer, "demo")]
        public void LocalWithNoOverride(string host, string expectedProductname, string expectedOrgShortCode)
        {
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), PathString.Empty, PathString.Empty, true, DefaultAppSettings).ProductName, Is.EqualTo(expectedProductname));
            Assert.That(GetRequestScope(expectedProductname, new HostString(host), PathString.Empty, PathString.Empty, true, DefaultAppSettings).Organization, Is.EqualTo(expectedOrgShortCode));
        }

        [TestCase("barometer.wgsn.com", "", Barometer)]
        [TestCase("carluccios.anydomain.com", "/eatingout", EatingOut)]
        public void LocalRequestOnServer(string host, string path, string expectedProductname)
        {
            var requestScope = GetRequestScope(expectedProductname,
                new HostString(host), PathString.Empty, new PathString(path), true, DefaultAppSettings);
            Assert.That(requestScope.ProductName, Is.EqualTo(expectedProductname));
        }

        [TestCase("barometer.wgsn.com", "", Barometer)]
        [TestCase("carluccios.anydomain.com", "/eatingout", EatingOut)]
        public void NonLocalRequest(string host, string path, string expectedProductname)
        {
            var requestScope = GetRequestScope(expectedProductname,
                new HostString(host), PathString.Empty, new PathString(path), false, DefaultAppSettings);
            Assert.That(requestScope.ProductName, Is.EqualTo(expectedProductname));
        }

        [TestCase("demo.test-vue-te.ch", "/barometer-1234", Barometer)]
        public void TestRequestOnPreMergeBranch(string host, string path, string expectedProductname)
        {
            var requestScope = GetRequestScope(expectedProductname,
                new HostString(host), PathString.Empty, new PathString(path), false, DefaultAppSettings, isTokenBasedAuthEnabled: true);
            Assert.That(requestScope.ProductName, Is.EqualTo(expectedProductname));
        }

        [TestCase("/")]
        [TestCase("")]
        public void MissingProductNameIsFirstLoadedForLoadingStaticFiles(string requestPath)
        {
            Assert.That(
                GetRequestScope(FirstLoaded, new HostString("anydomain.com"), PathString.Empty, new PathString(requestPath), false, DefaultAppSettings).ProductName, Is.EqualTo(FirstLoaded));
        }

        private RequestScope GetRequestScope(string expectedProductName, HostString urlHost, PathString requestPathBase,
            PathString requestPath, bool currentRequestIsLocal, NameValueCollection appSettings = null, bool isTokenBasedAuthEnabled = false)
        {
            appSettings ??= new NameValueCollection();
            appSettings["ProductsToLoadDataFor"] = expectedProductName;
            appSettings["MaxConcurrentDataLoaders"] = 100.ToString();
            var requestScopeRetriever = new RequestScopeRetriever(new AppSettings(appSettingsCollection: appSettings));
            return requestScopeRetriever.GetRequestScope(urlHost, requestPathBase, requestPath, currentRequestIsLocal, false,appSettings: appSettings);
        }
    }
}
