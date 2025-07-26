namespace AgntsChatUI.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    // Core interface for all data source implementations
    public interface IDataSource
    {
        string Name { get; }
        string Description { get; }
        bool IsEnabled { get; set; }

        // Primary search method - returns full content for documents
        Task<IEnumerable<DataSourceResult>> SearchAsync(string query, Dictionary<string, object>? parameters = null);

        // Configuration validation
        Task<bool> ValidateConfigurationAsync();
    }
}