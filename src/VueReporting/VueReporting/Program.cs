using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Savanta.Logging;

namespace VueReporting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var loggerFactory = SavantaLogging.CreateFactory();
            await QueueSystem.InitializeAsync(loggerFactory);
            var webHost = BuildWebHost(args);
            await webHost.RunAsync();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
