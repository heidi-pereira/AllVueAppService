using System.Collections.Generic;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Services;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd
{
    [TestFixture]
    public class PagesExportTests
    {
        [Test]
        public void ExportPagesToCsv_Returns_File()
        {
            PagesController controller = new PagesController(Substitute.For<IPageHierarchyGenerator>(), 
                Substitute.For<ISubsetRepository>(), Substitute.For<IPagesRepository>(), 
                Substitute.For<IPanesRepository>(), Substitute.For<IPartsRepository>(), 
                Substitute.For<IPageAboutRepository>(), Substitute.For<IUserContext>());

            var result = controller.ExportPagesToExcel();

            Assert.That(result, Is.TypeOf(typeof(FileStreamResult)));
        }

        [Test]
        public void ExportPagesToExcel_Content_Returns_Excel()
        {
            IPagesRepository pagesRepository = Substitute.For<IPagesRepository>();
            pagesRepository.GetPages().Returns(new List<PageDescriptor>()
            {
                new PageDescriptor{Id = 1, Name = "Test Page 1"},
                new PageDescriptor{Id = 2, Name = "Test Page 2"}
            });

            PagesController controller = new PagesController(Substitute.For<IPageHierarchyGenerator>(),
                Substitute.For<ISubsetRepository>(), pagesRepository, 
                Substitute.For<IPanesRepository>(), Substitute.For<IPartsRepository>(),
                Substitute.For<IPageAboutRepository>(), Substitute.For<IUserContext>());

            var result = controller.ExportPagesToExcel();

            Assert.That(result.ContentType, Is.EqualTo(ExportHelper.MimeTypes.Excel));
        }
    }
}