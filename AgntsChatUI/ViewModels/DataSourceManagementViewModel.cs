namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO; // Added for Directory.GetFiles
    using System.Threading.Tasks;

    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    using Avalonia; // Added for Application access
    using Avalonia.Controls.ApplicationLifetimes; // Added for desktop access
    using Avalonia.Platform.Storage; // Added for file dialog

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    // CRUD operations for data sources, enable/disable toggles
    public partial class DataSourceManagementViewModel : ViewModelBase
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly IDataSourceManager _dataSourceManager;

        [ObservableProperty]
        private ObservableCollection<DataSourceDefinition> dataSources = new();

        [ObservableProperty]
        private DataSourceDefinition? selectedDataSource;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isOperationInProgress;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private string operationStatus = string.Empty;

        [ObservableProperty]
        private bool isExpanded = false; // Collapsed by default

        public DataSourceManagementViewModel(IDataSourceService dataSourceService, IDataSourceManager dataSourceManager)
        {
            this._dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            this._dataSourceManager = dataSourceManager ?? throw new ArgumentNullException(nameof(dataSourceManager));

            _ = this.InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await this.ExecuteWithLoadingAsync(async () =>
            {
                await this._dataSourceService.InitializeAsync();
                IEnumerable<DataSourceDefinition> sources = await this._dataSourceService.GetAllDataSourcesAsync();
                this.DataSources = new ObservableCollection<DataSourceDefinition>(sources);
                await this._dataSourceManager.RefreshDataSourcesAsync();
                this.StatusMessage = $"Loaded {this.DataSources.Count} data sources";
            }, "Initializing data sources...");
        }

        // Implement CRUD commands, configuration dialogs, enable/disable toggles...
        /// <summary>
        /// Add a data source by selecting a single file
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteOperations))]
        private async Task AddDataSource()
        {
            await this.ExecuteWithOperationFeedbackAsync(async () =>
            {
                // Open file dialog to select a single file
                string? selectedFile = await this.OpenSingleFileDialogAsync();
                if (!string.IsNullOrEmpty(selectedFile))
                {
                    // Create a new data source with the selected file
                    LocalFilesConfiguration configuration = new LocalFilesConfiguration
                    {
                        FilePath = selectedFile,
                        SupportedExtensions = new[] { ".txt", ".md", ".json", ".xml", ".csv", ".pdf", ".doc", ".docx" },
                        MaxFileSizeBytes = 10 * 1024 * 1024 // 10MB
                    };

                    string configJson = System.Text.Json.JsonSerializer.Serialize(configuration, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    string fileName = Path.GetFileNameWithoutExtension(selectedFile);
                    DataSourceDefinition newSource = new DataSourceDefinition
                    {
                        Name = $"File: {fileName}",
                        Description = $"Single file data source: {Path.GetFileName(selectedFile)}",
                        Type = DataSourceType.LocalFiles,
                        ConfigurationJson = configJson,
                        IsEnabled = true,
                        DateCreated = DateTime.Now,
                        DateModified = DateTime.Now
                    };

                    if (!await this._dataSourceService.ValidateDataSourceAsync(newSource))
                    {
                        throw new InvalidOperationException("Invalid file configuration. Please check the file path and permissions.");
                    }

                    DataSourceDefinition saved = await this._dataSourceService.SaveDataSourceAsync(newSource);
                    this.DataSources.Add(saved);
                    await this._dataSourceManager.RefreshDataSourcesAsync();

                    return $"Successfully imported file '{Path.GetFileName(selectedFile)}' as data source '{saved.Name}'";
                }
                else
                {
                    return "No file selected for import";
                }
            }, "Selecting file for import...");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedOperations))]
        private async Task EditDataSource()
        {
            if (this.SelectedDataSource == null)
            {
                return;
            }

            await this.ExecuteWithOperationFeedbackAsync(async () =>
            {
                // Stub: in real app, show edit dialog and update fields
                this.SelectedDataSource.Description += " (edited)";
                this.SelectedDataSource.DateModified = DateTime.Now;

                if (!await this._dataSourceService.ValidateDataSourceAsync(this.SelectedDataSource))
                {
                    throw new InvalidOperationException("Invalid data source configuration after editing.");
                }

                DataSourceDefinition saved = await this._dataSourceService.SaveDataSourceAsync(this.SelectedDataSource);

                // Replace in collection safely
                int idx = this.DataSources.IndexOf(this.SelectedDataSource);
                if (idx >= 0)
                {
                    this.DataSources[idx] = saved;
                    this.SelectedDataSource = saved; // Update selection
                }

                await this._dataSourceManager.RefreshDataSourcesAsync();

                return $"Successfully updated data source '{saved.Name}'";
            }, "Updating data source...");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSelectedOperations))]
        private async Task DeleteDataSource()
        {
            if (this.SelectedDataSource == null)
            {
                return;
            }

            string dataSourceName = this.SelectedDataSource.Name;
            int dataSourceId = this.SelectedDataSource.Id ?? 0;

            await this.ExecuteWithOperationFeedbackAsync(async () =>
            {
                if (dataSourceId == 0)
                {
                    throw new InvalidOperationException("Cannot delete data source: Invalid ID.");
                }

                bool success = await this._dataSourceService.DeleteDataSourceAsync(dataSourceId);
                if (!success)
                {
                    throw new InvalidOperationException("Failed to delete data source: Not found or already deleted.");
                }

                this.DataSources.Remove(this.SelectedDataSource);
                this.SelectedDataSource = null;
                await this._dataSourceManager.RefreshDataSourcesAsync();

                return $"Successfully deleted data source '{dataSourceName}'";
            }, "Deleting data source...");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteOperations))]
        private async Task ToggleDataSource(DataSourceDefinition dataSource)
        {
            if (dataSource == null)
            {
                return;
            }

            bool originalState = dataSource.IsEnabled;
            string action = originalState ? "disable" : "enable";

            await this.ExecuteWithOperationFeedbackAsync(async () =>
            {
                dataSource.IsEnabled = !dataSource.IsEnabled;
                dataSource.DateModified = DateTime.Now;

                if (!await this._dataSourceService.ValidateDataSourceAsync(dataSource))
                {
                    // Revert on validation failure
                    dataSource.IsEnabled = originalState;
                    throw new InvalidOperationException($"Cannot {action} data source: Invalid configuration.");
                }

                DataSourceDefinition saved = await this._dataSourceService.SaveDataSourceAsync(dataSource);
                int idx = this.DataSources.IndexOf(dataSource);
                if (idx >= 0)
                {
                    this.DataSources[idx] = saved;
                }

                await this._dataSourceManager.RefreshDataSourcesAsync();

                return $"Successfully {action}d data source '{saved.Name}'";
            }, $"{char.ToUpper(action[0])}{action.Substring(1)}ing data source...");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteOperations))]
        private async Task RefreshDataSources()
        {
            await this.ExecuteWithLoadingAsync(async () =>
            {
                IEnumerable<DataSourceDefinition> sources = await this._dataSourceService.GetAllDataSourcesAsync();
                this.DataSources.Clear();
                foreach (DataSourceDefinition source in sources)
                {
                    this.DataSources.Add(source);
                }

                await this._dataSourceManager.RefreshDataSourcesAsync();
                this.StatusMessage = $"Refreshed {this.DataSources.Count} data sources";
            }, "Refreshing data sources...");
        }

        [RelayCommand]
        private void ClearError()
        {
            this.HasError = false;
            this.ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Toggles the expanded/collapsed state of the panel
        /// </summary>
        [RelayCommand]
        private void ToggleExpanded()
        {
            this.IsExpanded = !this.IsExpanded;
        }

        /// <summary>
        /// Select a single file for import as a data source
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteOperations))]
        private async Task SelectFileForDataSource()
        {
            await this.ExecuteWithOperationFeedbackAsync(async () =>
            {
                // Open file dialog to select a single file
                string? selectedFile = await this.OpenSingleFileDialogAsync();
                if (!string.IsNullOrEmpty(selectedFile))
                {
                    // Create a new data source with the selected file
                    LocalFilesConfiguration configuration = new LocalFilesConfiguration
                    {
                        FilePath = selectedFile,
                        SupportedExtensions = new[] { ".txt", ".md", ".json", ".xml", ".csv", ".pdf", ".doc", ".docx" },
                        MaxFileSizeBytes = 10 * 1024 * 1024 // 10MB
                    };

                    string configJson = System.Text.Json.JsonSerializer.Serialize(configuration, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    string fileName = Path.GetFileNameWithoutExtension(selectedFile);
                    DataSourceDefinition newSource = new DataSourceDefinition
                    {
                        Name = $"File: {fileName}",
                        Description = $"Single file data source: {Path.GetFileName(selectedFile)}",
                        Type = DataSourceType.LocalFiles,
                        ConfigurationJson = configJson,
                        IsEnabled = true,
                        DateCreated = DateTime.Now,
                        DateModified = DateTime.Now
                    };

                    if (!await this._dataSourceService.ValidateDataSourceAsync(newSource))
                    {
                        throw new InvalidOperationException("Invalid file configuration. Please check the file path and permissions.");
                    }

                    DataSourceDefinition saved = await this._dataSourceService.SaveDataSourceAsync(newSource);
                    this.DataSources.Add(saved);
                    await this._dataSourceManager.RefreshDataSourcesAsync();

                    return $"Successfully imported file '{Path.GetFileName(selectedFile)}' as data source '{saved.Name}'";
                }
                else
                {
                    return "No file selected for import";
                }
            }, "Selecting file for import...");
        }

        /// <summary>
        /// Open file dialog to select a single file for import
        /// </summary>
        private async Task<string?> OpenSingleFileDialogAsync()
        {
            try
            {
                // Get the main window's storage provider
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    Avalonia.Controls.Window? mainWindow = desktop.MainWindow;
                    if (mainWindow?.StorageProvider != null)
                    {
                        // Define file type filters
                        FilePickerFileType[] fileTypes = new[]
                        {
                            new FilePickerFileType("Text Files")
                            {
                                Patterns = new[] { "*.txt", "*.md" }
                            },
                            new FilePickerFileType("Data Files")
                            {
                                Patterns = new[] { "*.json", "*.xml", "*.csv" }
                            },
                            new FilePickerFileType("Document Files")
                            {
                                Patterns = new[] { "*.pdf", "*.doc", "*.docx" }
                            },
                            new FilePickerFileType("All Supported")
                            {
                                Patterns = new[] { "*.txt", "*.md", "*.json", "*.xml", "*.csv", "*.pdf", "*.doc", "*.docx" }
                            }
                        };

                        // Open file picker for single file selection
                        IReadOnlyList<IStorageFile> files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                        {
                            Title = "Select File to Import",
                            AllowMultiple = false, // Single file only
                            FileTypeFilter = fileTypes
                        });

                        // Return the first (and only) selected file
                        if (files?.Count > 0)
                        {
                            if (files[0].TryGetLocalPath() is string localPath)
                            {
                                return localPath;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open file dialog: {ex.Message}");
                return null;
            }
        }

        // Can execute methods for command state management
        private bool CanExecuteOperations() => !this.IsLoading && !this.IsOperationInProgress;
        private bool CanExecuteSelectedOperations() => this.CanExecuteOperations() && this.SelectedDataSource != null;

        // Helper methods for UI state management
        private async Task ExecuteWithLoadingAsync(Func<Task> operation, string loadingMessage)
        {
            if (this.IsLoading || this.IsOperationInProgress)
            {
                return;
            }

            this.IsLoading = true;
            this.HasError = false;
            this.ErrorMessage = string.Empty;
            this.StatusMessage = loadingMessage;

            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.ErrorMessage = ex.Message;
                this.StatusMessage = "Operation failed";
                System.Diagnostics.Debug.WriteLine($"Loading operation failed: {ex}");
            }
            finally
            {
                this.IsLoading = false;
                // Refresh command states
                this.AddDataSourceCommand.NotifyCanExecuteChanged();
                this.EditDataSourceCommand.NotifyCanExecuteChanged();
                this.DeleteDataSourceCommand.NotifyCanExecuteChanged();
                this.RefreshDataSourcesCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task ExecuteWithOperationFeedbackAsync(Func<Task<string>> operation, string operationMessage)
        {
            if (this.IsOperationInProgress)
            {
                return;
            }

            this.IsOperationInProgress = true;
            this.HasError = false;
            this.ErrorMessage = string.Empty;
            this.OperationStatus = operationMessage;

            try
            {
                string successMessage = await operation();
                this.StatusMessage = successMessage;
                this.OperationStatus = "Operation completed successfully";

                // Clear operation status after a delay
                _ = Task.Delay(3000).ContinueWith(_ => this.OperationStatus = string.Empty);
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.ErrorMessage = ex.Message;
                this.StatusMessage = "Operation failed";
                this.OperationStatus = "Operation failed";
                System.Diagnostics.Debug.WriteLine($"Operation failed: {ex}");

                // Clear operation status after a delay
                _ = Task.Delay(5000).ContinueWith(_ => this.OperationStatus = string.Empty);
            }
            finally
            {
                this.IsOperationInProgress = false;
                // Refresh command states
                this.AddDataSourceCommand.NotifyCanExecuteChanged();
                this.EditDataSourceCommand.NotifyCanExecuteChanged();
                this.DeleteDataSourceCommand.NotifyCanExecuteChanged();
                this.ToggleDataSourceCommand.NotifyCanExecuteChanged();
            }
        }
    }
}