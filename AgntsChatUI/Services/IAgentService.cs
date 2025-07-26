using AgntsChatUI.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgntsChatUI.Services
{
    /// <summary>
    /// Service layer interface for agent management operations
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// Retrieves all available agents
        /// </summary>
        /// <returns>Collection of all agents</returns>
        Task<IEnumerable<AgentDefinition>> GetAllAgentsAsync();

        /// <summary>
        /// Saves an agent (creates new or updates existing)
        /// </summary>
        /// <param name="agent">The agent to save</param>
        /// <returns>The saved agent with updated Id</returns>
        Task<AgentDefinition> SaveAgentAsync(AgentDefinition agent);

        /// <summary>
        /// Deletes an agent by its Id
        /// </summary>
        /// <param name="id">The Id of the agent to delete</param>
        /// <returns>True if the agent was deleted, false if not found</returns>
        Task<bool> DeleteAgentAsync(int id);

        /// <summary>
        /// Initializes the service and ensures the database is ready
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Migrates data from the old JSON configuration file to the database
        /// </summary>
        /// <returns>True if migration was performed, false if no migration was needed</returns>
        Task<bool> MigrateFromJsonConfigAsync();
    }
} 