namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    // Single file data source implementation
    public class LocalFilesDataSource : IDataSource
    {
        private readonly LocalFilesConfiguration _configuration;

        public string Name { get; }
        public string Description { get; }
        public bool IsEnabled { get; set; }

        public LocalFilesDataSource(DataSourceDefinition definition)
        {
            this.Name = definition.Name;
            this.Description = definition.Description;
            this.IsEnabled = definition.IsEnabled;

            // Deserialize configuration from JSON
            this._configuration = JsonSerializer.Deserialize<LocalFilesConfiguration>(definition.ConfigurationJson)
                ?? new LocalFilesConfiguration();
        }

        public async Task<IEnumerable<DataSourceResult>> SearchAsync(string query, Dictionary<string, object>? parameters = null)
        {
            if (!await this.ValidateConfigurationAsync())
            {
                return Enumerable.Empty<DataSourceResult>();
            }

            List<DataSourceResult> results = new List<DataSourceResult>();

            try
            {
                if (!this.IsFileSupported(this._configuration.FilePath) || !this.IsFileSizeAcceptable(this._configuration.FilePath))
                {
                    return Enumerable.Empty<DataSourceResult>();
                }

                string content = await this.ReadFileContentAsync(this._configuration.FilePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Enumerable.Empty<DataSourceResult>();
                }

                results.Add(this.CreateDataSourceResult(this._configuration.FilePath, content));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to search file '{this._configuration.FilePath}': {ex.Message}");
            }

            return results;
        }

        public Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this._configuration.FilePath))
                {
                    return Task.FromResult(false);
                }

                if (!File.Exists(this._configuration.FilePath))
                {
                    return Task.FromResult(false);
                }

                // Check if file is accessible
                try
                {
                    using FileStream stream = File.OpenRead(this._configuration.FilePath);
                    return Task.FromResult(true);
                }
                catch (UnauthorizedAccessException)
                {
                    return Task.FromResult(false);
                }
                catch
                {
                    return Task.FromResult(false);
                }
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private bool IsFileSupported(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return this._configuration.SupportedExtensions.Contains(extension);
        }

        private bool IsFileSizeAcceptable(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length <= this._configuration.MaxFileSizeBytes;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ReadFileContentAsync(string filePath)
        {
            try
            {
                // For demonstration, reading as text file
                // In a real implementation, you might want to handle different file types
                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read file '{filePath}': {ex.Message}");
                return string.Empty;
            }
        }

        private DataSourceResult CreateDataSourceResult(string filePath, string content)
        {
            return new DataSourceResult
            {
                Content = content,
                Title = Path.GetFileName(filePath),
                SourceName = this.Name,
                SourceType = "LocalFile",
                Metadata = new Dictionary<string, object>
                {
                    ["FilePath"] = filePath,
                    ["FileSize"] = new FileInfo(filePath).Length,
                    ["LastModified"] = File.GetLastWriteTime(filePath)
                },
                RetrievedAt = DateTime.Now
            };
        }
    }
}