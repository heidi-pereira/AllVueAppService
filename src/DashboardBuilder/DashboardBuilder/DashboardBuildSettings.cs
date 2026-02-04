using System.IO;

namespace DashboardBuilder
{

    internal class DashboardBuildSettings
    {
        public DashboardBuildSettings(string egnyteReadOnlyRoot, string mapFilePath)
        {
            SourceFolder = Path.GetDirectoryName(mapFilePath);
            SourceFolderRelativeToEgnyteDashboards = Path.GetFileName(SourceFolder);
            BaseFolder = Path.Combine(egnyteReadOnlyRoot, BaseFolderRelativeToEgynteDashboards);
            MapFilePath = mapFilePath;
        }

        public static string BaseFolderRelativeToEgynteDashboards { get; } = "_Base";
        public string BaseFolder { get; }
        public string SourceFolder { get; }
        public string SourceFolderRelativeToEgnyteDashboards { get; }
        public string MapFilePath { get; }
    }
}