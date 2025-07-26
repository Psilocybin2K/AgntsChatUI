namespace AgntsChatUI
{
    using System;
    using System.Linq;

    using AgntsChatUI.Services;
    using AgntsChatUI.ViewModels;
    using AgntsChatUI.Views;

    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Data.Core.Plugins;
    using Avalonia.Markup.Xaml;

    using Microsoft.Extensions.DependencyInjection;

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
                ServiceCollection services = new ServiceCollection();
                this.ConfigureServices(services);
                this._serviceProvider = services.BuildServiceProvider();

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                this.DisableAvaloniaDataAnnotationValidation();

                MainWindowViewModel mainWindowViewModel = this._serviceProvider.GetRequiredService<MainWindowViewModel>();
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
            services.AddSingleton<IFileTemplateService, FileTemplateService>();

            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddTransient<DocumentManagementViewModel>();
            services.AddTransient<AgentManagementViewModel>();
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