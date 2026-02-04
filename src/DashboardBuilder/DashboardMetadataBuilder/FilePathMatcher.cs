using System;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;

namespace DashboardMetadataBuilder
{
    public class FilePathMatcher
    {
        private readonly Matcher _fileSystemGlobbingMatcher;

        public FilePathMatcher(string fileName) : this()
        {
            LoadFile(fileName);
        }
        public FilePathMatcher()
        {
            _fileSystemGlobbingMatcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        }

        private void LoadFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                using (var file = new FileStream(fileName, FileMode.Open))
                {
                    using (var streamReader = new StreamReader(file))
                    {
                        LoadStream(streamReader);
                    }
                }
            }
            else
            {
                AddRule("*");
            }
        }

        public void LoadFile(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            using (var streamReader = new StreamReader(memoryStream))
            {
                LoadStream(streamReader);
            }
        }

        private void LoadStream(StreamReader streamReader)
        {
            while (!streamReader.EndOfStream)
            {
                var rule = streamReader.ReadLine();
                if (string.IsNullOrWhiteSpace(rule) || rule.StartsWith("//"))
                {
                    continue;
                }
                AddRule(rule);
                if (rule.EndsWith("/**"))
                {
                    // Match the directory itself having just setup match for all of its contents
                    AddRule(rule.Substring(0, rule.Length - 3));
                }
            }
        }

        public void AddRule(string rule)
        {
            var excludeCharacter = "!";
            var isExclude = rule.StartsWith(excludeCharacter);

            try
            {
                if (isExclude)
                {
                    rule = rule.Substring(excludeCharacter.Length);
                    _fileSystemGlobbingMatcher.AddExclude(rule);
                }
                else
                {
                    _fileSystemGlobbingMatcher.AddInclude(rule);
                }
            }
            catch (Exception e)
            {
                var ruleType = isExclude ? "exclude" : "include";
                throw new Exception($"Unable to add {ruleType} rule `{rule}`", e);
            }
        }

        public bool HasMatch(string relativePath)
        {
            return _fileSystemGlobbingMatcher.Execute(new InMemoryDirectoryInfo(@".", new[] { relativePath })).HasMatches;
        }
    }
}