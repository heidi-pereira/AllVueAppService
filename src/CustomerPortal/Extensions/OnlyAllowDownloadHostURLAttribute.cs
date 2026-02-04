using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace CustomerPortal.Extensions
{
    public sealed class OnlyAllowDownloadHostUrlAttribute : Attribute, IHostMetadata
    {
        static string _downloadHost;
        public OnlyAllowDownloadHostUrlAttribute()
        {
            if (string.IsNullOrEmpty(_downloadHost))
            {
                try
                {
                    var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
                    _downloadHost = config["AppSettings:DataDownloadDomain"];
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to read appsettings.json or AppSettings:DataDownloadDomain - {e.Message}", e);
                }
                if (string.IsNullOrEmpty(_downloadHost))
                {
                    throw new Exception("AppSettings:DataDownloadDomain is set to empty, this is not allowed");
                }
            }
            Hosts = new[] { _downloadHost };
        }
        public IReadOnlyList<string> Hosts { get; }
    }
}
