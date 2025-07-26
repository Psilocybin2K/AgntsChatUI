using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class Program
{
    // The following using may be required for the generated Projects class:
    // using Aspire;

    private static void Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

        // Register the Avalonia app as an Aspire project resource
        IResourceBuilder<ProjectResource> ui = builder.AddProject<Projects.AgntsChatUI>("agntschatui");

        // Logging
        Microsoft.Extensions.Logging.ILogger? logger = builder.Services.BuildServiceProvider().GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("AppHost");
        logger?.Log(LogLevel.Information, "Aspire AppHost started.");

        builder.Build().Run();
    }
}