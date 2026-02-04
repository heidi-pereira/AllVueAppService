using System;
using System.Collections.Specialized;
using System.Configuration;

namespace DashboardBuilder.Core
{
    internal class AppSettings : IAppSettings
    {
        private static readonly NameValueCollection _settingsCollection = ConfigurationManager.AppSettings;

        public string EgnyteReadOnlyRoot { get; } =  _settingsCollection.GetTrimmedString("Egnyte.Dashboards.LocalReadOnly");
        public string OverrideOutputPath { get; } = _settingsCollection.GetTrimmedString("OverrideOutputPath");
        public bool PackageOutput { get; } = _settingsCollection.GetBool(nameof(PackageOutput));
    }
}