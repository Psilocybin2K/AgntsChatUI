namespace AgntsChatUI.Services
{
    using System;

    using AgntsChatUI.Models;

    // Factory for creating data source instances by type
    public static class DataSourceFactory
    {
        public static IDataSource CreateDataSource(DataSourceDefinition definition, IServiceProvider serviceProvider)
        {
            return definition.Type switch
            {
                DataSourceType.LocalFiles => new LocalFilesDataSource(definition),
                DataSourceType.WebApi => throw new NotImplementedException(), // Placeholder for future types
                // Add more types as implemented...
                _ => throw new NotSupportedException($"Data source type {definition.Type} is not supported")
            };
        }
    }
}