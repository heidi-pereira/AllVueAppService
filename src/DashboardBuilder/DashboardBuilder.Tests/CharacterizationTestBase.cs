using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    public class CharacterizationTestBase
    {
        protected const string ActualOutputFolderName = "ActualOutput";
        protected const string TestOutputFolderName = "TestData";
        protected static readonly string AssemblyDir = Path.GetDirectoryName(new Uri(typeof(CharacterizationTestBase).Assembly.Location).AbsolutePath);

        /// <summary>
        /// Set to true and run the test to overwrite the repository files with the actual output - make sure to check any changes, and revert this setting before committing
        /// </summary>
        protected static readonly bool _OverwriteExpectedWithActual = false;

        protected void AssertCharacterizationMatches(string outputDirPath, string searchPattern, SearchOption searchOption)
        {
            var actualFilePaths = Directory.GetFiles(outputDirPath, searchPattern, searchOption);
            Assert.That(actualFilePaths, Has.Length.GreaterThan(0));
            foreach (var actualFilePath in actualFilePaths)
            {
                var expectedFilePath = actualFilePath.Replace(ActualOutputFolderName, TestOutputFolderName);
                Assert.That(actualFilePath, Is.Not.EqualTo(expectedFilePath),
                    $"Test setup issue: Actual file path must contain '{ActualOutputFolderName}'");
                AssertFileContentsEqual(actualFilePath, expectedFilePath);
            }

            Assert.That(_OverwriteExpectedWithActual, Is.False,
                $"Test setup issue: Disable {nameof(_OverwriteExpectedWithActual)} before committing");
        }

        private void AssertFileContentsEqual(string actualFilePath, string expectedFilePath)
        {
            try
            {
                Assert.That(File.ReadAllText(actualFilePath, Encoding.UTF8).Replace("\r\n", "\n"), Is.EqualTo(File.ReadAllText(expectedFilePath, Encoding.UTF8).Replace("\r\n", "\n")), $"Difference in {Path.GetFileName(actualFilePath)}");
            }
            catch (Exception e) when (_OverwriteExpectedWithActual)
            {
                var expectedFileSourcePath = expectedFilePath.ToLower().Replace(AssemblyDir.ToLower(), AssemblyDir.ToLower() + @"\..\..\");
                Console.WriteLine($"Overwriting expected output at {expectedFileSourcePath} due to difference: {e}");
                Console.WriteLine();
                Assert.That(expectedFileSourcePath.ToLower(), Is.Not.EqualTo(actualFilePath.ToLower()),
                    "Test setup issue: Can't calculate expectedFileSourcePath");

                Directory.CreateDirectory(Path.GetDirectoryName(expectedFileSourcePath));
                File.WriteAllText(expectedFileSourcePath, File.ReadAllText(actualFilePath, Encoding.UTF8), Encoding.UTF8);
            }
        }
    }
}