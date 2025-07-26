using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgntsChatUI.Models;
using System.Linq;

namespace AgntsChatUI.Services
{
    // Factory-based instantiation and search coordination across all sources
    public class DataSourceManager : IDataSourceManager
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<int, IDataSource> _activeDataSources = new();
        
        public DataSourceManager(IDataSourceService dataSourceService, IServiceProvider serviceProvider)
        {
            _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        
        public async Task InitializeAsync()
        {
            await _dataSourceService.InitializeAsync();
            await RefreshDataSourcesAsync();
        }

        public async Task<IEnumerable<DataSourceResult>> SearchAllSourcesAsync(string query, Dictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<DataSourceResult>();
            
            var allResults = new List<DataSourceResult>();
            var searchTasks = new List<Task<IEnumerable<DataSourceResult>>>();
            
            foreach (var dataSource in _activeDataSources.Values)
            {
                searchTasks.Add(SearchSingleSourceAsync(dataSource, query, parameters));
            }
            
            var results = await Task.WhenAll(searchTasks);
            foreach (var result in results)
            {
                allResults.AddRange(result);
            }
            
            return allResults;
        }

        public Task<IEnumerable<IDataSource>> GetActiveDataSourcesAsync()
        {
            return Task.FromResult(_activeDataSources.Values.AsEnumerable());
        }

        public async Task RefreshDataSourcesAsync()
        {
            _activeDataSources.Clear();
            
            var definitions = await _dataSourceService.GetAllDataSourcesAsync();
            foreach (var definition in definitions.Where(d => d.IsEnabled))
            {
                try
                {
                    var dataSource = DataSourceFactory.CreateDataSource(definition, _serviceProvider);
                    if (await dataSource.ValidateConfigurationAsync())
                    {
                        _activeDataSources[definition.Id ?? 0] = dataSource;
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