using System.IO;
using System.Runtime.CompilerServices;

namespace DashboardBuilder.Tests
{
    public static class TempPath
    {
        public static DirectoryInfo CreateTraceableDirectory([CallerFilePath] string traceableInfo = "")
        {
            if (traceableInfo.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                traceableInfo = Path.GetFileName(traceableInfo);
            }
            var temporaryPath = Path.Combine(Path.GetTempPath(), traceableInfo, Path.GetRandomFileName());
            var directoryInfo = new DirectoryInfo(temporaryPath);
            directoryInfo.Create();
            return directoryInfo;
        }

        public static FileInfo CreateFile(this DirectoryInfo parentDirectory, string relativeFilename, string contents = "file contents")
        {
            var filePath = Path.Combine(parentDirectory.FullName, relativeFilename);
            var fileInfo = new FileInfo(filePath);
            fileInfo.Directory.Create();
            File.WriteAllText(filePath, contents);
            return fileInfo;
        }

        public static bool DeleteIfExists(this DirectoryInfo dir, bool recursive)
        {
            if (dir.Exists)
            {
                dir.Delete(recursive);
                return true;
            }
            return false;
        }
    }
}