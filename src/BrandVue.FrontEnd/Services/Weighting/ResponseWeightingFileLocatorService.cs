using System.IO;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework;
using Microsoft.Extensions.FileProviders;

namespace BrandVue.Services.Weighting
{
    public interface IFileLocatorService
    {
        string PhysicalRoot { get; }
        string GetFullFileName(string path);
        IFileInfo GetFileInfo(string fileName);
    }

    public class ResponseWeightingFileLocatorServiceService : IFileLocatorService
    {
        private readonly IProductContext _productContext;
        private readonly string _allVueUploadFolder;

        public ResponseWeightingFileLocatorServiceService(IProductContext productContext, AppSettings appSettings)
        {
            _productContext = productContext;
            _allVueUploadFolder = appSettings.AllVueUploadFolder;
        }

        public string PhysicalRoot
        {
            get
            {
                var pathSource = _allVueUploadFolder;
                var fullPath = Path.Combine(pathSource, FileHelpers.SanitizePath(_productContext.ShortCode),
                    FileHelpers.SanitizePath(_productContext.SubProductId), "responseWeighting");

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                return fullPath;
            }
        }

        private string LocalPath(string path)
        {
            var fullPath = Path.Combine(FileHelpers.SanitizePath(_productContext.ShortCode), FileHelpers.SanitizePath(_productContext.SubProductId), "responseWeighting", path);

            return fullPath;
        }

        public string GetFullFileName(string path)
        {
            var rootOfPath = PhysicalRoot.ToLower();
            var fullPath = Path.GetFullPath(Path.Combine(rootOfPath, path));

            if (!fullPath.StartsWith(rootOfPath))
            {
                throw new FileNotFoundException(path);
            }
            return fullPath;
        }

        public IFileInfo GetFileInfo(string fileName)
        {
            var fileProvider = new PhysicalFileProvider(_allVueUploadFolder);
            var localFileName = LocalPath(fileName.Replace("/", "\\"));
            return fileProvider.GetFileInfo(localFileName);
        }
    }
}
