namespace AgntsChatUI.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    // Data access interface for data source persistence
    public interface IDataSourceRepository
    {
        Task InitializeDatabaseAsync();
        Task<IEnumerable<DataSourceDefinition>> GetAllDataSourcesAsync();
        Task<DataSourceDefinition> SaveDataSourceAsync(DataSourceDefinition dataSource);
        Task<bool> DeleteDataSourceAsync(int id);
        Task<DataSourceDefinition?> GetDataSourceByIdAsync(int id);
    }
}