using System;
using System.Collections.Generic;

namespace VueReporting
{
    public interface IAppSettings
    {
        string Root { get; }
        string ProductName { get; }
        string UserName { get; }
        string ProductFilter { get; }
        string ProductDescription { get; }
        string[] ExcludedFilters { get; set; }
        string[] RemoveFilters { get; }
        string ReportingApiAccessToken { get; set; }
    }
}