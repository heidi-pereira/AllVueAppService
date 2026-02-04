using System.IO;

namespace BrandVue.SourceData.Utils
{
    public static class PathExtensions
    {
        /// <remarks>https://stackoverflow.com/a/23182807/1128762</remarks>
        public static string ReplaceInvalidFilenameCharacters(this string filename, string replacement = "")
        {
            return string.Join(replacement, filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}