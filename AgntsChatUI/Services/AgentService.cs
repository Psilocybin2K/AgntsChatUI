namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using AgntsChatUI.AI;

    /// <summary>
    /// Service layer implementation for agent management operations
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly IAgentRepository _repository;
        private bool _isInitialized = false;

        public AgentService(IAgentRepository repository)
        {
            this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task InitializeAsync()
        {
            if (this._isInitialized)
            {
                return;
            }

            await this._repository.InitializeDatabaseAsync();

            // Attempt to migrate from JSON config if it exists
            await this.MigrateFromJsonConfigAsync();

            this._isInitialized = true;
        }

        public async Task<IEnumerable<AgentDefinition>> GetAllAgentsAsync()
        {
            await this.EnsureInitializedAsync();
            return await this._repository.GetAllAgentsAsync();
        }

        public async Task<AgentDefinition> SaveAgentAsync(AgentDefinition agent)
        {
            await this.EnsureInitializedAsync();

            return string.IsNullOrWhiteSpace(agent.Name)
                ? throw new ArgumentException("Agent name cannot be null or empty", nameof(agent))
                : await this._repository.SaveAgentAsync(agent);
        }

        public async Task<bool> DeleteAgentAsync(int id)
        {
            await this.EnsureInitializedAsync();
            return await this._repository.DeleteAgentAsync(id);
        }

        public async Task<bool> MigrateFromJsonConfigAsync()
        {
            // Try multiple locations for the config file
            string[] possiblePaths = new[]
            {
                "agents.config.json", // Current directory
                Path.Combine("..", "AgntsChatUI.AI", "agents.config.json"), // Relative to current directory
                Path.Combine(Directory.GetCurrentDirectory(), "..", "AgntsChatUI.AI", "agents.config.json"), // Absolute path
            };

            string? configFileName = null;
            foreach (string? path in possiblePaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    configFileName = fullPath;
                    break;
                }
            }

            if (configFileName == null)
            {
                return false;
            }

            try
            {
                // Read existing agents from database to check if migration is needed
                IEnumerable<AgentDefinition> existingAgents = await this._repository.GetAllAgentsAsync();
                if (existingAgents.Any())
                {
                    // Database already has data, no migration needed
                    return false;
                }

                // Read JSON configuration file
                string configContent = await File.ReadAllTextAsync(configFileName);
                AgentDefinition[]? jsonAgents = JsonSerializer.Deserialize<AgentDefinition[]>(configContent);

                if (jsonAgents == null || !jsonAgents.Any())
                {
                    return false;
                }

                // Migrate each agent to the database
                foreach (AgentDefinition agent in jsonAgents)
                {
                    // Ensure Id is null for new agents
                    agent.Id = null;
                    await this._repository.SaveAgentAsync(agent);
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
            if (!this._isInitialized)
            {
                await this.InitializeAsync();
            }
        }
    }
}