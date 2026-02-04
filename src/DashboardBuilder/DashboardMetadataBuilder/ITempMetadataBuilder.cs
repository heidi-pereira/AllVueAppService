using System.IO;
using System.Threading.Tasks;

namespace DashboardMetadataBuilder
{
    public interface ITempMetadataBuilder
    {
        Task BuildToTempMetadataFolder(string mapFilePath, DirectoryInfo outputPath);
        bool IsBrandVue(string mapFilePath);
    }
}