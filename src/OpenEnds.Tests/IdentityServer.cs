using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OpenEnds.Tests
{
    [TestFixture]
    internal class IdentityServer
    {
        [Test]
        public void GenerateHash()
        {
            using var sha = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes("9ab9d2976485447192534feb868582b8");
            var hash = sha.ComputeHash(bytes);

            var hashString = Convert.ToBase64String(hash);

            Console.Write(hashString);
        }
    }
}
