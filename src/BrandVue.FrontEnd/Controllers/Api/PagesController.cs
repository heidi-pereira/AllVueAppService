using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.Services;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using Vue.AuthMiddleware;
using static BrandVue.MixPanel.MixPanel;
using BrandVue.MixPanel;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    [NbspFilter]
    public class PagesController : ApiController
    {
        private readonly IPageHierarchyGenerator _pageHierarchyGenerator;
        private readonly ISubsetRepository _subsetsRepository;
        private readonly IPagesRepository _pagesRepository;
        private readonly IPanesRepository _panesRepository;
        private readonly IPartsRepository _partsRepository;
        private readonly IPageAboutRepository _pageAboutRepository;
        private readonly IUserContext _userContext;

        public PagesController(
            IPageHierarchyGenerator pageHierarchyGenerator,
            ISubsetRepository subsetsRepository,
            IPagesRepository pagesRepository,
            IPanesRepository panesRepository,
            IPartsRepository partsRepository,
            IPageAboutRepository pageAboutRepository,
            IUserContext userContext)
        {
            _pageHierarchyGenerator = pageHierarchyGenerator;
            _subsetsRepository = subsetsRepository;
            _pagesRepository = pagesRepository;
            _panesRepository = panesRepository;
            _partsRepository = partsRepository;
            _pageAboutRepository = pageAboutRepository;
            _userContext = userContext;
        }

        [HttpGet]
        [Route("pages")]
        [SubsetAuthorisation(nameof(selectedSubsetId))]
        public IEnumerable<PageDescriptor> GetPages(string selectedSubsetId)
        {
            return _pageHierarchyGenerator.GetHierarchy(_subsetsRepository.Get(selectedSubsetId));
        }

        [HttpGet]
        [Route("allpages")]
        [RoleAuthorisation(Roles.Administrator)]
        public IEnumerable<PageDescriptor> GetPagesForAllSubsets()
        {
            return _pageHierarchyGenerator.GetHierarchy();
        }

        [HttpPost]
        [Route("pages")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [SubsetAuthorisation]
        public async Task<PageDescriptor> CreatePage([FromBody] PageDescriptor page)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.CreateNewPage,
                _userContext.UserId,
                GetClientIpAddress()));
            _pagesRepository.CreatePage(page);
            return page;
        }

        [HttpPut]
        [Route("pages/{pageId}")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [SubsetAuthorisation]
        public async Task<PageDescriptor> UpdateOrCreatePage(int pageId, [FromBody] PageDescriptor page)
        {
            page.Id = pageId;
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.UpdatePageConfiguration,
                _userContext.UserId,
                GetClientIpAddress()));
            _pagesRepository.UpdatePage(page);
            return page;
        }

        [HttpDelete]
        [Route("pages/{pageId}")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<HttpStatusCode> DeletePage(int pageId)
        {
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.DeletePage,
                _userContext.UserId,
                GetClientIpAddress()));
            _pagesRepository.DeletePage(pageId);
            return HttpStatusCode.OK;
        }

        [HttpGet]
        [Route("PagesExcelExport")]
        public FileStreamResult ExportPagesToExcel()
        {
            ExportToExcel exporter = new ExportToExcel(_pagesRepository, _panesRepository, _partsRepository);
            exporter.ExportPagesPanesAndParts();
            Stream stream = exporter.ToStream();
            return File(stream, ExportHelper.MimeTypes.Excel);
        }

        [HttpGet]
        [Route("pageAbouts")]
        public IEnumerable<PageAbout> GetPageAbouts(int pageId)
        {
            return _pageAboutRepository.GetAllForPage(pageId);
        }

        [HttpPost]
        [Route("pageAbouts")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public PageAbout CreatePageAbout([FromBody] PageAbout pageAbout)
        {
            pageAbout.User = _userContext.UserName;
            _pageAboutRepository.Create(pageAbout);
            return pageAbout;
        }

        [HttpPut]
        [Route("pageAbouts")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public PageAbout[] UpdatePageAboutList([FromBody] PageAbout[] pageAbouts)
        {
            foreach (var pageAbout in pageAbouts)
            {
                pageAbout.User = _userContext.UserName;
            }

            _pageAboutRepository.UpdateList(pageAbouts);
            return pageAbouts;
        }

        [HttpDelete]
        [Route("pageAbouts")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public HttpStatusCode DeletePageAbout([FromBody] PageAbout pageAbout)
        {
            // Update username so we can keep track of who deleted it
            pageAbout.User = _userContext.UserName;
            _pageAboutRepository.Delete(pageAbout);
            return HttpStatusCode.OK;
        }
    }
}