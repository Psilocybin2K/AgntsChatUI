using AgntsChatUI.AI;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AgntsChatUI.Services
{
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
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AgntsChatUI");
            
            // Ensure directory exists
            Directory.CreateDirectory(appDataPath);
            
            _databasePath = Path.Combine(appDataPath, "agents.db");
            _connectionString = $"Data Source={_databasePath}";
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var createTableCommand = @"
                CREATE TABLE IF NOT EXISTS Agents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    InstructionsPath TEXT,
                    PromptyPath TEXT
                )";

            using var command = new SqliteCommand(createTableCommand, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<AgentDefinition>> GetAllAgentsAsync()
        {
            var agents = new List<AgentDefinition>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var selectCommand = "SELECT Id, Name, Description, InstructionsPath, PromptyPath FROM Agents ORDER BY Name";
            using var command = new SqliteCommand(selectCommand, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var agent = new AgentDefinition
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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            if (agent.Id.HasValue)
            {
                // Update existing agent
                var updateCommand = @"
                    UPDATE Agents 
                    SET Name = @Name, Description = @Description, InstructionsPath = @InstructionsPath, PromptyPath = @PromptyPath 
                    WHERE Id = @Id";

                using var command = new SqliteCommand(updateCommand, connection);
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
                var insertCommand = @"
                    INSERT INTO Agents (Name, Description, InstructionsPath, PromptyPath) 
                    VALUES (@Name, @Description, @InstructionsPath, @PromptyPath);
                    SELECT last_insert_rowid();";

                using var command = new SqliteCommand(insertCommand, connection);
                command.Parameters.AddWithValue("@Name", agent.Name);
                command.Parameters.AddWithValue("@Description", agent.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InstructionsPath", agent.InstructionsPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PromptyPath", agent.PromptyPath ?? (object)DBNull.Value);

                var newId = await command.ExecuteScalarAsync();
                agent.Id = Convert.ToInt32(newId);
                return agent;
            }
        }

        public async Task<bool> DeleteAgentAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var deleteCommand = "DELETE FROM Agents WHERE Id = @Id";
            using var command = new SqliteCommand(deleteCommand, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
} 