using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenEnds.BackEnd.Library;

/// <summary>
/// For Migrations only
/// </summary>

// ReSharper disable once UnusedMember.Global
public class OpenEndsContextFactory : IDesignTimeDbContextFactory<OpenEndsContext>
{
    public OpenEndsContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenEndsContext>();

        Console.WriteLine("Getting connection string from BackEnd functions.");

        var relativePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\..\Clientopenends.BackEnd");

        Console.WriteLine("Path to config file: " + relativePath);

        var config = new ConfigurationBuilder()
            .SetBasePath(relativePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var settings = new Settings();
        config.GetRequiredSection("Settings").Bind(settings);

        Console.WriteLine("Connection string: "+ settings.SurveyConnectionString);

        optionsBuilder.UseSqlServer(settings.SurveyConnectionString);

        return new OpenEndsContext(optionsBuilder.Options);
    }
}