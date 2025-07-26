using AgntsChatUI.AI;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace AgntsChatUI.Services
{
    /// <summary>
    /// Service layer implementation for agent management operations
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly IAgentRepository _repository;
        private bool _isInitialized = false;

        public AgentService(IAgentRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            await _repository.InitializeDatabaseAsync();
            
            // Attempt to migrate from JSON config if it exists
            await MigrateFromJsonConfigAsync();
            
            _isInitialized = true;
        }

        public async Task<IEnumerable<AgentDefinition>> GetAllAgentsAsync()
        {
            await EnsureInitializedAsync();
            return await _repository.GetAllAgentsAsync();
        }

        public async Task<AgentDefinition> SaveAgentAsync(AgentDefinition agent)
        {
            await EnsureInitializedAsync();
            
            if (string.IsNullOrWhiteSpace(agent.Name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(agent));

            return await _repository.SaveAgentAsync(agent);
        }

        public async Task<bool> DeleteAgentAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _repository.DeleteAgentAsync(id);
        }

        public async Task<bool> MigrateFromJsonConfigAsync()
        {
            const string configFileName = "agents.config.json";
            
            if (!File.Exists(configFileName))
                return false;

            try
            {
                // Read existing agents from database to check if migration is needed
                var existingAgents = await _repository.GetAllAgentsAsync();
                if (existingAgents.Any())
                {
                    // Database already has data, no migration needed
                    return false;
                }

                // Read JSON configuration file
                string configContent = await File.ReadAllTextAsync(configFileName);
                var jsonAgents = JsonSerializer.Deserialize<AgentDefinition[]>(configContent);

                if (jsonAgents == null || !jsonAgents.Any())
                {
                    return false;
                }

                // Migrate each agent to the database
                foreach (var agent in jsonAgents)
                {
                    // Ensure Id is null for new agents
                    agent.Id = null;
                    await _repository.SaveAgentAsync(agent);
                }

                // Optionally, you can rename or delete the old config file
                // File.Move(configFileName, $"{configFileName}.backup");
                
                return true;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - migration failure shouldn't break the app
                System.Diagnostics.Debug.WriteLine($"Failed to migrate from JSON config: {ex.Message}");
                return false;
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
    }
} 