namespace AgntsChatUI.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    // Business logic interface for data source management
    public interface IDataSourceService
    {
        Task InitializeAsync();
        Task<IEnumerable<DataSourceDefinition>> GetAllDataSourcesAsync();
        Task<DataSourceDefinition> SaveDataSourceAsync(DataSourceDefinition dataSource);
        Task<bool> DeleteDataSourceAsync(int id);
        Task<bool> ValidateDataSourceAsync(DataSourceDefinition dataSource);
    }
}