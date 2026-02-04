using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EgnyteClient
{
    public class Sha512Hash
    {
        public string FromBytes(byte[] bytes)
        {
            return Hash(sha => sha.ComputeHash(bytes));
        }

        public string FromFilePath(string destinationFilePath)
        {
            using (var fileStream = new FileStream(destinationFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Hash(sha => sha.ComputeHash(fileStream));
            }
        }

        private string Hash(Func<SHA512, byte[]> computeHash)
        {
            using (var sha = SHA512.Create())
            {
                var hash = computeHash(sha);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}