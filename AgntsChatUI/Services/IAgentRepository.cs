namespace AgntsChatUI.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using AgntsChatUI.AI;

    /// <summary>
    /// Interface for agent data access operations
    /// </summary>
    public interface IAgentRepository
    {
        /// <summary>
        /// Retrieves all agents from the database
        /// </summary>
        /// <returns>Collection of all agents</returns>
        Task<IEnumerable<AgentDefinition>> GetAllAgentsAsync();

        /// <summary>
        /// Saves an agent to the database (insert if new, update if existing)
        /// </summary>
        /// <param name="agent">The agent to save</param>
        /// <returns>The saved agent with updated Id</returns>
        Task<AgentDefinition> SaveAgentAsync(AgentDefinition agent);

        /// <summary>
        /// Deletes an agent from the database by its Id
        /// </summary>
        /// <param name="id">The Id of the agent to delete</param>
        /// <returns>True if the agent was deleted, false if not found</returns>
        Task<bool> DeleteAgentAsync(int id);

        /// <summary>
        /// Initializes the database and creates the schema if it doesn't exist
        /// </summary>
        Task InitializeDatabaseAsync();
    }
}