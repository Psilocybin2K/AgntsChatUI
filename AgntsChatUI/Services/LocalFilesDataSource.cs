using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AgntsChatUI.Models;
using System.IO;
using System.Linq;

namespace AgntsChatUI.Services
{
    // Single file data source implementation
    public class LocalFilesDataSource : IDataSource
    {
        private readonly LocalFilesConfiguration _configuration;
        
        public string Name { get; }
        public string Description { get; }
        public bool IsEnabled { get; set; }
        
        public LocalFilesDataSource(DataSourceDefinition definition)
        {
            Name = definition.Name;
            Description = definition.Description;
            IsEnabled = definition.IsEnabled;
            
            // Deserialize configuration from JSON
            _configuration = JsonSerializer.Deserialize<LocalFilesConfiguration>(definition.ConfigurationJson) 
                ?? new LocalFilesConfiguration();
        }
        
        public async Task<IEnumerable<DataSourceResult>> SearchAsync(string query, Dictionary<string, object>? parameters = null)
        {
            if (!await ValidateConfigurationAsync())
                return Enumerable.Empty<DataSourceResult>();
            
            var results = new List<DataSourceResult>();
            
            try
            {
                if (!IsFileSupported(_configuration.FilePath) || !IsFileSizeAcceptable(_configuration.FilePath))
                    return Enumerable.Empty<DataSourceResult>();
                
                var content = await ReadFileContentAsync(_configuration.FilePath);
                if (string.IsNullOrWhiteSpace(content))
                    return Enumerable.Empty<DataSourceResult>();
                
                // Simple keyword search (case-insensitive)
                if (content.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                    Path.GetFileNameWithoutExtension(_configuration.FilePath).Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(CreateDataSourceResult(_configuration.FilePath, content));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to search file '{_configuration.FilePath}': {ex.Message}");
            }
            
            return results;
        }
        
        public Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_configuration.FilePath))
                    return Task.FromResult(false);
                
                if (!File.Exists(_configuration.FilePath))
                    return Task.FromResult(false);
                
                // Check if file is accessible
                try
                {
                    using var stream = File.OpenRead(_configuration.FilePath);
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
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return _configuration.SupportedExtensions.Contains(extension);
        }

        private bool IsFileSizeAcceptable(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length <= _configuration.MaxFileSizeBytes;
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
                SourceName = Name,
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