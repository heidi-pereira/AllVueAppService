using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using VueReporting.Services;

namespace VueReportingTests
{
    [TestFixture]
    class ReportGeneratorTests : TestBase
    {
        //[Test]
        //public void EndToEndReportGenerator()
        //{
        //    var reportGenerator = ServiceProvider.GetRequiredService<IReportGeneratorService>();
        //    var reportName = reportGenerator.GenerateReports("barometer.pptx", null).Result;
        //    Assert.DoesNotThrow(() =>
        //    {
        //        using (PresentationDocument.Open(reportName, false)){}
        //    });
        //}
    }
}
