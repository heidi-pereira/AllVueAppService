using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CustomerPortal.Utils
{
    // Taken from https://github.com/dotnet/AspNetCore.Docs/blob/e43694e1cdd2c29decd0d45cca5ff78d6b0e2506/aspnetcore/mvc/models/file-uploads/samples/3.x/SampleApp/Utilities/FileHelpers.cs
    // with some modifications
    public static class FileHelpers
    {
        // File signatures taken from https://www.garykessler.net/library/file_sigs.html
        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new Dictionary<string, List<byte[]>>
        {
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { ".ods", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".odt", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".odp", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".doc", new List<byte[]>
                {
                    new byte[] { 0xDB, 0xA5, 0x2D, 0x00 },
                    new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 },
                }
            },
            { ".xls", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
            { ".ppt", new List<byte[]>
                {
                    new byte[] { 0xA0, 0x46, 0x1D, 0xF0 },
                    new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 },
                }
            },
            { ".docx", new List<byte[]>
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 },
                }
            },
            { ".xlsx", new List<byte[]>
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 },
                    new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 }, //Aspose does it wrong
                }
            },
            { ".pptx", new List<byte[]>
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 },
                    new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 }, //Aspose does it wrong
                }
            },
            { ".sav", new List<byte[]>
                {
                    new byte[] { 0x24, 0x46, 0x4C, 0x32, 0x40, 0x28, 0x23, 0x29, 0x20, 0x53, 0x50, 0x53, 0x53, 0x20, 0x44, 0x41, 0x54, 0x41, 0x20, 0x46, 0x49, 0x4C, 0x45 },
                    new byte[] { 0x24, 0x46, 0x4C, 0x32, 0x40, 0x28, 0x23, 0x29, 0x20, 0x49, 0x42, 0x4D, 0x20, 0x53, 0x50, 0x53, 0x53, 0x20, 0x53, 0x54, 0x41, 0x54, 0x49, 0x53, 0x54, 0x49, 0x43, 0x53 }
                }
            },
        };

        //Files which when extension is renamed from value -> key are still functional (e.g. pptx can be renamed to ppt and it will still work even though its not a ppt)
        private static readonly Dictionary<string, string> _fileExtensionRemappings = new Dictionary<string, string>
        {
            { ".doc", ".docx" },
            { ".ppt", ".pptx" },
        };

        private static readonly string[] _reservedFileNameWords = new []
        {
            "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

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

        public static async Task<(byte[] File, string SanitizedFileName, string Error)> ProcessFormFile(IFormFile formFile, string[] permittedExtensions, long sizeLimit)
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

            if (formFile.Length > sizeLimit)
            {
                var megabyteSizeLimit = sizeLimit / 1048576;
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

                var fileName = formFile.FileName;
                var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
                var hasValidExtensionAndSignature = IsValidFileExtensionAndSignature(fileName, data, permittedExtensions);

                if (!hasValidExtensionAndSignature && _fileExtensionRemappings.ContainsKey(extension))
                {
                    //allow for some incorrect extensions which are still functional
                    var remappedExtension = _fileExtensionRemappings[extension];
                    fileName = OverrideFileExtension(fileName, remappedExtension);
                    hasValidExtensionAndSignature = IsValidFileExtensionAndSignature(fileName, data, permittedExtensions);
                }

                if (!hasValidExtensionAndSignature)
                {
                    return (Array.Empty<byte>(), null, "File type isn't permitted or the file's signature doesn't match the file's extension.");
                }
                else
                {
                    return (data, SanitizeFileName(fileName), null);
                }
            }
            catch (Exception)
            {
                return (Array.Empty<byte>(), null, "File upload failed");
            }
        }

        private static bool IsValidFileExtensionAndSignature(string fileName, byte[] data, string[] permittedExtensions)
        {
            if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
            {
                return false;
            }

            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                return false;
            }

            // Text files
            if (ext.Equals(".txt") || ext.Equals(".csv"))
            {
                // Limits characters to ASCII encoding.
                for (var i = 0; i < data.Length; i++)
                {
                    if (data[i] > sbyte.MaxValue)
                    {
                        return false;
                    }
                }
                return true;
            }

            // File signature check
            // --------------------
            // With the file signatures provided in the _fileSignature
            // dictionary, the following code tests the input content's
            // file signature.
            if (!_fileSignatures.ContainsKey(ext))
            {
                return false;
            }

            var signatures = _fileSignatures[ext];
            return signatures.Any(signature => data.Take(signature.Length).SequenceEqual(signature));
        }

        private static string OverrideFileExtension(string fileName, string extension)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
            return $"{Path.GetFileNameWithoutExtension(fileName)}{extension}";
        }
    }
}