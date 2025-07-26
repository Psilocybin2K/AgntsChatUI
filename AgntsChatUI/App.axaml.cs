namespace AgntsChatUI
{
    using System;
    using System.Linq;
    using System.Net.Http; // Add this using directive
    using System.Threading.Tasks;

    using AgntsChatUI.Services;
    using AgntsChatUI.ViewModels;
    using AgntsChatUI.Views;
    using AgntsChatUI.ServiceDefaults; // Add this for Aspire extension methods

    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Data.Core.Plugins;
    using Avalonia.Markup.Xaml;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting; // Add this using directive
                                        // Or, if AddAppDefaults is in your ServiceDefaults project,
                                        // ensure that project exposes it in a public static class.
    using Microsoft.Extensions.Logging; // Add for logging


    public partial class App : Application
    {
        private IHost? _host; // Change to IHost to manage lifetime and services

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted() // Make async
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Create a HostApplicationBuilder for Aspire integration
                HostApplicationBuilder builder = Host.CreateApplicationBuilder();

                // -------------------------------------------------------------
                // Aspire Integration Starts Here
                // -------------------------------------------------------------

                builder.AddServiceDefaults(); // Now works due to correct using

                // Configure services specific to your Avalonia application
                this.ConfigureServices(builder.Services);

                // Add HttpClient for service communication with Aspire service discovery
                builder.Services.AddHttpClient<MyBackendApiService>((serviceProvider, httpClient) =>
                {
                    httpClient.BaseAddress = new Uri("http://apiservice");
                });

                // -------------------------------------------------------------
                // Aspire Integration Ends Here
                // -------------------------------------------------------------

                this._host = builder.Build();

                // Add a startup log
                var loggerFactory = this._host.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<App>();
                logger.LogInformation("Aspire-enabled Avalonia app started.");

                // Start the host (important for background services, logging, etc.)
                await this._host.StartAsync(); // Await host start

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                this.DisableAvaloniaDataAnnotationValidation();

                // Get MainWindowViewModel from the host's service provider
                MainWindowViewModel mainWindowViewModel = this._host.Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };

                // Register for shutdown event to dispose host
                desktop.Exit += async (_, __) =>
                {
                    if (this._host != null)
                    {
                        await this._host.StopAsync();
                        this._host.Dispose();
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        // Remove OnExit override (not valid in Avalonia)

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IAgentRepository, SqliteAgentRepository>();
            services.AddSingleton<IAgentService, AgentService>();
            services.AddSingleton<IFileTemplateService, FileTemplateService>();
            services.AddSingleton<PlaywrightManagementService>();

            // Register ViewModels
            services.AddTransient<DocumentManagementViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddTransient<AgentManagementViewModel>();
            services.AddTransient<MainWindowViewModel>();

            // Add your new API service client
            services.AddSingleton<MyBackendApiService>();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (DataAnnotationsValidationPlugin? plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }

    // Example API service client - you will replace this with your actual API calls
    public class MyBackendApiService
    {
        private readonly HttpClient _httpClient;

        public MyBackendApiService(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<string> GetWeatherDataAsync()
        {
            // This will call the "apiservice" (e.g., your ASP.NET Core API)
            // Ensure your API has a /weatherforecast endpoint for this example
            try
            {
                return await this._httpClient.GetStringAsync("/weatherforecast");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error calling API: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}