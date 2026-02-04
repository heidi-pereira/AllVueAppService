using System.Threading;

namespace BrandVue.Services
{
    public interface IDataPreloader
    {
        DataPreloadTaskStatus PreloadReportDataIntoMemory(CancellationToken cancellationToken);
        DataPreloadTaskStatus CheckTaskStatus();
        void ClearTaskStatus();
        void CancelTask();
    }
}
