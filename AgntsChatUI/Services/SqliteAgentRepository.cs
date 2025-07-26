namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using AgntsChatUI.AI;

    using Microsoft.Data.Sqlite;

    /// <summary>
    /// SQLite implementation of the agent repository
    /// </summary>
    public class SqliteAgentRepository : IAgentRepository
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public SqliteAgentRepository()
        {
            // Store database in user's app data folder
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AgntsChatUI");

            // Ensure directory exists
            Directory.CreateDirectory(appDataPath);

            this._databasePath = Path.Combine(appDataPath, "agents.db");
            this._connectionString = $"Data Source={this._databasePath}";
        }

        public async Task InitializeDatabaseAsync()
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync();

            string createTableCommand = @"
                CREATE TABLE IF NOT EXISTS Agents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    InstructionsPath TEXT,
                    PromptyPath TEXT
                )";

            using SqliteCommand command = new SqliteCommand(createTableCommand, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<AgentDefinition>> GetAllAgentsAsync()
        {
            List<AgentDefinition> agents = new List<AgentDefinition>();

            using SqliteConnection connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync();

            string selectCommand = "SELECT Id, Name, Description, InstructionsPath, PromptyPath FROM Agents ORDER BY Name";
            using SqliteCommand command = new SqliteCommand(selectCommand, connection);
            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                AgentDefinition agent = new AgentDefinition
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    InstructionsPath = reader.IsDBNull(reader.GetOrdinal("InstructionsPath")) ? string.Empty : reader.GetString(reader.GetOrdinal("InstructionsPath")),
                    PromptyPath = reader.IsDBNull(reader.GetOrdinal("PromptyPath")) ? string.Empty : reader.GetString(reader.GetOrdinal("PromptyPath"))
                };

                agents.Add(agent);
            }

            return agents;
        }

        public async Task<AgentDefinition> SaveAgentAsync(AgentDefinition agent)
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync();

            if (agent.Id.HasValue)
            {
                // Update existing agent
                string updateCommand = @"
                    UPDATE Agents 
                    SET Name = @Name, Description = @Description, InstructionsPath = @InstructionsPath, PromptyPath = @PromptyPath 
                    WHERE Id = @Id";

                using SqliteCommand command = new SqliteCommand(updateCommand, connection);
                command.Parameters.AddWithValue("@Id", agent.Id.Value);
                command.Parameters.AddWithValue("@Name", agent.Name);
                command.Parameters.AddWithValue("@Description", agent.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InstructionsPath", agent.InstructionsPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PromptyPath", agent.PromptyPath ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
                return agent;
            }
            else
            {
                // Insert new agent
                string insertCommand = @"
                    INSERT INTO Agents (Name, Description, InstructionsPath, PromptyPath) 
                    VALUES (@Name, @Description, @InstructionsPath, @PromptyPath);
                    SELECT last_insert_rowid();";

                using SqliteCommand command = new SqliteCommand(insertCommand, connection);
                command.Parameters.AddWithValue("@Name", agent.Name);
                command.Parameters.AddWithValue("@Description", agent.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InstructionsPath", agent.InstructionsPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PromptyPath", agent.PromptyPath ?? (object)DBNull.Value);

                object? newId = await command.ExecuteScalarAsync();
                agent.Id = Convert.ToInt32(newId);
                return agent;
            }
        }

        public async Task<bool> DeleteAgentAsync(int id)
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync();

            string deleteCommand = "DELETE FROM Agents WHERE Id = @Id";
            using SqliteCommand command = new SqliteCommand(deleteCommand, connection);
            command.Parameters.AddWithValue("@Id", id);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}