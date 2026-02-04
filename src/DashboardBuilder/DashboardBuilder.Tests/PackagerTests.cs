using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octopus.Client.Repositories.Async;

namespace DashboardBuilder.Tests
{
    public class PackagerTests
    {
        private DirectoryInfo _tempLocalDirectory;

        [SetUp]
        public void CreateTempDirectory()
        {
            _tempLocalDirectory = TempPath.CreateTraceableDirectory();
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            _tempLocalDirectory.DeleteIfExists(true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CreatesAndDeletesZipFile(bool shouldDeleteAfterPush)
        {
            _tempLocalDirectory.CreateFile("data/data/dat.csv");
            _tempLocalDirectory.CreateFile("metadata/config/dashPanes.csv");
            ConfigurationManager.AppSettings["Packager.ShouldDeleteAfterPush"] = shouldDeleteAfterPush.ToString();

            var octopusRepository = Substitute.For<IBuiltInPackageRepositoryRepository>();
            octopusRepository.PushPackage(Arg.Any<string>(), Arg.Is<FileStream>(s => AssertNoTopLevelFiles(s)), true)
                .Throws(new AssertionException("The top level of the zip file should only contain directories"));

            await new Packager("theVersionISet", Substitute.For<ILoggerFactory>(), octopusRepository).CreateAndPush(_tempLocalDirectory, false);
            
            await octopusRepository.Received(1).PushPackage(Arg.Is<string>(s => s.Contains("metadata.theVersionISet")), Arg.Any<FileStream>(), true);
            Assert.That(!_tempLocalDirectory.Exists, Is.EqualTo(shouldDeleteAfterPush));
        }

        public bool AssertNoTopLevelFiles(FileStream zipStream)
        {
            var files = new ZipArchive(zipStream, ZipArchiveMode.Read).Entries.Select(e => e.FullName.Split('/')[0])
                .Where(n => n.Contains(".")).ToList();
            Assert.That(files, Is.Empty);
            return files.Any();
        }
    }
}
