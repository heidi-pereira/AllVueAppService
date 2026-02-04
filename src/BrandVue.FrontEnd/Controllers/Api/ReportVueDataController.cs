using BrandVue.EntityFramework;
using BrandVue.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using BrandVue.Services.ReportVue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("reportvue")]
    [CacheControl(NoStore = true)]
    public class ReportVueDataController : ApiController
    {
        private readonly string _allVueUploadFolder;
        private readonly ReportVuePathTransformationService _reportVuePathMapper;

        public ReportVueDataController(
            IProductContext productContext,
            AppSettings appSettings)
        {
            _allVueUploadFolder = appSettings.AllVueUploadFolder;
            _reportVuePathMapper = new ReportVuePathTransformationService(productContext);
        }


        [Authorize]
        [Route("{*filename}")]
        public IActionResult StaticFile(string filename)
        {
            var FileProvider = new PhysicalFileProvider(_allVueUploadFolder);
            var fileInfo = FileProvider.GetFileInfo(_reportVuePathMapper.ConvertURLToDiskLayout(filename));
            if (fileInfo.Exists)
            {
                switch (Path.GetExtension(fileInfo.PhysicalPath).ToLowerInvariant())
                {
                    case ".svg":
                        return PhysicalFile(fileInfo.PhysicalPath, "image/svg+xml");
                    case ".png":
                        return PhysicalFile(fileInfo.PhysicalPath, "image/png");
                    case ".jpg":
                        return PhysicalFile(fileInfo.PhysicalPath, "image/jpg");

                    case ".json":
                    default:
                        return PhysicalFile(fileInfo.PhysicalPath, "text/plain");

                }
            }
            return new NotFoundObjectResult(null);
        }
    }
}