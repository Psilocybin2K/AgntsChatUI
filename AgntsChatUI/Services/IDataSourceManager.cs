namespace AgntsChatUI.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    // Runtime management of active data sources and search coordination
    public interface IDataSourceManager
    {
        Task InitializeAsync();
        Task<IEnumerable<DataSourceResult>> SearchAllSourcesAsync(string query, Dictionary<string, object>? parameters = null);
        Task<IEnumerable<IDataSource>> GetActiveDataSourcesAsync();
        Task RefreshDataSourcesAsync();
    }
}