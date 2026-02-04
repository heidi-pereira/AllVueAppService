using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Egnyte.Api;
using Egnyte.Api.Common;
using Egnyte.Api.Files;
using Microsoft.Extensions.Logging;

namespace EgnyteClient
{
    public class FolderClient
    {
        private readonly ILogger _logger;
        public FileClient Files { get; }

        public FolderClient(IEnumerable<string> egnyteBearerTokens, string egnyteSubdomain, ILoggerFactory loggerFactory)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _logger = loggerFactory?.CreateLogger<FolderClient>();
            Files = new FileClient(egnyteBearerTokens, egnyteSubdomain, loggerFactory);
        }

        /// <summary>
        /// Process map files last since they can fail validation but may depend on other files
        /// </summary>
        private static IOrderedEnumerable<FileBasicMetadata> InDependencyOrder(IEnumerable<FileBasicMetadata> files)
        {
            return files.OrderBy(f => f.Name.Equals("Map.xlsx", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Unused by code - this is a convenience method for getting a new token
        /// Needs a developer API key for the domain - then username/password to make a token for access
        /// </summary>
        public static async Task<string> WriteBearerTokenToConsoleAsync(string username, string password, string egnyteSubdomain, string apiKey)
        {
            var tokenResponse = await EgnyteClientHelper.GetTokenResourceOwnerFlow(egnyteSubdomain, apiKey, username, password);
            Console.WriteLine($"BearerToken: {tokenResponse.AccessToken}");
            return tokenResponse.AccessToken;
        }

        /// <param name="egnyteFolderPath">e.g. Shared/Systems/Dashboards/WGSN</param>
        /// <param name="destinationDirectory">The destination directory needn't exist, but beware any existing contents will be removed/overwritten</param>
        /// <param name="downloadOptions"></param>
        /// <returns>true iff filesystem changes *within* <paramref name="destinationDirectory"/> as a result of calling this method</returns>
        /// <remarks>The destination directory being created is not regarded as a filesystem change *within* that directory. Skipped files are also deleted if not protected by <paramref name="downloadOptions.DeleteIfNotInEgnyte"/>.</remarks>
        public async Task<bool> DownloadFolder(string egnyteFolderPath, DirectoryInfo destinationDirectory, DownloadOptions downloadOptions)
        {
            _logger.LogInformation($"Downloading {egnyteFolderPath} to {destinationDirectory} with recursion depth {downloadOptions.RecurseDepth}");

            try
            {
                return await DownloadFolderInner(egnyteFolderPath, destinationDirectory, downloadOptions);
            }
            catch (EgnyteApiException e) when (e.Message.Contains("Not found"))
            {
                _logger.LogWarning("Folder not found", e);
                throw new DirectoryNotFoundException(egnyteFolderPath, e);
            }

        }

        private async Task<bool> DownloadFolderInner(string egnyteFolderPath, DirectoryInfo destinationDirectory, DownloadOptions downloadOptions)
        {
            destinationDirectory.Create();

            bool updatedFilesystem = false;
            if (Files.IsValidClient)
            {

                var (folders, files) =
                    await Files.GetEgnyteFilesAndFolders(egnyteFolderPath, downloadOptions.SkipEgnytePath);


                updatedFilesystem |= DeleteIfNotInEgnyte(destinationDirectory, (folders, files),
                    downloadOptions.DeleteIfNotInEgnyte);

                foreach (var fileMetadata in InDependencyOrder(files))
                {

                    string destinationFilePath = GetDestinationPath(destinationDirectory, fileMetadata);
                    updatedFilesystem |= await Files.DownloadFile(fileMetadata, destinationFilePath,
                        downloadOptions.ShouldKeepNewFile);
                }


                foreach (var folderMetadata in folders)
                {
                    if (downloadOptions.RecurseDepth > 0)
                    {
                        updatedFilesystem |= await DownloadFolder(folderMetadata.Path,
                            new DirectoryInfo(GetDestinationPath(destinationDirectory, folderMetadata)),
                            new DownloadOptions(downloadOptions.SkipEgnytePath, downloadOptions.DeleteIfNotInEgnyte,
                                downloadOptions.ShouldKeepNewFile, downloadOptions.RecurseDepth - 1));
                    }
                    else
                    {
                        _logger.LogInformation($"Skipping {folderMetadata.Path} because it's beyond the recursion depth");
                    }
                }
            }

            return updatedFilesystem;
        }

        private bool DeleteIfNotInEgnyte(DirectoryInfo destinationDirectory, (List<FolderMetadata> Folders, List<FileBasicMetadata> Files) egnyteFilesAndFolders, Func<string, bool> deleteIfNotInEgnyte)
        {
            var egnyteFileDestinations = new HashSet<string>(egnyteFilesAndFolders.Files.Select(file => GetDestinationPath(destinationDirectory, file)), StringComparer.OrdinalIgnoreCase);
            var egnyteFolderDestinations = new HashSet<string>(egnyteFilesAndFolders.Folders.Select(folder => GetDestinationPath(destinationDirectory, folder)), StringComparer.OrdinalIgnoreCase);

            var diskFilesAndFolders = destinationDirectory.GetFileSystemInfos();

            var filesAndFoldersToDelete = diskFilesAndFolders.Where(fileOrFolderOnDisk =>
                fileOrFolderOnDisk is FileInfo _ && !egnyteFileDestinations.Contains(fileOrFolderOnDisk.FullName) && deleteIfNotInEgnyte(fileOrFolderOnDisk.FullName)
                || fileOrFolderOnDisk is DirectoryInfo _ && !egnyteFolderDestinations.Contains(fileOrFolderOnDisk.FullName) && deleteIfNotInEgnyte(fileOrFolderOnDisk.FullName))
                .ToList();
            foreach (var fileOrFolderOnDisk in filesAndFoldersToDelete)
            {
                _logger.LogInformation($"Deleting {fileOrFolderOnDisk.FullName} because it's not in Egnyte");
                if (fileOrFolderOnDisk is DirectoryInfo di) di.Delete(true);
                else fileOrFolderOnDisk.Delete();
            }

            return filesAndFoldersToDelete.Any();
        }
        private static string GetDestinationPath(DirectoryInfo destinationDirectory, FolderMetadata f)
        {
            return Path.Combine(destinationDirectory.FullName, f.Name);
        }

        private static string GetDestinationPath(DirectoryInfo destinationDirectory, FileBasicMetadata f)
        {
            return Path.Combine(destinationDirectory.FullName, f.Name);
        }
    }
}
