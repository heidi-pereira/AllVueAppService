using System;

namespace VueReporting.Services
{
    public interface IReportParameterManipulator
    {
        Uri UpdateUrl(Uri uri, DateTimeOffset reportDate, string productFilter, string root);
        string Name { get; }
        bool ReplaceTokens(string source, DateTimeOffset reportDate, out string textWithReplacedTokens);
    }
}