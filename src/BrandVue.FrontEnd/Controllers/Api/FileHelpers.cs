using System.IO;
using Microsoft.AspNetCore.Http;

namespace BrandVue.Controllers.Api
{
    public class FileHelpers
    {
        private static readonly string[] _reservedFileNameWords = new[]
        {
            "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string SanitizePath(string path)
        {
            foreach (var item in Path.GetInvalidPathChars()) {
                path = path.Replace(item, '_');
            }
            return path;
        }
        public static string SanitizeFileName(string fileName)
        {
            // Files sent from client should have their names sanitized before usage
            var name = Path.GetFileNameWithoutExtension(fileName);
            // Assume extension was already validated in ProcessFormFile
            var extension = Path.GetExtension(fileName);

            // Alphanumeric only and replace reserved words
            var sanitizedName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_').ToArray());
            foreach (var reservedWord in _reservedFileNameWords)
            {
                sanitizedName = sanitizedName.Replace(reservedWord, "_", StringComparison.OrdinalIgnoreCase);
            }

            return $"{sanitizedName}{extension}";
        }

        public record FilesExtensionWithMaxFileSize(string Extension, int MaxSizeInBytes);

        public static async Task<(byte[] File, string SanitizedFileName, string Error)> ProcessFormFile(IFormFile formFile, FilesExtensionWithMaxFileSize[] permittedExtensions)
        {
            //must have a filename
            if (string.IsNullOrWhiteSpace(formFile.FileName))
            {
                return (Array.Empty<byte>(), null, "Invalid filename");
            }

            // FileNames should be shorter than 255 chars
            if (formFile.FileName.Length >= 255)
            {
                return (Array.Empty<byte>(), null, "Filename is too long.");
            }

            // Check the file length. This check doesn't catch files that only have
            // a BOM as their content.
            if (formFile.Length == 0)
            {
                return (Array.Empty<byte>(), null, "File is empty.");
            }
            var fileName = formFile.FileName;
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            var filesExtensionWithMaxFileSize = permittedExtensions.SingleOrDefault(x=> string.Compare(x.Extension,extension, true) == 0);


            if (filesExtensionWithMaxFileSize == null)
            {
                return (Array.Empty<byte>(), null, "File type isn't permitted.");
            }

            if (formFile.Length > filesExtensionWithMaxFileSize.MaxSizeInBytes)
            {
                var megabyteSizeLimit = filesExtensionWithMaxFileSize.MaxSizeInBytes / 1048576;
                return (Array.Empty<byte>(), null, $"File size exceeds {megabyteSizeLimit:N1} MB.");
            }

            try
            {
                byte[] data;
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);

                    // Check the content length in case the file's only
                    // content was a BOM and the content is actually
                    // empty after removing the BOM.
                    if (memoryStream.Length == 0)
                    {
                        return (Array.Empty<byte>(), null, "File is empty.");
                    }

                    data = memoryStream.ToArray();
                }
                //
                //Could now check if the file format looks good wrt to file extension
                //

                return (data, SanitizeFileName(fileName), null);
            }
            catch (Exception)
            {
                return (Array.Empty<byte>(), null, "File upload failed");
            }
        }
    }
}