using BrandVue.Controllers.Api;
using BrandVue.EntityFramework;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using static BrandVue.Controllers.Api.FileHelpers;
using System.Xml;
using Microsoft.AspNetCore.Http;

namespace BrandVue.Services.ReportVue
{
    public interface IAllVueWebPageService
    {
        Task<string> UploadFile(string path, IFormFile file);
        IEnumerable<AllVueWebPageService.WebFileFileInformation> GetFiles(string root);
        void DeleteFile(string name);
    }

    public class AllVueWebPageService : IAllVueWebPageService
    {
        private readonly IProductContext _productContext;
        private readonly ILogger<AllVueWebPageService> _logger;
        private readonly string _allVueUploadFolder;

        public AllVueWebPageService(IProductContext productContext, ILogger<AllVueWebPageService> logger, AppSettings appSettings)
        {
            _productContext = productContext;
            _logger = logger;
            _allVueUploadFolder = appSettings.AllVueUploadFolder;
        }

        public async Task<string> UploadFile(string path, IFormFile file)
        {
            var smallSizeLimitBytes = 1 * 1024 * 1024;

            var allowedFileTypes = new[] {
                new FilesExtensionWithMaxFileSize(".png", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".txt", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".html", smallSizeLimitBytes ),
                new FilesExtensionWithMaxFileSize(".svg", smallSizeLimitBytes ),
            };

            var (fileBytes, sanitizedFileName, error) = await FileHelpers.ProcessFormFile(file, allowedFileTypes);

            if (!IsPathRootedWithinSandbox(path, null, out var fullPath))
            {
                return "Requested to create a file outside of sandboxed area";
            }

            if (error != null)
            {
                return (error);
            }

            try
            {
                if (!Path.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                var fullFileName = Path.Combine(fullPath, sanitizedFileName);
                if (File.Exists(fullFileName))
                {
                    return $"File {sanitizedFileName} already exists.";
                }

                File.WriteAllBytes(fullFileName, ReplacePaths(path, fileBytes));
                return string.Empty;
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception occurred trying to upload file");
                return "An error ocurred trying to upload this file. Please try again.";
            }
        }

        byte[] ReplacePaths(string path, byte[] input)
        {
            try
            {
                using var inputStream = new MemoryStream(input);
                using XmlTextReader textReader = new XmlTextReader(inputStream);
                XmlDocument doc = new XmlDocument();
                doc.Load(textReader);

                foreach (XmlNode imgTag in doc.SelectNodes("//img"))
                {
                    var srcAttrib = imgTag.Attributes["src"];
                    if (srcAttrib != null)
                    {
                        var imgSource = imgTag.Attributes["src"].Value;
                        bool isFile = true;
                        try
                        {
                            var uri = new UriBuilder(imgSource);
                            isFile = uri.Scheme == "file";
                        }
                        catch (Exception )
                        {
                        }

                        if (isFile)
                        {
                            imgTag.Attributes["src"].Value =
                                $"{UrlToFile(Path.Combine(path, Path.GetFileName(imgTag.Attributes["src"].Value)))}";
                        }
                    }
                }

                

                using var stream = new MemoryStream();
                using var textWriter = new XmlTextWriter(stream, Encoding.ASCII);
                doc.WriteTo(textWriter);
                textWriter.Flush();
                textWriter.Close();
                return stream.ToArray();

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to parse HTML file");
                return (input);
            }
        }

        private string UrlToFile(string path)
        {
            var fullPath = Path.Combine(_productContext.SubProductId, "reportvue", "WebPageContent", path);
            return fullPath;
        }

        private string FullWebPath()
        {
            var pathSource = _allVueUploadFolder;
            var fullPath = Path.Combine(pathSource, FileHelpers.SanitizePath(_productContext.ShortCode), FileHelpers.SanitizePath(_productContext.SubProductId), "WebPageContent");

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        private string FullWebPath(string root)
        {
            var fullPath = Path.Combine(FullWebPath(), root);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        private bool IsPathRootedWithinSandbox(string path, string fileName, out string fullPath)
        {
            var rootOfPath = FullWebPath(path).ToLower();
            fullPath = fileName == null ? Path.GetFullPath(rootOfPath) : Path.GetFullPath(Path.Combine(rootOfPath, fileName));

            return fullPath.StartsWith(rootOfPath);
        }

        public record WebFileFileInformation(string name, string url, long? size, DateTime? LastModified);

        public IEnumerable<WebFileFileInformation> GetFiles(string root)
        {
            if (!IsPathRootedWithinSandbox(root, null, out var fullPath))
            {
                throw new Exception("Requested to list files outside of sandboxed area");
            }
            var files = new List<WebFileFileInformation>();
            foreach (var item in Directory.GetFiles(fullPath, "*.*"))
            {
                var fileName = item.Substring(FullWebPath().Length + 1);
                {
                    var fileItem = new FileInfo(item);

                    files.Add(
                        new WebFileFileInformation(fileName,
                            UrlToFile(fileName),
                            fileItem.Length,
                            fileItem.LastWriteTimeUtc));
                }
            }
            return files;
        }

        public void DeleteFile(string name)
        {
            if (!IsPathRootedWithinSandbox("", name, out var fullFileName))
            {
                throw new Exception($"Attempting to delete file outside of sandboxed area");
            }

            if (Directory.Exists(fullFileName))
            {
                Directory.Delete(fullFileName, true);
            }
            else if (System.IO.File.Exists(fullFileName))
            {
                System.IO.File.Delete(fullFileName);
            }
            else
            {
                throw new Exception($"File '{name}' does not exist.");
            }
        }
    }
}
