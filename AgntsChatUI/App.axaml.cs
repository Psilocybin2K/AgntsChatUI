namespace AgntsChatUI
{
    using System.Linq;

    using AgntsChatUI.Services;
    using AgntsChatUI.ViewModels;
    using AgntsChatUI.Views;

    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Data.Core.Plugins;
    using Avalonia.Markup.Xaml;
    using Microsoft.Extensions.DependencyInjection;
    using System;

    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set up dependency injection
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                this.DisableAvaloniaDataAnnotationValidation();
                
                var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IAgentRepository, SqliteAgentRepository>();
            services.AddSingleton<IAgentService, AgentService>();
            
            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddTransient<DocumentManagementViewModel>();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (DataAnnotationsValidationPlugin? plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}