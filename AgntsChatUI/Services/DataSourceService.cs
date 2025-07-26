using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgntsChatUI.Models;
using System.Text.Json;
using System.IO;

namespace AgntsChatUI.Services
{
    // Business logic implementation with validation and configuration handling
    public class DataSourceService : IDataSourceService
    {
        private readonly IDataSourceRepository _repository;
        
        public DataSourceService(IDataSourceRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        
        public async Task InitializeAsync() => await _repository.InitializeDatabaseAsync();
        public async Task<IEnumerable<DataSourceDefinition>> GetAllDataSourcesAsync() => await _repository.GetAllDataSourcesAsync();
        public async Task<DataSourceDefinition> SaveDataSourceAsync(DataSourceDefinition dataSource) => await _repository.SaveDataSourceAsync(dataSource);
        public async Task<bool> DeleteDataSourceAsync(int id) => await _repository.DeleteDataSourceAsync(id);
        
        public async Task<bool> ValidateDataSourceAsync(DataSourceDefinition dataSource)
        {
            try
            {
                // Basic validation: required fields
                if (dataSource == null)
                    return false;
                
                if (string.IsNullOrWhiteSpace(dataSource.Name))
                    return false;
                
                if (dataSource.Name.Length > 100) // Reasonable name length limit
                    return false;
                
                if (dataSource.Description?.Length > 500) // Reasonable description length limit
                    return false;
                
                // JSON configuration validation
                if (string.IsNullOrWhiteSpace(dataSource.ConfigurationJson))
                    dataSource.ConfigurationJson = "{}"; // Set default empty JSON
                
                if (!IsValidJson(dataSource.ConfigurationJson))
                    return false;
                
                // Type-specific validation
                if (!await ValidateTypeSpecificConfigurationAsync(dataSource))
                    return false;
                
                // Try to create the data source and call ValidateConfigurationAsync
                var instance = DataSourceFactory.CreateDataSource(dataSource, null!);
                if (instance != null)
                {
                    return await instance.ValidateConfigurationAsync();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Validation failed for data source '{dataSource?.Name}': {ex.Message}");
                return false;
            }
        }
        
        private static bool IsValidJson(string jsonString)
        {
            try
            {
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        
        private async Task<bool> ValidateTypeSpecificConfigurationAsync(DataSourceDefinition dataSource)
        {
            return dataSource.Type switch
            {
                DataSourceType.LocalFiles => await ValidateLocalFilesConfigurationAsync(dataSource.ConfigurationJson),
                DataSourceType.WebApi => ValidateWebApiConfiguration(dataSource.ConfigurationJson),
                DataSourceType.Database => ValidateDatabaseConfiguration(dataSource.ConfigurationJson),
                DataSourceType.SharePoint => ValidateSharePointConfiguration(dataSource.ConfigurationJson),
                DataSourceType.Custom => ValidateCustomConfiguration(dataSource.ConfigurationJson),
                _ => false
            };
        }
        
        private Task<bool> ValidateLocalFilesConfigurationAsync(string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<LocalFilesConfiguration>(configJson);
                if (config == null)
                    return Task.FromResult(false);
                
                // Validate file path
                if (string.IsNullOrWhiteSpace(config.FilePath))
                    return Task.FromResult(false);
                
                // Check if file exists and is accessible
                if (!File.Exists(config.FilePath))
                    return Task.FromResult(false);
                
                // Try to access the file
                try
                {
                    using var stream = File.OpenRead(config.FilePath);
                }
                catch (UnauthorizedAccessException)
                {
                    return Task.FromResult(false);
                }
                catch (Exception)
                {
                    return Task.FromResult(false);
                }
                
                // Validate file extensions
                if (config.SupportedExtensions == null || config.SupportedExtensions.Length == 0)
                    return Task.FromResult(false);
                
                foreach (var ext in config.SupportedExtensions)
                {
                    if (string.IsNullOrWhiteSpace(ext) || !ext.StartsWith("."))
                        return Task.FromResult(false);
                }
                
                // Validate file size limit (must be positive and reasonable)
                if (config.MaxFileSizeBytes <= 0 || config.MaxFileSizeBytes > 100 * 1024 * 1024) // Max 100MB
                    return Task.FromResult(false);
                
                return Task.FromResult(true);
            }
            catch (JsonException)
            {
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Local files configuration validation failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }
        
        private bool ValidateWebApiConfiguration(string configJson)
        {
            try
            {
                // Placeholder for WebAPI configuration validation
                var config = JsonDocument.Parse(configJson);
                
                // Basic structure validation for web API
                if (config.RootElement.TryGetProperty("endpoint", out var endpoint))
                {
                    var endpointStr = endpoint.GetString();
                    if (string.IsNullOrWhiteSpace(endpointStr))
                        return false;
                    
                    // Validate URL format
                    if (!Uri.TryCreate(endpointStr, UriKind.Absolute, out var uri))
                        return false;
                    
                    // Ensure it's HTTP or HTTPS
                    if (uri.Scheme != "http" && uri.Scheme != "https")
                        return false;
                }
                
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        
        private bool ValidateDatabaseConfiguration(string configJson)
        {
            try
            {
                // Placeholder for database configuration validation
                var config = JsonDocument.Parse(configJson);
                
                // Basic structure validation for database
                if (config.RootElement.TryGetProperty("connectionString", out var connStr))
                {
                    if (string.IsNullOrWhiteSpace(connStr.GetString()))
                        return false;
                }
                
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        
        private bool ValidateSharePointConfiguration(string configJson)
        {
            try
            {
                // Placeholder for SharePoint configuration validation
                var config = JsonDocument.Parse(configJson);
                
                // Basic structure validation for SharePoint
                if (config.RootElement.TryGetProperty("siteUrl", out var siteUrl))
                {
                    var siteUrlStr = siteUrl.GetString();
                    if (string.IsNullOrWhiteSpace(siteUrlStr))
                        return false;
                    
                    // Validate URL format
                    if (!Uri.TryCreate(siteUrlStr, UriKind.Absolute, out var uri))
                        return false;
                }
                
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        
        private bool ValidateCustomConfiguration(string configJson)
        {
            try
            {
                // For custom data sources, just validate that it's valid JSON
                JsonDocument.Parse(configJson);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}