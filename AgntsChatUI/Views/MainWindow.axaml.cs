namespace AgntsChatUI.Views
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    using AgntsChatUI.Services;
    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Threading;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;
            this.KeyDown += this.MainWindow_KeyDown;
            // Prepare for command palette logic
            // (actual show/hide logic will be handled via ViewModel binding)

            // Subscribe to CommandPalette close event
            Components.CommandPaletteComponent? commandPalette = this.FindControl<Views.Components.CommandPaletteComponent>("CommandPalette");
            if (commandPalette != null)
            {
                commandPalette.RequestClose += (s, e) =>
                {
                    if (this.DataContext is MainWindowViewModel vm)
                    {
                        vm.IsCommandPaletteVisible = false;
                    }
                };
            }
        }

        private async void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.Enter)
            {
                App? app = App.Current as App;
                if (app?.GetType().GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(app) is IServiceProvider provider)
                {
                    if (provider.GetService(typeof(PlaywrightManagementService)) is PlaywrightManagementService playwrightService)
                    {
                        await playwrightService.InitializeAsync();
                        // await ShowDialogAsync("Playwright browser launched.");
                    }
                }
            }

            if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.P)
            {
                if (this.DataContext is MainWindowViewModel vm)
                {
                    vm.IsCommandPaletteVisible = true;
                    // Focus the input box after the palette is visible
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (this.FindControl<Views.Components.CommandPaletteComponent>("CommandPalette") is { } palette)
                        {
                            TextBox? input = palette.FindControl<TextBox>("InputBox");
                            input?.Focus();
                        }
                    });
                }

                e.Handled = true;
            }
        }

        private async Task ShowDialogAsync(string message)
        {
            Window dialog = new Window
            {
                Width = 300,
                Height = 100,
                Content = new TextBlock { Text = message, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };
            await dialog.ShowDialog(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                // Subscribe to property changes for dynamic column width updates
                viewModel.AgentManagementViewModel.PropertyChanged += this.OnAgentManagementPropertyChanged;
                viewModel.DataSourceManagementViewModel.PropertyChanged += this.OnDataSourceManagementPropertyChanged;

                // Set initial state (collapsed by default)
                this.UpdateAgentColumnWidth(false);
                this.UpdateDataSourceColumnWidth(false);
            }
        }

        private void OnAgentManagementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AgentManagementViewModel.IsExpanded) &&
                sender is AgentManagementViewModel agentViewModel)
            {
                this.UpdateAgentColumnWidth(agentViewModel.IsExpanded);
            }
        }

        private void OnDataSourceManagementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataSourceManagementViewModel.IsExpanded) &&
                sender is DataSourceManagementViewModel dataSourceViewModel)
            {
                this.UpdateDataSourceColumnWidth(dataSourceViewModel.IsExpanded);
            }
        }

        private void UpdateAgentColumnWidth(bool isExpanded)
        {
            Grid? mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid?.ColumnDefinitions.Count > 0)
            {
                mainGrid.ColumnDefinitions[0].Width = isExpanded ? new GridLength(400, GridUnitType.Pixel) : new GridLength(50, GridUnitType.Pixel);
            }
        }

        private void UpdateDataSourceColumnWidth(bool isExpanded)
        {
            Grid? mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid?.ColumnDefinitions.Count > 1)
            {
                mainGrid.ColumnDefinitions[1].Width = isExpanded ? new GridLength(600, GridUnitType.Pixel) : new GridLength(50, GridUnitType.Pixel);
            }
        }
    }
}