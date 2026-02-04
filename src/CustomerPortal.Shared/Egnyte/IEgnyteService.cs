using System;
using System.Threading.Tasks;
using Egnyte.Api;
using Egnyte.Api.Files;

namespace CustomerPortal.Shared.Egnyte
{
    public interface IEgnyteService
    {
        Task<T> ExecuteEgnyteCall<T>(Func<EgnyteClient, Task<T>> action);

        Task<FolderExtendedMetadata> GetEgnyteFolder(string folderPath);
        string EgnyteDomain { get; }
    }
}