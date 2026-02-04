using System;
using System.IO;
using System.Threading.Tasks;

namespace EgnyteClient
{
    public class DownloadOptions
    {
        private const int MaxRecursionDepth = 4;

        /// <param name="skipEgnytePath"></param>
        /// <param name="deleteIfNotInEgnyte"></param>
        /// <param name="shouldKeepNewFile">Returns true iff for the given path containing new data, the old file should be overwritten</param>
        /// <param name="recurseDepth">Must be less than 3, since when I set it to infinite, it ended up downloading a node_modules directory nested away inside the one I wanted and hitting the API quota limit</param>
        public DownloadOptions(Func<string, bool> skipEgnytePath, Func<string, bool> deleteIfNotInEgnyte, Func<string, string, DateTime, Task<bool>> shouldKeepNewFile = null, uint recurseDepth = 0)
        {
            if (recurseDepth >= MaxRecursionDepth)
                throw new ArgumentOutOfRangeException(nameof(recurseDepth), recurseDepth,
                    $"{nameof(recurseDepth)} should be less than {MaxRecursionDepth} to avoid hitting API quota limit");

            SkipEgnytePath = skipEgnytePath;
            DeleteIfNotInEgnyte = deleteIfNotInEgnyte;
            ShouldKeepNewFile = shouldKeepNewFile;
            RecurseDepth = recurseDepth;
        }

        public Func<string, bool> SkipEgnytePath { get; }
        public Func<string, bool> DeleteIfNotInEgnyte { get; }
        public uint RecurseDepth { get; }
        public Func<string, string, DateTime, Task<bool>> ShouldKeepNewFile { get; }
    }
}