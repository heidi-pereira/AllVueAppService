namespace DashboardBuilder.Core
{
    internal interface IAppSettings
    {
        string EgnyteReadOnlyRoot { get; }
        string OverrideOutputPath { get; }
        bool PackageOutput { get; }
    }
}