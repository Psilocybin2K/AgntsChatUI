namespace AgntsChatUI.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing template files for agent instructions and personas
    /// </summary>
    public interface IFileTemplateService
    {
        /// <summary>
        /// Creates an instruction file for an agent in the PromptTemplates\Instructions directory
        /// </summary>
        /// <param name="agentName">The name of the agent</param>
        /// <param name="description">The description of the agent</param>
        /// <returns>The path to the created instruction file</returns>
        Task<string> CreateInstructionFileAsync(string agentName, string description);

        /// <summary>
        /// Creates a persona file for an agent in the PromptTemplates\Personas directory
        /// </summary>
        /// <param name="agentName">The name of the agent</param>
        /// <param name="description">The description of the agent</param>
        /// <returns>The path to the created persona file</returns>
        Task<string> CreatePersonaFileAsync(string agentName, string description);

        /// <summary>
        /// Ensures that the required directories exist for storing template files
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task EnsureDirectoriesExistAsync();

        /// <summary>
        /// Gets the resolved base directory path (for debugging purposes)
        /// </summary>
        /// <returns>The absolute path to the PromptTemplates directory</returns>
        string GetBaseDirectory();
    }
}