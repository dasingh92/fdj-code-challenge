using System;

namespace FdjCodeChallenge.Api.Utilities;

public class ConfigurationSingleton
{
    private static Lazy<IConfiguration>? _instance = null;

    public static IConfiguration Instance
    {
        get
        {
            if(_instance == null)
            {
                var builder = GetConfigurationBuilder();
                _instance = new Lazy<IConfiguration>(() => builder.Build());
            }
            return _instance.Value;
        }
    }

    protected static IConfigurationBuilder GetConfigurationBuilder()
    {
        // In a real application might want to do this differently, but for the sake of this challenge we can assume that the configuration files are in the same directory as the application and that the environment variable is set correctly.
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder;
    }

}
