using System;
using System.Collections.Generic;
using System.IO;
using DashboardBuilder.Core;
using DashboardMetadataBuilder;

namespace DashboardBuilder.Helper
{
    class MapFileLocator
    {
        /// <summary>
        /// Locate map files in the specified root folder and sub-folders and return their paths as a collection of strings.
        /// Use the .vueinclude file to include/exclude files.
        /// </summary>
        /// <param name="rootPath">Root folder for search</param>
        /// <returns></returns>
        public static IEnumerable<String> LocateMapFiles(string rootPath)
        {
            var filePathMatcher = new FilePathMatcher(Path.Combine(rootPath, DashboardMetadataUpdater.EgnyteDashboardsIgnoreFile));
            filePathMatcher.AddRule($"!{DashboardBuildSettings.BaseFolderRelativeToEgynteDashboards}/");

            foreach (var directory in new DirectoryInfo(rootPath).EnumerateDirectories("*.*"))
            {
                if (filePathMatcher.HasMatch(directory.Name))
                {
                    var mapFile = MapSettings.GetMapFilePath(directory.FullName);
                    if (File.Exists(mapFile))
                    {
                        yield return mapFile;
                    }
                }
            }
        }
    }
}
