using BrandVue.EntityFramework;
using System.IO;

namespace BrandVue.Services.ReportVue
{
    public class ReportVuePathTransformationService
    {
        private readonly IProductContext _productContext;
        const string keyWordPublished = "published";
        const string keyWordPublishedAndTrailingSlash = "published/";

        public ReportVuePathTransformationService(IProductContext productContext)
        {
            _productContext = productContext;
        }

        public string ConvertURLToDiskLayout(string fileName)
        {
            var lowercasedLogicalFileName = fileName.ToLowerInvariant();
            var actualFileName = Path.Combine(_productContext.ShortCode, _productContext.SubProductId, fileName);
            
            if (lowercasedLogicalFileName.StartsWith(keyWordPublishedAndTrailingSlash))
            {
                var reducedFileName = fileName.Substring(keyWordPublishedAndTrailingSlash.Length);
                actualFileName = Path.Combine(keyWordPublished, _productContext.ShortCode, _productContext.SubProductId, reducedFileName);
            }
            return actualFileName;
        }
    }
}
