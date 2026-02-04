using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace DashboardMetadataBuilder.MapProcessing
{
    public class MetadataFileProcessor
    {
        public static void ProcessAssets(string sourceFolder, string baseFolder, string destinationFolder)
        {
            ProcessPagesFolder(sourceFolder, destinationFolder);
            ProcessWeightingsFolder(sourceFolder, destinationFolder);
            
            var destinationAssetsFolder = Path.Combine(destinationFolder, "assets");
            ProcessCssFolder(sourceFolder, destinationAssetsFolder, baseFolder);
            ProcessImagesFolder(sourceFolder, destinationAssetsFolder);
        }

        private static void ProcessPagesFolder(string sourcePath, string metadataFolder)
        {
            var sourceFolder = Path.Combine(sourcePath, "Pages");
            if (Directory.Exists(sourceFolder))
            {
                var destinationFolder = Path.Combine(metadataFolder, "pages");
                CopyOrCreateFolder(sourceFolder, destinationFolder);
            }
        }

        private static void ProcessWeightingsFolder(string sourcePath, string metadataFolder)
        {
            var sourceFolder = Path.Combine(sourcePath, "Weightings");
            if (Directory.Exists(sourceFolder))
            {
                var destinationFolder = Path.Combine(metadataFolder, "weightings");

                CopyOrCreateFolder(sourceFolder, destinationFolder);
            }
        }

        private static void ProcessCssFolder(string sourcePath, string assetsFolder, string baseFolder)
        {
            var sourceFolder = Path.Combine(sourcePath, "Css");
            if (Directory.Exists(sourceFolder))
            {
                var destinationFolder = Path.Combine(assetsFolder, "css");
                CopyOrCreateFolder(sourceFolder, destinationFolder);
                CopyFile(baseFolder, destinationFolder, "base.css");
            }
        }

        private static void ProcessImagesFolder(string sourcePath, string assetsFolder)
        {
            var sourceFolder = Path.Combine(sourcePath, "Images");
            if (Directory.Exists(sourceFolder))
            {
                var destinationFolder = Path.Combine(assetsFolder, "img");
                CopyOrCreateFolder(sourceFolder, destinationFolder);
                CopyFile(sourceFolder, assetsFolder, "favicon.ico");
            }
        }

        private static void CopyFile(string sourceFolder, string destinationFolder, string fileName)
        {
            var fileSource = Path.Combine(sourceFolder, fileName);
            var fileDestination = Path.Combine(destinationFolder, fileName);
            if (File.Exists(fileSource)) File.Copy(fileSource, fileDestination, true);
        }

        private static void CopyOrCreateFolder(string source, string destination)
        {
            if (Directory.Exists(source))
            {
                FileSystem.CopyDirectory(source, destination, true);
            }
            else
            {
                Directory.CreateDirectory(destination);
            }
        }
    }
}