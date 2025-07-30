namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    using Microsoft.Data.Sqlite;

    // SQLite implementation of data source repository
    public class SqliteDataSourceRepository : IDataSourceRepository
    {
        private readonly string _connectionString;
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 1000;

        public SqliteDataSourceRepository()
        {
            // Use same database as agents
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AgntsChatUI");
            Directory.CreateDirectory(appDataPath);

            string databasePath = Path.Combine(appDataPath, "agents.db");
            this._connectionString = $"Data Source={databasePath};Cache=Shared;";
        }

        public async Task InitializeDatabaseAsync()
        {
            await this.ExecuteWithRetryAsync(async () =>
            {
                using SqliteConnection connection = new SqliteConnection(this._connectionString);
                await connection.OpenAsync();

                string createTableCommand = @"
                    CREATE TABLE IF NOT EXISTS DataSources (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        Type INTEGER NOT NULL,
                        ConfigurationJson TEXT NOT NULL DEFAULT '{}',
                        IsEnabled BOOLEAN NOT NULL DEFAULT 1,
                        DateCreated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        DateModified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(Name)
                    )";

                using SqliteCommand command = new SqliteCommand(createTableCommand, connection);
                await command.ExecuteNonQueryAsync();
            }, "Failed to initialize data sources database");
        }

        public async Task<IEnumerable<DataSourceDefinition>> GetAllDataSourcesAsync()
        {
            return await this.ExecuteWithRetryAsync(async () =>
            {
                List<DataSourceDefinition> results = new List<DataSourceDefinition>();
                using SqliteConnection connection = new SqliteConnection(this._connectionString);
                await connection.OpenAsync();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, Description, Type, ConfigurationJson, IsEnabled, DateCreated, DateModified FROM DataSources ORDER BY DateCreated DESC";
                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    try
                    {
                        results.Add(new DataSourceDefinition
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Type = (Models.DataSourceType)reader.GetInt32(3),
                            ConfigurationJson = reader.IsDBNull(4) ? "{}" : reader.GetString(4),
                            IsEnabled = reader.GetBoolean(5),
                            DateCreated = reader.GetDateTime(6),
                            DateModified = reader.GetDateTime(7)
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to parse data source row: {ex.Message}");
                        // Continue with other rows
                    }
                }

                return results;
            }, "Failed to retrieve data sources from database");
        }

        public async Task<DataSourceDefinition> SaveDataSourceAsync(DataSourceDefinition dataSource)
        {
            return dataSource == null
                ? throw new ArgumentNullException(nameof(dataSource))
                : await this.ExecuteWithRetryAsync(async () =>
            {
                using SqliteConnection connection = new SqliteConnection(this._connectionString);
                await connection.OpenAsync();

                if (dataSource.Id.HasValue)
                {
                    // Update
                    SqliteCommand command = connection.CreateCommand();
                    command.CommandText = @"UPDATE DataSources 
                        SET Name = @Name, Description = @Description, Type = @Type, 
                            ConfigurationJson = @ConfigurationJson, IsEnabled = @IsEnabled, 
                            DateModified = CURRENT_TIMESTAMP 
                        WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Name", dataSource.Name ?? string.Empty);
                    command.Parameters.AddWithValue("@Description", dataSource.Description ?? string.Empty);
                    command.Parameters.AddWithValue("@Type", (int)dataSource.Type);
                    command.Parameters.AddWithValue("@ConfigurationJson", dataSource.ConfigurationJson ?? "{}");
                    command.Parameters.AddWithValue("@IsEnabled", dataSource.IsEnabled);
                    command.Parameters.AddWithValue("@Id", dataSource.Id.Value);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"Data source with ID {dataSource.Id} not found for update");
                    }
                }
                else
                {
                    // Insert
                    SqliteCommand command = connection.CreateCommand();
                    command.CommandText = @"INSERT INTO DataSources 
                        (Name, Description, Type, ConfigurationJson, IsEnabled, DateCreated, DateModified) 
                        VALUES (@Name, @Description, @Type, @ConfigurationJson, @IsEnabled, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP); 
                        SELECT last_insert_rowid();";
                    command.Parameters.AddWithValue("@Name", dataSource.Name ?? string.Empty);
                    command.Parameters.AddWithValue("@Description", dataSource.Description ?? string.Empty);
                    command.Parameters.AddWithValue("@Type", (int)dataSource.Type);
                    command.Parameters.AddWithValue("@ConfigurationJson", dataSource.ConfigurationJson ?? "{}");
                    command.Parameters.AddWithValue("@IsEnabled", dataSource.IsEnabled);

                    object? result = await command.ExecuteScalarAsync();
                    long id = result != null ? (long)result : 0;
                    if (id == 0)
                    {
                        throw new InvalidOperationException("Failed to insert data source - no ID returned");
                    }

                    dataSource.Id = (int)id;
                }

                // Return the up-to-date entity
                DataSourceDefinition? updated = await this.GetDataSourceByIdAsync(dataSource.Id.Value);
                return updated ?? throw new InvalidOperationException("Failed to retrieve saved data source");
            }, $"Failed to save data source '{dataSource.Name}'");
        }

        public async Task<bool> DeleteDataSourceAsync(int id)
        {
            return await this.ExecuteWithRetryAsync(async () =>
            {
                using SqliteConnection connection = new SqliteConnection(this._connectionString);
                await connection.OpenAsync();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM DataSources WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);
                int rows = await command.ExecuteNonQueryAsync();
                return rows > 0;
            }, $"Failed to delete data source with ID {id}");
        }

        public async Task<DataSourceDefinition?> GetDataSourceByIdAsync(int id)
        {
            return await this.ExecuteWithRetryAsync(async () =>
            {
                using SqliteConnection connection = new SqliteConnection(this._connectionString);
                await connection.OpenAsync();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, Description, Type, ConfigurationJson, IsEnabled, DateCreated, DateModified FROM DataSources WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);
                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                return await reader.ReadAsync()
                    ? new DataSourceDefinition
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Type = (Models.DataSourceType)reader.GetInt32(3),
                        ConfigurationJson = reader.IsDBNull(4) ? "{}" : reader.GetString(4),
                        IsEnabled = reader.GetBoolean(5),
                        DateCreated = reader.GetDateTime(6),
                        DateModified = reader.GetDateTime(7)
                    }
                    : null;
            }, $"Failed to retrieve data source with ID {id}");
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string errorMessage)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (SqliteException ex) when (IsRetriableError(ex))
                {
                    lastException = ex;
                    System.Diagnostics.Debug.WriteLine($"Database operation failed (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");

                    if (attempt < MaxRetryAttempts)
                    {
                        await Task.Delay(RetryDelayMs * attempt); // Exponential backoff
                    }
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
                {
                    throw new InvalidOperationException($"Database constraint violation: {ex.Message}. This may indicate duplicate data or invalid references.", ex);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 11) // SQLITE_CORRUPT
                {
                    throw new InvalidOperationException($"Database corruption detected: {ex.Message}. Please contact support.", ex);
                }
                catch (SqliteException ex)
                {
                    throw new InvalidOperationException($"{errorMessage}: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"{errorMessage}: {ex.Message}", ex);
                }
            }

            throw new InvalidOperationException($"{errorMessage} after {MaxRetryAttempts} attempts. Last error: {lastException?.Message}", lastException);
        }

        private async Task ExecuteWithRetryAsync(Func<Task> operation, string errorMessage)
        {
            await this.ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true;
            }, errorMessage);
        }

        private static bool IsRetriableError(SqliteException ex)
        {
            return ex.SqliteErrorCode == 5 ||  // SQLITE_BUSY
                   ex.SqliteErrorCode == 6 ||  // SQLITE_LOCKED
                   ex.SqliteErrorCode == 10 || // SQLITE_IOERR
                   ex.SqliteErrorCode == 9;    // SQLITE_INTERRUPT
        }
    }
}