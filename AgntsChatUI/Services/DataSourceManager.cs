namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    // Factory-based instantiation and search coordination across all sources
    public class DataSourceManager : IDataSourceManager
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<int, IDataSource> _activeDataSources = new();

        public DataSourceManager(IDataSourceService dataSourceService, IServiceProvider serviceProvider)
        {
            this._dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task InitializeAsync()
        {
            await this._dataSourceService.InitializeAsync();
            await this.RefreshDataSourcesAsync();
        }

        public async Task<IEnumerable<DataSourceResult>> SearchAllSourcesAsync(string query, Dictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<DataSourceResult>();
            }

            List<DataSourceResult> allResults = new List<DataSourceResult>();
            List<Task<IEnumerable<DataSourceResult>>> searchTasks = new List<Task<IEnumerable<DataSourceResult>>>();

            foreach (IDataSource dataSource in this._activeDataSources.Values)
            {
                searchTasks.Add(this.SearchSingleSourceAsync(dataSource, query, parameters));
            }

            IEnumerable<DataSourceResult>[] results = await Task.WhenAll(searchTasks);
            foreach (IEnumerable<DataSourceResult>? result in results)
            {
                allResults.AddRange(result);
            }

            return allResults;
        }

        public Task<IEnumerable<IDataSource>> GetActiveDataSourcesAsync()
        {
            return Task.FromResult(this._activeDataSources.Values.AsEnumerable());
        }

        public async Task RefreshDataSourcesAsync()
        {
            this._activeDataSources.Clear();

            IEnumerable<DataSourceDefinition> definitions = await this._dataSourceService.GetAllDataSourcesAsync();
            foreach (DataSourceDefinition? definition in definitions.Where(d => d.IsEnabled))
            {
                try
                {
                    IDataSource dataSource = DataSourceFactory.CreateDataSource(definition, this._serviceProvider);
                    if (await dataSource.ValidateConfigurationAsync())
                    {
                        this._activeDataSources[definition.Id ?? 0] = dataSource;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other data sources
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize data source '{definition.Name}': {ex.Message}");
                }
            }
        }

        private async Task<IEnumerable<DataSourceResult>> SearchSingleSourceAsync(
            IDataSource dataSource, string query, Dictionary<string, object>? parameters)
        {
            try
            {
                return await dataSource.SearchAsync(query, parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search failed for data source '{dataSource.Name}': {ex.Message}");
                return Enumerable.Empty<DataSourceResult>();
            }
        }
    }
}